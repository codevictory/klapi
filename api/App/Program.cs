using Microsoft.Data.Sqlite;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        var configuredConnectionString = builder.Configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

        var sqliteConnectionStringBuilder = new SqliteConnectionStringBuilder(configuredConnectionString);
        var databasePath = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, sqliteConnectionStringBuilder.DataSource));
        var databaseDirectory = Path.GetDirectoryName(databasePath)
            ?? throw new InvalidOperationException("Could not determine database directory.");

        Directory.CreateDirectory(databaseDirectory);

        sqliteConnectionStringBuilder.DataSource = databasePath;
        var resolvedConnectionString = sqliteConnectionStringBuilder.ToString();

        builder.Services.AddScoped(_ => new SqliteConnection(resolvedConnectionString));
        builder.Services.AddScoped<LokService>();
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("UiCors", policy =>
            {
                policy
                    .WithOrigins(
                        "http://localhost:5173",
                        "http://127.0.0.1:5173",
                        "http://localhost:4173",
                        "http://127.0.0.1:4173")
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        builder.Services.AddOpenApi();

        var app = builder.Build();

        using (var connection = new SqliteConnection(resolvedConnectionString))
        {
            connection.Open();

            var initScriptPath = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "../Database/init.sql"));

            if (File.Exists(initScriptPath))
            {
                var initSql = File.ReadAllText(initScriptPath);
                if (!string.IsNullOrWhiteSpace(initSql))
                {
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = initSql;
                        command.ExecuteNonQuery();
                    }
                }
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT COUNT(*) FROM pragma_table_info('LokOpenHours') WHERE name = 'version';";
                var hasVersionColumn = Convert.ToInt32(command.ExecuteScalar()) > 0;

                if (!hasVersionColumn)
                {
                    command.CommandText = "ALTER TABLE LokOpenHours ADD COLUMN version TEXT NOT NULL DEFAULT '';";
                    command.ExecuteNonQuery();
                }

                command.CommandText = "SELECT COUNT(*) FROM pragma_table_info('LokOpenHours') WHERE name = 'name';";
                var hasNameColumn = Convert.ToInt32(command.ExecuteScalar()) > 0;

                if (!hasNameColumn)
                {
                    command.CommandText = "ALTER TABLE LokOpenHours ADD COLUMN name TEXT NOT NULL DEFAULT '';";
                    command.ExecuteNonQuery();
                }

                command.CommandText = "SELECT COUNT(*) FROM pragma_table_info('LokOpenHours') WHERE name = 'isActive';";
                var hasIsActiveColumn = Convert.ToInt32(command.ExecuteScalar()) > 0;

                if (!hasIsActiveColumn)
                {
                    command.CommandText = "ALTER TABLE LokOpenHours ADD COLUMN isActive INTEGER NOT NULL DEFAULT 0;";
                    command.ExecuteNonQuery();
                }

                command.CommandText = "SELECT COUNT(*) FROM LokOpenHours WHERE isActive = 1;";
                var activeCount = Convert.ToInt32(command.ExecuteScalar());

                if (activeCount == 0)
                {
                    command.CommandText = @"
                                            UPDATE LokOpenHours
                                            SET isActive = 1
                                            WHERE id = (
                                                SELECT id
                                                FROM LokOpenHours
                                                ORDER BY datetime(version) DESC, id DESC
                                                LIMIT 1
                                            );";
                    command.ExecuteNonQuery();
                }
                else if (activeCount > 1)
                {
                    command.CommandText = @"
                                            WITH selected_active AS (
                                                SELECT id
                                                FROM LokOpenHours
                                                WHERE isActive = 1
                                                ORDER BY datetime(version) DESC, id DESC
                                                LIMIT 1
                                            )
                                            UPDATE LokOpenHours
                                            SET isActive = CASE
                                                WHEN id = (SELECT id FROM selected_active) THEN 1
                                                ELSE 0
                                            END
                                            WHERE isActive = 1
                                                OR id = (SELECT id FROM selected_active);";
                    command.ExecuteNonQuery();
                }

                command.CommandText = @"
                                    CREATE UNIQUE INDEX IF NOT EXISTS IX_LokOpenHours_OneActive
                                    ON LokOpenHours(isActive)
                                    WHERE isActive = 1;";
                command.ExecuteNonQuery();
            }
        }

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseCors("UiCors");

        if (!app.Environment.IsDevelopment())
        {
            app.UseHttpsRedirection();
        }

        SystemEndpoints.MapSystemEndpoints(app);
        LokEndpoints.MapLokEndpoints(app);

        app.Run();
    }
}
