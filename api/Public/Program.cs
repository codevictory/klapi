using Microsoft.Data.Sqlite;

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

app.MapGet("/", () =>
{
    return new
    {
        Version = "1.0.0"
    };
})
.WithName("GetVersion");

app.MapGet("/health/db", async (SqliteConnection connection) =>
{
    await connection.OpenAsync();
    await using var command = connection.CreateCommand();
    command.CommandText = "SELECT 1";
    var result = await command.ExecuteScalarAsync();

    return Results.Ok(new
    {
        Database = "ok",
        Result = result
    });
})
.WithName("GetDatabaseHealth");

app.Run();
