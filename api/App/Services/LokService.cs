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
      SELECT id, name, isActive, version, paragraph1, paragraph2, paragraph3, paragraph4, kitchenNotice
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
        IsActive = ParseBoolean(reader["isActive"]),
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

    using var transaction = _connection.BeginTransaction();

    await using var resetCommand = _connection.CreateCommand();
    resetCommand.Transaction = transaction;
    resetCommand.CommandText = "UPDATE LokOpenHours SET isActive = 0 WHERE isActive = 1;";
    await resetCommand.ExecuteNonQueryAsync();

    await using var command = _connection.CreateCommand();
    command.Transaction = transaction;
    command.CommandText = @"
      INSERT INTO LokOpenHours (name, isActive, version, paragraph1, paragraph2, paragraph3, paragraph4, kitchenNotice)
      VALUES (@name, @isActive, @version, @paragraph1, @paragraph2, @paragraph3, @paragraph4, @kitchenNotice);
      SELECT last_insert_rowid();";

    command.Parameters.AddWithValue("@name", openHours.Name ?? string.Empty);
    command.Parameters.AddWithValue("@isActive", 1);
    command.Parameters.AddWithValue("@version", version.ToString("O"));
    command.Parameters.AddWithValue("@paragraph1", openHours.Paragraph1 ?? string.Empty);
    command.Parameters.AddWithValue("@paragraph2", openHours.Paragraph2 ?? string.Empty);
    command.Parameters.AddWithValue("@paragraph3", openHours.Paragraph3 ?? string.Empty);
    command.Parameters.AddWithValue("@paragraph4", openHours.Paragraph4 ?? string.Empty);
    command.Parameters.AddWithValue("@kitchenNotice", openHours.KitchenNotice ?? string.Empty);

    var insertedId = await command.ExecuteScalarAsync();
    var insertedIdValue = Convert.ToInt64(insertedId);

    transaction.Commit();

    return new LokOpenHours
    {
      Id = insertedIdValue,
      Name = openHours.Name ?? string.Empty,
      IsActive = true,
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

    await using var activeCommand = _connection.CreateCommand();
    activeCommand.CommandText = "SELECT isActive FROM LokOpenHours WHERE id = @id;";
    activeCommand.Parameters.AddWithValue("@id", id);
    var activeValue = await activeCommand.ExecuteScalarAsync();

    if (activeValue is null)
    {
      return false;
    }

    await using var command = _connection.CreateCommand();
    command.CommandText = @"
      DELETE FROM LokOpenHours
      WHERE id = @id;";

    command.Parameters.AddWithValue("@id", id);

    var affectedRows = await command.ExecuteNonQueryAsync();

    if (affectedRows > 0)
    {
      await EnsureSingleActiveInvariant();
    }

    return affectedRows > 0;
  }

  public async Task<LokOpenHours?> UpdateOpenHours(long id, LokOpenHours openHours)
  {
    if (_connection.State != ConnectionState.Open)
    {
      await _connection.OpenAsync();
    }

    var version = DateTime.UtcNow;

    await using var activeCommand = _connection.CreateCommand();
    activeCommand.CommandText = "SELECT isActive FROM LokOpenHours WHERE id = @id;";
    activeCommand.Parameters.AddWithValue("@id", id);
    var activeValue = await activeCommand.ExecuteScalarAsync();

    if (activeValue is null)
    {
      return null;
    }

    var isActive = ParseBoolean(activeValue);

    await using var command = _connection.CreateCommand();
    command.CommandText = @"
      UPDATE LokOpenHours
      SET
        name = @name,
        version = @version,
        paragraph1 = @paragraph1,
        paragraph2 = @paragraph2,
        paragraph3 = @paragraph3,
        paragraph4 = @paragraph4,
        kitchenNotice = @kitchenNotice
      WHERE id = @id;";

    command.Parameters.AddWithValue("@id", id);
    command.Parameters.AddWithValue("@name", openHours.Name ?? string.Empty);
    command.Parameters.AddWithValue("@version", version.ToString("O"));
    command.Parameters.AddWithValue("@paragraph1", openHours.Paragraph1 ?? string.Empty);
    command.Parameters.AddWithValue("@paragraph2", openHours.Paragraph2 ?? string.Empty);
    command.Parameters.AddWithValue("@paragraph3", openHours.Paragraph3 ?? string.Empty);
    command.Parameters.AddWithValue("@paragraph4", openHours.Paragraph4 ?? string.Empty);
    command.Parameters.AddWithValue("@kitchenNotice", openHours.KitchenNotice ?? string.Empty);

    var affectedRows = await command.ExecuteNonQueryAsync();

    if (affectedRows == 0)
    {
      return null;
    }

    return new LokOpenHours
    {
      Id = id,
      Name = openHours.Name ?? string.Empty,
      IsActive = isActive,
      Version = version,
      Paragraph1 = openHours.Paragraph1 ?? string.Empty,
      Paragraph2 = openHours.Paragraph2 ?? string.Empty,
      Paragraph3 = openHours.Paragraph3 ?? string.Empty,
      Paragraph4 = openHours.Paragraph4 ?? string.Empty,
      KitchenNotice = openHours.KitchenNotice ?? string.Empty
    };
  }

  public async Task<bool> SetActiveOpenHours(long id)
  {
    if (_connection.State != ConnectionState.Open)
    {
      await _connection.OpenAsync();
    }

    using var transaction = _connection.BeginTransaction();

    await using var existsCommand = _connection.CreateCommand();
    existsCommand.Transaction = transaction;
    existsCommand.CommandText = "SELECT COUNT(*) FROM LokOpenHours WHERE id = @id;";
    existsCommand.Parameters.AddWithValue("@id", id);
    var exists = Convert.ToInt32(await existsCommand.ExecuteScalarAsync()) > 0;

    if (!exists)
    {
      transaction.Rollback();
      return false;
    }

    await using var resetCommand = _connection.CreateCommand();
    resetCommand.Transaction = transaction;
    resetCommand.CommandText = "UPDATE LokOpenHours SET isActive = 0 WHERE isActive = 1;";
    await resetCommand.ExecuteNonQueryAsync();

    await using var activateCommand = _connection.CreateCommand();
    activateCommand.Transaction = transaction;
    activateCommand.CommandText = "UPDATE LokOpenHours SET isActive = 1 WHERE id = @id;";
    activateCommand.Parameters.AddWithValue("@id", id);
    await activateCommand.ExecuteNonQueryAsync();

    transaction.Commit();
    return true;
  }

  private static DateTime ParseVersion(string? value)
  {
    if (!string.IsNullOrWhiteSpace(value) && DateTime.TryParse(value, out var parsed))
    {
      return parsed;
    }

    return DateTime.MinValue;
  }

  private static bool ParseBoolean(object? value)
  {
    return value switch
    {
      bool boolValue => boolValue,
      long longValue => longValue == 1,
      int intValue => intValue == 1,
      string stringValue when int.TryParse(stringValue, out var parsedInt) => parsedInt == 1,
      string stringValue when bool.TryParse(stringValue, out var parsedBool) => parsedBool,
      _ => false
    };
  }

  private async Task EnsureSingleActiveInvariant()
  {
    await using var countCommand = _connection.CreateCommand();
    countCommand.CommandText = "SELECT COUNT(*) FROM LokOpenHours WHERE isActive = 1;";
    var activeCount = Convert.ToInt32(await countCommand.ExecuteScalarAsync());

    if (activeCount == 0)
    {
      await using var promoteCommand = _connection.CreateCommand();
      promoteCommand.CommandText = @"
        UPDATE LokOpenHours
        SET isActive = 1
        WHERE id = (
          SELECT id
          FROM LokOpenHours
          ORDER BY datetime(version) DESC, id DESC
          LIMIT 1
        );";
      await promoteCommand.ExecuteNonQueryAsync();
      return;
    }

    if (activeCount > 1)
    {
      await using var normalizeCommand = _connection.CreateCommand();
      normalizeCommand.CommandText = @"
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
      await normalizeCommand.ExecuteNonQueryAsync();
    }
  }
}