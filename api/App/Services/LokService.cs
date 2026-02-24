using System.Data;
using Microsoft.Data.Sqlite;

public class LokService
{
  private readonly SqliteConnection _connection;

  public LokService(SqliteConnection connection)
  {
    _connection = connection;
  }

  public async Task<List<LokOpenHours>> GetOpenHours()
  {
    if (_connection.State != ConnectionState.Open)
    {
      await _connection.OpenAsync();
    }

    await using var command = _connection.CreateCommand();
    command.CommandText = @"
      SELECT id, name, version, paragraph1, paragraph2, paragraph3, paragraph4, kitchenNotice
      FROM LokOpenHours
      ORDER BY datetime(version) DESC, id DESC
      LIMIT 5";

    await using var reader = await command.ExecuteReaderAsync();

    var openHoursList = new List<LokOpenHours>();

    while (await reader.ReadAsync())
    {
      openHoursList.Add(new LokOpenHours
      {
        Id = reader["id"] is long id ? id : Convert.ToInt64(reader["id"]),
        Name = reader["name"]?.ToString() ?? string.Empty,
        Version = ParseVersion(reader["version"]?.ToString()),
        Paragraph1 = reader["paragraph1"]?.ToString() ?? string.Empty,
        Paragraph2 = reader["paragraph2"]?.ToString() ?? string.Empty,
        Paragraph3 = reader["paragraph3"]?.ToString() ?? string.Empty,
        Paragraph4 = reader["paragraph4"]?.ToString() ?? string.Empty,
        KitchenNotice = reader["kitchenNotice"]?.ToString() ?? string.Empty
      });
    }

    return openHoursList;
  }

  public async Task<LokOpenHours> InsertOpenHours(LokOpenHours openHours)
  {
    if (_connection.State != ConnectionState.Open)
    {
      await _connection.OpenAsync();
    }

    var version = DateTime.UtcNow;

    await using var command = _connection.CreateCommand();
    command.CommandText = @"
      INSERT INTO LokOpenHours (name, version, paragraph1, paragraph2, paragraph3, paragraph4, kitchenNotice)
      VALUES (@name, @version, @paragraph1, @paragraph2, @paragraph3, @paragraph4, @kitchenNotice);
      SELECT last_insert_rowid();";

    command.Parameters.AddWithValue("@name", openHours.Name ?? string.Empty);
    command.Parameters.AddWithValue("@version", version.ToString("O"));
    command.Parameters.AddWithValue("@paragraph1", openHours.Paragraph1 ?? string.Empty);
    command.Parameters.AddWithValue("@paragraph2", openHours.Paragraph2 ?? string.Empty);
    command.Parameters.AddWithValue("@paragraph3", openHours.Paragraph3 ?? string.Empty);
    command.Parameters.AddWithValue("@paragraph4", openHours.Paragraph4 ?? string.Empty);
    command.Parameters.AddWithValue("@kitchenNotice", openHours.KitchenNotice ?? string.Empty);

    var insertedId = await command.ExecuteScalarAsync();

    return new LokOpenHours
    {
      Id = Convert.ToInt64(insertedId),
      Name = openHours.Name ?? string.Empty,
      Version = version,
      Paragraph1 = openHours.Paragraph1 ?? string.Empty,
      Paragraph2 = openHours.Paragraph2 ?? string.Empty,
      Paragraph3 = openHours.Paragraph3 ?? string.Empty,
      Paragraph4 = openHours.Paragraph4 ?? string.Empty,
      KitchenNotice = openHours.KitchenNotice ?? string.Empty
    };
  }

  public async Task<bool> DeleteOpenHours(long id)
  {
    if (_connection.State != ConnectionState.Open)
    {
      await _connection.OpenAsync();
    }

    await using var command = _connection.CreateCommand();
    command.CommandText = @"
      DELETE FROM LokOpenHours
      WHERE id = @id;";

    command.Parameters.AddWithValue("@id", id);

    var affectedRows = await command.ExecuteNonQueryAsync();
    return affectedRows > 0;
  }

  private static DateTime ParseVersion(string? value)
  {
    if (!string.IsNullOrWhiteSpace(value) && DateTime.TryParse(value, out var parsed))
    {
      return parsed;
    }

    return DateTime.MinValue;
  }
}