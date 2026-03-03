using Microsoft.Data.Sqlite;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

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

        var authOptions = builder.Configuration.GetSection("Auth").Get<AuthOptions>()
            ?? throw new InvalidOperationException("Auth configuration was not found.");

        if (builder.Environment.IsProduction())
        {
            authOptions.Admin.Password =
                Environment.GetEnvironmentVariable("KLAPI_ADMIN_PASSWORD")
                ?? throw new InvalidOperationException("Admin password must be set in production environment using KLAPI_ADMIN_PASSWORD environment variable.");
        }

        if (string.IsNullOrWhiteSpace(authOptions.SigningKey) || authOptions.SigningKey.Length < 32)
        {
            throw new InvalidOperationException("Auth:SigningKey must be at least 32 characters long.");
        }

        if (string.IsNullOrWhiteSpace(authOptions.Admin.Username)
            || string.IsNullOrWhiteSpace(authOptions.Admin.Password)
            || string.IsNullOrWhiteSpace(authOptions.Admin.DisplayName))
        {
            throw new InvalidOperationException("Auth:Admin username, password and display name must be configured.");
        }

        builder.Services.Configure<AuthOptions>(builder.Configuration.GetSection("Auth"));

        builder.Services.AddScoped(_ => new SqliteConnection(resolvedConnectionString));
        builder.Services.AddScoped<LokService>();
        builder.Services.AddScoped<UserService>();
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("PublicReadCors", policy =>
            {
                policy
                    .AllowAnyOrigin()
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });

            options.AddPolicy("FrontendWriteCors", policy =>
            {
                policy
                    .WithOrigins(authOptions.AllowedOrigins.ToArray())
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ValidIssuer = authOptions.Issuer,
                    ValidAudience = authOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(authOptions.SigningKey)),
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
            });

        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy("OpenHoursWrite", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireClaim("scope", "openhours:write");
            });

            options.AddPolicy("AdminOnly", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireClaim("is_admin", "true");
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

                command.CommandText = @"
                                    CREATE UNIQUE INDEX IF NOT EXISTS IX_Users_Username
                                    ON Users(username);";
                command.ExecuteNonQuery();
            }
        }

        using (var scope = app.Services.CreateScope())
        {
            var userService = scope.ServiceProvider.GetRequiredService<UserService>();
            userService.EnsureAdminUser(authOptions.Admin).GetAwaiter().GetResult();
        }

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseCors();

        app.UseAuthentication();
        app.UseAuthorization();

        if (!app.Environment.IsDevelopment())
        {
            app.UseHttpsRedirection();
        }

        SystemEndpoints.MapSystemEndpoints(app);
        AuthEndpoints.MapAuthEndpoints(app);
        LokEndpoints.MapLokEndpoints(app);
        UserEndpoints.MapUserEndpoints(app);

        app.Run();
    }
}
