using System.Data;
using Microsoft.Data.Sqlite;

public class LokService
{
  private readonly SqliteConnection _connection;

  public LokService(SqliteConnection connection)
  {
    _connection = connection;
  }

  public async Task<LokOpenHours?> GetOpenHoursAsync()
  {
    if (_connection.State != ConnectionState.Open)
    {
      await _connection.OpenAsync();
    }

    await using var command = _connection.CreateCommand();
    command.CommandText = @"
      SELECT paragraph1, paragraph2, paragraph3, paragraph4, kitchenNotice
      FROM LokOpenHours
      LIMIT 1";

    await using var reader = await command.ExecuteReaderAsync();

    if (!await reader.ReadAsync())
    {
      return null;
    }

    return new LokOpenHours
    {
      Paragraph1 = reader["paragraph1"]?.ToString() ?? string.Empty,
      Paragraph2 = reader["paragraph2"]?.ToString() ?? string.Empty,
      Paragraph3 = reader["paragraph3"]?.ToString() ?? string.Empty,
      Paragraph4 = reader["paragraph4"]?.ToString() ?? string.Empty,
      KitchenNotice = reader["kitchenNotice"]?.ToString() ?? string.Empty
    };
  }
}