using System.Data;
using Microsoft.Data.Sqlite;

public class VihkotappeluService
{
  private const string EnglishLanguage = "en";

  private readonly SqliteConnection _connection;

  public VihkotappeluService(SqliteConnection connection)
  {
    _connection = connection;
  }

  public async Task<VihkotappeluGameData> GetAll()
  {
    await EnsureOpenConnection();

    var data = new VihkotappeluGameData();

    await using (var command = _connection.CreateCommand())
    {
      command.CommandText = @"
        SELECT unitKey, name, description, hp, attack, defence, movement, range, minRange, sprite, sortOrder
        FROM VihkotappeluUnits
        ORDER BY sortOrder ASC, unitKey ASC;";

      await using var reader = await command.ExecuteReaderAsync();
      while (await reader.ReadAsync())
      {
        data.Units.Add(ReadUnit(reader));
      }
    }

    var unitTranslations = await ReadAllUnitTranslations();
    foreach (var unit in data.Units)
    {
      if (unitTranslations.TryGetValue(unit.UnitKey, out var translations))
      {
        unit.Translations = translations;
      }

      // The base columns are the canonical English texts; make sure "en" is
      // always available even when no explicit translation row exists.
      if (!unit.Translations.ContainsKey(EnglishLanguage))
      {
        unit.Translations[EnglishLanguage] = new VihkotappeluUnitTranslation
        {
          Name = unit.Name,
          Description = unit.Description
        };
      }
    }

    await using (var command = _connection.CreateCommand())
    {
      command.CommandText = @"
        SELECT id, name, color, sortOrder
        FROM VihkotappeluFactions
        ORDER BY sortOrder ASC, id ASC;";

      await using var reader = await command.ExecuteReaderAsync();
      while (await reader.ReadAsync())
      {
        data.Factions.Add(new VihkotappeluFaction
        {
          Id = reader["id"]?.ToString() ?? string.Empty,
          Name = reader["name"]?.ToString() ?? string.Empty,
          Color = reader["color"]?.ToString() ?? string.Empty,
          SortOrder = reader["sortOrder"] is long sortOrder ? (int)sortOrder : Convert.ToInt32(reader["sortOrder"])
        });
      }
    }

    var factionTranslations = new Dictionary<string, Dictionary<string, VihkotappeluFactionTranslation>>(StringComparer.OrdinalIgnoreCase);

    await using (var command = _connection.CreateCommand())
    {
      command.CommandText = @"
        SELECT factionId, language, name
        FROM VihkotappeluFactionTranslations;";

      await using var reader = await command.ExecuteReaderAsync();
      while (await reader.ReadAsync())
      {
        var factionId = reader["factionId"]?.ToString() ?? string.Empty;
        var language = (reader["language"]?.ToString() ?? string.Empty).Trim().ToLowerInvariant();
        if (factionId.Length == 0 || language.Length == 0)
        {
          continue;
        }

        if (!factionTranslations.TryGetValue(factionId, out var translations))
        {
          translations = new Dictionary<string, VihkotappeluFactionTranslation>(StringComparer.OrdinalIgnoreCase);
          factionTranslations[factionId] = translations;
        }

        translations[language] = new VihkotappeluFactionTranslation
        {
          Name = reader["name"]?.ToString() ?? string.Empty
        };
      }
    }

    await using (var command = _connection.CreateCommand())
    {
      command.CommandText = @"
        SELECT fu.factionId, fu.unitKey
        FROM VihkotappeluFactionUnits fu
        JOIN VihkotappeluUnits u ON u.unitKey = fu.unitKey
        ORDER BY fu.factionId ASC, u.sortOrder ASC, fu.unitKey ASC;";

      await using var reader = await command.ExecuteReaderAsync();
      while (await reader.ReadAsync())
      {
        var factionId = reader["factionId"]?.ToString() ?? string.Empty;
        var unitKey = reader["unitKey"]?.ToString() ?? string.Empty;
        var faction = data.Factions.FirstOrDefault(f => string.Equals(f.Id, factionId, StringComparison.OrdinalIgnoreCase));
        if (faction is not null && unitKey.Length > 0)
        {
          faction.Units.Add(unitKey);
        }
      }
    }

    foreach (var faction in data.Factions)
    {
      if (factionTranslations.TryGetValue(faction.Id, out var translations))
      {
        faction.Translations = translations;
      }

      if (!faction.Translations.ContainsKey(EnglishLanguage))
      {
        faction.Translations[EnglishLanguage] = new VihkotappeluFactionTranslation
        {
          Name = faction.Name
        };
      }
    }

    return data;
  }

