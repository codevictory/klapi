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
            }
        }

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseHttpsRedirection();

        SystemEndpoints.MapSystemEndpoints(app);
        LokEndpoints.MapLokEndpoints(app);

        app.Run();
    }
}
