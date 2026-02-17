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

        builder.Services.AddOpenApi();

        var app = builder.Build();

        using (var connection = new SqliteConnection(resolvedConnectionString))
        {
            connection.Open();
        }

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseHttpsRedirection();

        PublicEndpoints.MapPublicEndpoints(app);

        app.Run();
    }
}