  public async Task<VihkotappeluGameData> Sync(VihkotappeluGameData data)
  {
    await EnsureOpenConnection();

    var now = DateTime.UtcNow.ToString("O");

    using var transaction = _connection.BeginTransaction();

    await using (var upsertUnitCommand = _connection.CreateCommand())
    {
      upsertUnitCommand.Transaction = transaction;
      upsertUnitCommand.CommandText = @"
        INSERT INTO VihkotappeluUnits (unitKey, name, description, hp, attack, defence, movement, range, minRange, sprite, sortOrder, lastUpdated)
        VALUES (@unitKey, @name, @description, @hp, @attack, @defence, @movement, @range, @minRange, @sprite, @sortOrder, @lastUpdated)
        ON CONFLICT(unitKey) DO UPDATE SET
          name = excluded.name,
          description = excluded.description,
          hp = excluded.hp,
          attack = excluded.attack,
          defence = excluded.defence,
          movement = excluded.movement,
          range = excluded.range,
          minRange = excluded.minRange,
          sprite = excluded.sprite,
          sortOrder = excluded.sortOrder,
          lastUpdated = excluded.lastUpdated;";

      var unitKeyParam = upsertUnitCommand.Parameters.Add("@unitKey", SqliteType.Text);
      var nameParam = upsertUnitCommand.Parameters.Add("@name", SqliteType.Text);
      var descriptionParam = upsertUnitCommand.Parameters.Add("@description", SqliteType.Text);
      var hpParam = upsertUnitCommand.Parameters.Add("@hp", SqliteType.Integer);
      var attackParam = upsertUnitCommand.Parameters.Add("@attack", SqliteType.Integer);
      var defenceParam = upsertUnitCommand.Parameters.Add("@defence", SqliteType.Integer);
      var movementParam = upsertUnitCommand.Parameters.Add("@movement", SqliteType.Integer);
      var rangeParam = upsertUnitCommand.Parameters.Add("@range", SqliteType.Integer);
      var minRangeParam = upsertUnitCommand.Parameters.Add("@minRange", SqliteType.Integer);
      var spriteParam = upsertUnitCommand.Parameters.Add("@sprite", SqliteType.Text);
      var sortOrderParam = upsertUnitCommand.Parameters.Add("@sortOrder", SqliteType.Integer);
      var lastUpdatedParam = upsertUnitCommand.Parameters.Add("@lastUpdated", SqliteType.Text);

      foreach (var unit in data.Units)
      {
        unitKeyParam.Value = unit.UnitKey;
        nameParam.Value = unit.Name;
        descriptionParam.Value = unit.Description;
        hpParam.Value = unit.Hp;
        attackParam.Value = unit.Attack;
        defenceParam.Value = unit.Defence;
        movementParam.Value = unit.Movement;
        rangeParam.Value = unit.Range;
        minRangeParam.Value = unit.MinRange;
        spriteParam.Value = unit.Sprite;
        sortOrderParam.Value = unit.SortOrder;
        lastUpdatedParam.Value = now;
        await upsertUnitCommand.ExecuteNonQueryAsync();
      }
    }

    // Replace the translation rows of every synced unit. Units without any
    // supplied translations still get the canonical English row derived from
    // the base columns. Rows of deleted units are removed by the
    // ON DELETE CASCADE on VihkotappeluUnitTranslations.
    await using (var deleteUnitTranslationsCommand = _connection.CreateCommand())
    {
      deleteUnitTranslationsCommand.Transaction = transaction;
      deleteUnitTranslationsCommand.CommandText = "DELETE FROM VihkotappeluUnitTranslations WHERE unitKey = @unitKey;";
      var unitKeyParam = deleteUnitTranslationsCommand.Parameters.Add("@unitKey", SqliteType.Text);

      await using var insertUnitTranslationCommand = _connection.CreateCommand();
      insertUnitTranslationCommand.Transaction = transaction;
      insertUnitTranslationCommand.CommandText = @"
        INSERT INTO VihkotappeluUnitTranslations (unitKey, language, name, description)
        VALUES (@unitKey, @language, @name, @description);";
      var insUnitKeyParam = insertUnitTranslationCommand.Parameters.Add("@unitKey", SqliteType.Text);
      var languageParam = insertUnitTranslationCommand.Parameters.Add("@language", SqliteType.Text);
      var insNameParam = insertUnitTranslationCommand.Parameters.Add("@name", SqliteType.Text);
      var insDescriptionParam = insertUnitTranslationCommand.Parameters.Add("@description", SqliteType.Text);

      foreach (var unit in data.Units)
      {
        unitKeyParam.Value = unit.UnitKey;
        await deleteUnitTranslationsCommand.ExecuteNonQueryAsync();

        foreach (var (language, translation) in NormalizeUnitTranslations(unit))
        {
          insUnitKeyParam.Value = unit.UnitKey;
          languageParam.Value = language;
          insNameParam.Value = translation.Name;
          insDescriptionParam.Value = translation.Description;
          await insertUnitTranslationCommand.ExecuteNonQueryAsync();
        }
      }
    }

    await using (var upsertFactionCommand = _connection.CreateCommand())
    {
      upsertFactionCommand.Transaction = transaction;
      upsertFactionCommand.CommandText = @"
        INSERT INTO VihkotappeluFactions (id, name, color, sortOrder, lastUpdated)
        VALUES (@id, @name, @color, @sortOrder, @lastUpdated)
        ON CONFLICT(id) DO UPDATE SET
          name = excluded.name,
          color = excluded.color,
          sortOrder = excluded.sortOrder,
          lastUpdated = excluded.lastUpdated;";

      var idParam = upsertFactionCommand.Parameters.Add("@id", SqliteType.Text);
      var nameParam = upsertFactionCommand.Parameters.Add("@name", SqliteType.Text);
      var colorParam = upsertFactionCommand.Parameters.Add("@color", SqliteType.Text);
      var sortOrderParam = upsertFactionCommand.Parameters.Add("@sortOrder", SqliteType.Integer);
      var lastUpdatedParam = upsertFactionCommand.Parameters.Add("@lastUpdated", SqliteType.Text);

      foreach (var faction in data.Factions)
      {
        idParam.Value = faction.Id;
        nameParam.Value = faction.Name;
        colorParam.Value = faction.Color;
        sortOrderParam.Value = faction.SortOrder;
        lastUpdatedParam.Value = now;
        await upsertFactionCommand.ExecuteNonQueryAsync();
      }
    }

    await using (var deleteFactionTranslationsCommand = _connection.CreateCommand())
    {
      deleteFactionTranslationsCommand.Transaction = transaction;
      deleteFactionTranslationsCommand.CommandText = "DELETE FROM VihkotappeluFactionTranslations WHERE factionId = @factionId;";
      var factionIdParam = deleteFactionTranslationsCommand.Parameters.Add("@factionId", SqliteType.Text);

      await using var insertFactionTranslationCommand = _connection.CreateCommand();
      insertFactionTranslationCommand.Transaction = transaction;
      insertFactionTranslationCommand.CommandText = @"
        INSERT INTO VihkotappeluFactionTranslations (factionId, language, name)
        VALUES (@factionId, @language, @name);";
      var insFactionIdParam = insertFactionTranslationCommand.Parameters.Add("@factionId", SqliteType.Text);
      var languageParam = insertFactionTranslationCommand.Parameters.Add("@language", SqliteType.Text);
      var insNameParam = insertFactionTranslationCommand.Parameters.Add("@name", SqliteType.Text);

      foreach (var faction in data.Factions)
      {
        factionIdParam.Value = faction.Id;
        await deleteFactionTranslationsCommand.ExecuteNonQueryAsync();

        foreach (var (language, translation) in NormalizeFactionTranslations(faction))
        {
          insFactionIdParam.Value = faction.Id;
          languageParam.Value = language;
          insNameParam.Value = translation.Name;
          await insertFactionTranslationCommand.ExecuteNonQueryAsync();
        }
      }
    }

    // Replace the faction -> unit links of every synced faction.
    await using (var deleteLinksCommand = _connection.CreateCommand())
    {
      deleteLinksCommand.Transaction = transaction;
      deleteLinksCommand.CommandText = "DELETE FROM VihkotappeluFactionUnits WHERE factionId = @factionId;";
      var factionIdParam = deleteLinksCommand.Parameters.Add("@factionId", SqliteType.Text);

      await using var insertLinkCommand = _connection.CreateCommand();
      insertLinkCommand.Transaction = transaction;
      insertLinkCommand.CommandText = @"
        INSERT INTO VihkotappeluFactionUnits (factionId, unitKey)
        VALUES (@factionId, @unitKey);";
      var insFactionIdParam = insertLinkCommand.Parameters.Add("@factionId", SqliteType.Text);
      var insUnitKeyParam = insertLinkCommand.Parameters.Add("@unitKey", SqliteType.Text);

      foreach (var faction in data.Factions)
      {
        factionIdParam.Value = faction.Id;
        await deleteLinksCommand.ExecuteNonQueryAsync();

        foreach (var unitKey in faction.Units)
        {
          insFactionIdParam.Value = faction.Id;
          insUnitKeyParam.Value = unitKey;
          await insertLinkCommand.ExecuteNonQueryAsync();
        }
      }
    }

    // Remove units and factions that are no longer in the payload. Their
    // translation/link rows go away via ON DELETE CASCADE.
    await using (var deleteUnitsCommand = _connection.CreateCommand())
    {
      deleteUnitsCommand.Transaction = transaction;

      if (data.Units.Count == 0)
      {
        deleteUnitsCommand.CommandText = "DELETE FROM VihkotappeluUnits;";
      }
      else
      {
        var keyParameters = string.Join(", ", data.Units.Select((_, index) => $"@unitKey{index}"));
        deleteUnitsCommand.CommandText = $"DELETE FROM VihkotappeluUnits WHERE unitKey NOT IN ({keyParameters});";
        for (var index = 0; index < data.Units.Count; index++)
        {
          deleteUnitsCommand.Parameters.AddWithValue($"@unitKey{index}", data.Units[index].UnitKey);
        }
      }

      await deleteUnitsCommand.ExecuteNonQueryAsync();
    }

    await using (var deleteFactionsCommand = _connection.CreateCommand())
    {
      deleteFactionsCommand.Transaction = transaction;

      if (data.Factions.Count == 0)
      {
        deleteFactionsCommand.CommandText = "DELETE FROM VihkotappeluFactions;";
      }
      else
      {
        var idParameters = string.Join(", ", data.Factions.Select((_, index) => $"@id{index}"));
        deleteFactionsCommand.CommandText = $"DELETE FROM VihkotappeluFactions WHERE id NOT IN ({idParameters});";
        for (var index = 0; index < data.Factions.Count; index++)
        {
          deleteFactionsCommand.Parameters.AddWithValue($"@id{index}", data.Factions[index].Id);
        }
      }

      await deleteFactionsCommand.ExecuteNonQueryAsync();
    }

    transaction.Commit();

    return await GetAll();
  }

