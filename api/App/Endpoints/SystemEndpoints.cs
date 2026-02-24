public static class SystemEndpoints
{
  public static void MapSystemEndpoints(WebApplication app)
  {
    app.MapGet("/", () =>
    {
      return new
      {
        Version = "1.0.0"
      };
    })
    .WithName("GetVersion");

    app.MapGet("/health/db", async (Microsoft.Data.Sqlite.SqliteConnection connection) =>
    {
      await connection.OpenAsync();
      await using var command = connection.CreateCommand();
      command.CommandText = "SELECT 1";
      var result = await command.ExecuteScalarAsync();

      return new
      {
        Database = "ok",
        Result = result
      };
    })
    .WithName("GetDatabaseHealth");
  }
}