  private async Task<Dictionary<string, Dictionary<string, VihkotappeluUnitTranslation>>> ReadAllUnitTranslations()
  {
    var result = new Dictionary<string, Dictionary<string, VihkotappeluUnitTranslation>>(StringComparer.OrdinalIgnoreCase);

    await using var command = _connection.CreateCommand();
    command.CommandText = @"
      SELECT unitKey, language, name, description
      FROM VihkotappeluUnitTranslations;";

    await using var reader = await command.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
      var unitKey = reader["unitKey"]?.ToString() ?? string.Empty;
      var language = (reader["language"]?.ToString() ?? string.Empty).Trim().ToLowerInvariant();
      if (unitKey.Length == 0 || language.Length == 0)
      {
        continue;
      }

      if (!result.TryGetValue(unitKey, out var translations))
      {
        translations = new Dictionary<string, VihkotappeluUnitTranslation>(StringComparer.OrdinalIgnoreCase);
        result[unitKey] = translations;
      }

      translations[language] = new VihkotappeluUnitTranslation
      {
        Name = reader["name"]?.ToString() ?? string.Empty,
        Description = reader["description"]?.ToString() ?? string.Empty
      };
    }

    return result;
  }

  private static IReadOnlyDictionary<string, VihkotappeluUnitTranslation> NormalizeUnitTranslations(VihkotappeluUnit unit)
  {
    var translations = new Dictionary<string, VihkotappeluUnitTranslation>(StringComparer.OrdinalIgnoreCase);

    foreach (var pair in unit.Translations)
    {
      var language = pair.Key.Trim().ToLowerInvariant();
      if (language.Length == 0)
      {
        continue;
      }

      translations[language] = new VihkotappeluUnitTranslation
      {
        Name = pair.Value.Name.Trim(),
        Description = pair.Value.Description
      };
    }

    if (!translations.ContainsKey(EnglishLanguage))
    {
      translations[EnglishLanguage] = new VihkotappeluUnitTranslation
      {
        Name = unit.Name,
        Description = unit.Description
      };
    }

    return translations;
  }

  private static IReadOnlyDictionary<string, VihkotappeluFactionTranslation> NormalizeFactionTranslations(VihkotappeluFaction faction)
  {
    var translations = new Dictionary<string, VihkotappeluFactionTranslation>(StringComparer.OrdinalIgnoreCase);

    foreach (var pair in faction.Translations)
    {
      var language = pair.Key.Trim().ToLowerInvariant();
      if (language.Length == 0)
      {
        continue;
      }

      translations[language] = new VihkotappeluFactionTranslation
      {
        Name = pair.Value.Name.Trim()
      };
    }

    if (!translations.ContainsKey(EnglishLanguage))
    {
      translations[EnglishLanguage] = new VihkotappeluFactionTranslation
      {
        Name = faction.Name
      };
    }

    return translations;
  }

  private static VihkotappeluUnit ReadUnit(SqliteDataReader reader)
  {
    return new VihkotappeluUnit
    {
      UnitKey = reader["unitKey"]?.ToString() ?? string.Empty,
      Name = reader["name"]?.ToString() ?? string.Empty,
      Description = reader["description"]?.ToString() ?? string.Empty,
      Hp = Convert.ToInt32(reader["hp"]),
      Attack = Convert.ToInt32(reader["attack"]),
      Defence = Convert.ToInt32(reader["defence"]),
      Movement = Convert.ToInt32(reader["movement"]),
      Range = Convert.ToInt32(reader["range"]),
      MinRange = Convert.ToInt32(reader["minRange"]),
      Sprite = reader["sprite"]?.ToString() ?? string.Empty,
      SortOrder = reader["sortOrder"] is long sortOrder ? (int)sortOrder : Convert.ToInt32(reader["sortOrder"])
    };
  }

  private async Task EnsureOpenConnection()
  {
    if (_connection.State != ConnectionState.Open)
    {
      await _connection.OpenAsync();

      await using var pragma = _connection.CreateCommand();
      pragma.CommandText = "PRAGMA foreign_keys = ON;";
      await pragma.ExecuteNonQueryAsync();
    }
  }
}
