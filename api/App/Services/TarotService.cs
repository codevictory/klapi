using System.Data;
using Microsoft.Data.Sqlite;

public class TarotService
{
  private const string EnglishLanguage = "en";

  private readonly SqliteConnection _connection;

  public TarotService(SqliteConnection connection)
  {
    _connection = connection;
  }

  public async Task<List<TarotCard>> GetAllCards()
  {
    await EnsureOpenConnection();

    var cards = new List<TarotCard>();

    await using (var command = _connection.CreateCommand())
    {
      command.CommandText = @"
        SELECT id, title, arcana, image, description, sortOrder, writtenByAi
        FROM TarotCards
        ORDER BY sortOrder ASC, id ASC;";

      await using var reader = await command.ExecuteReaderAsync();
      while (await reader.ReadAsync())
      {
        cards.Add(ReadCard(reader));
      }
    }

    var translationsByCard = new Dictionary<string, Dictionary<string, TarotCardTranslation>>(StringComparer.OrdinalIgnoreCase);

    await using (var command = _connection.CreateCommand())
    {
      command.CommandText = @"
        SELECT cardId, language, title, arcana, description
        FROM TarotCardTranslations;";

      await using var reader = await command.ExecuteReaderAsync();
      while (await reader.ReadAsync())
      {
        var cardId = reader["cardId"]?.ToString() ?? string.Empty;
        var language = (reader["language"]?.ToString() ?? string.Empty).Trim().ToLowerInvariant();
        if (cardId.Length == 0 || language.Length == 0)
        {
          continue;
        }

        if (!translationsByCard.TryGetValue(cardId, out var translations))
        {
          translations = new Dictionary<string, TarotCardTranslation>(StringComparer.OrdinalIgnoreCase);
          translationsByCard[cardId] = translations;
        }

        translations[language] = new TarotCardTranslation
        {
          Title = reader["title"]?.ToString() ?? string.Empty,
          Arcana = reader["arcana"]?.ToString() ?? string.Empty,
          Description = reader["description"]?.ToString() ?? string.Empty
        };
      }
    }

    foreach (var card in cards)
    {
      if (translationsByCard.TryGetValue(card.Id, out var translations))
      {
        card.Translations = translations;
      }

      // The base columns are the canonical English texts; make sure "en" is
      // always available even when no explicit translation row exists.
      if (!card.Translations.ContainsKey(EnglishLanguage))
      {
        card.Translations[EnglishLanguage] = new TarotCardTranslation
        {
          Title = card.Title,
          Arcana = card.Arcana,
          Description = card.Description
        };
      }
    }

    return cards;
  }

  public async Task<List<TarotCard>> SyncCards(IReadOnlyList<TarotCard> cards)
  {
    await EnsureOpenConnection();

    var now = DateTime.UtcNow.ToString("O");

    using var transaction = _connection.BeginTransaction();

    await using (var upsertCommand = _connection.CreateCommand())
    {
      upsertCommand.Transaction = transaction;
      // writtenByAi is deliberately NOT updated on conflict: the flag is
      // managed per card from the /admin portal, not by the data sync, so a
      // re-sync must not reset it. New cards get the synced value (the model
      // defaults to true when the payload omits the field).
      upsertCommand.CommandText = @"
        INSERT INTO TarotCards (id, title, arcana, image, description, sortOrder, lastUpdated, writtenByAi)
        VALUES (@id, @title, @arcana, @image, @description, @sortOrder, @lastUpdated, @writtenByAi)
        ON CONFLICT(id) DO UPDATE SET
          title = excluded.title,
          arcana = excluded.arcana,
          image = excluded.image,
          description = excluded.description,
          sortOrder = excluded.sortOrder,
          lastUpdated = excluded.lastUpdated;";

      var idParam = upsertCommand.Parameters.Add("@id", SqliteType.Text);
      var titleParam = upsertCommand.Parameters.Add("@title", SqliteType.Text);
      var arcanaParam = upsertCommand.Parameters.Add("@arcana", SqliteType.Text);
      var imageParam = upsertCommand.Parameters.Add("@image", SqliteType.Text);
      var descriptionParam = upsertCommand.Parameters.Add("@description", SqliteType.Text);
      var sortOrderParam = upsertCommand.Parameters.Add("@sortOrder", SqliteType.Integer);
      var lastUpdatedParam = upsertCommand.Parameters.Add("@lastUpdated", SqliteType.Text);
      var writtenByAiParam = upsertCommand.Parameters.Add("@writtenByAi", SqliteType.Integer);

      foreach (var card in cards)
      {
        idParam.Value = card.Id;
        titleParam.Value = card.Title;
        arcanaParam.Value = card.Arcana;
        imageParam.Value = card.Image;
        descriptionParam.Value = card.Description;
        sortOrderParam.Value = card.SortOrder;
        lastUpdatedParam.Value = now;
        writtenByAiParam.Value = card.WrittenByAi ? 1 : 0;
        await upsertCommand.ExecuteNonQueryAsync();
      }
    }

    // Replace the translation rows of every synced card. Cards without any
    // supplied translations still get the canonical English row derived
    // from the base columns. Rows of deleted cards are removed by the
    // ON DELETE CASCADE on TarotCardTranslations.
    await using (var deleteTranslationsCommand = _connection.CreateCommand())
    {
      deleteTranslationsCommand.Transaction = transaction;
      deleteTranslationsCommand.CommandText = "DELETE FROM TarotCardTranslations WHERE cardId = @cardId;";
      var cardIdParam = deleteTranslationsCommand.Parameters.Add("@cardId", SqliteType.Text);

      await using var insertTranslationCommand = _connection.CreateCommand();
      insertTranslationCommand.Transaction = transaction;
      insertTranslationCommand.CommandText = @"
        INSERT INTO TarotCardTranslations (cardId, language, title, arcana, description)
        VALUES (@cardId, @language, @title, @arcana, @description);";
      var insCardIdParam = insertTranslationCommand.Parameters.Add("@cardId", SqliteType.Text);
      var languageParam = insertTranslationCommand.Parameters.Add("@language", SqliteType.Text);
      var titleParam = insertTranslationCommand.Parameters.Add("@title", SqliteType.Text);
      var arcanaParam = insertTranslationCommand.Parameters.Add("@arcana", SqliteType.Text);
      var descriptionParam = insertTranslationCommand.Parameters.Add("@description", SqliteType.Text);

      foreach (var card in cards)
      {
        cardIdParam.Value = card.Id;
        await deleteTranslationsCommand.ExecuteNonQueryAsync();

        var translations = NormalizeTranslations(card);
        foreach (var (language, translation) in translations)
        {
          insCardIdParam.Value = card.Id;
          languageParam.Value = language;
          titleParam.Value = translation.Title;
          arcanaParam.Value = translation.Arcana;
          descriptionParam.Value = translation.Description;
          await insertTranslationCommand.ExecuteNonQueryAsync();
        }
      }
    }

    await using (var deleteCommand = _connection.CreateCommand())
    {
      deleteCommand.Transaction = transaction;

      if (cards.Count == 0)
      {
        deleteCommand.CommandText = "DELETE FROM TarotCards;";
      }
      else
      {
        var idParameters = string.Join(", ", cards.Select((_, index) => $"@id{index}"));
        deleteCommand.CommandText = $"DELETE FROM TarotCards WHERE id NOT IN ({idParameters});";
        for (var index = 0; index < cards.Count; index++)
        {
          deleteCommand.Parameters.AddWithValue($"@id{index}", cards[index].Id);
        }
      }

      await deleteCommand.ExecuteNonQueryAsync();
    }

    transaction.Commit();

    return await GetAllCards();
  }

  // Updates the English title/description (canonical TarotCards columns plus
  // the "en" translation row) and the Finnish title/description ("fi"
  // translation row) of a single card. Returns null when the card does not
  // exist.
  public async Task<TarotCard?> UpdateCardLocalizedTexts(string cardId, TarotCardTextsUpdate texts)
  {
    await EnsureOpenConnection();

    var normalizedId = cardId.Trim();
    var now = DateTime.UtcNow.ToString("O");

    using var transaction = _connection.BeginTransaction();

    await using (var updateCardCommand = _connection.CreateCommand())
    {
      updateCardCommand.Transaction = transaction;
      updateCardCommand.CommandText = @"
        UPDATE TarotCards
        SET title = @title,
            description = @description,
            lastUpdated = @lastUpdated
        WHERE id = @cardId;";

      updateCardCommand.Parameters.AddWithValue("@cardId", normalizedId);
      updateCardCommand.Parameters.AddWithValue("@title", texts.EnglishTitle.Trim());
      updateCardCommand.Parameters.AddWithValue("@description", texts.EnglishDescription);
      updateCardCommand.Parameters.AddWithValue("@lastUpdated", now);

      var affectedRows = await updateCardCommand.ExecuteNonQueryAsync();
      if (affectedRows == 0)
      {
        transaction.Rollback();
        return null;
      }
    }

    // Upsert the localized rows. On insert the arcana is copied from the base
    // row; on conflict only title/description are touched.
    await using (var upsertTranslationCommand = _connection.CreateCommand())
    {
      upsertTranslationCommand.Transaction = transaction;
      upsertTranslationCommand.CommandText = @"
        INSERT INTO TarotCardTranslations (cardId, language, title, arcana, description)
        VALUES (@cardId, @language, @title, (SELECT arcana FROM TarotCards WHERE id = @cardId), @description)
        ON CONFLICT(cardId, language) DO UPDATE SET
          title = excluded.title,
          description = excluded.description;";

      var cardIdParam = upsertTranslationCommand.Parameters.Add("@cardId", SqliteType.Text);
      var languageParam = upsertTranslationCommand.Parameters.Add("@language", SqliteType.Text);
      var titleParam = upsertTranslationCommand.Parameters.Add("@title", SqliteType.Text);
      var descriptionParam = upsertTranslationCommand.Parameters.Add("@description", SqliteType.Text);

      foreach (var (language, title, description) in new[]
      {
        (EnglishLanguage, texts.EnglishTitle, texts.EnglishDescription),
        (AppLanguages.Finnish, texts.FinnishTitle, texts.FinnishDescription)
      })
      {
        cardIdParam.Value = normalizedId;
        languageParam.Value = language;
        titleParam.Value = title.Trim();
        descriptionParam.Value = description;
        await upsertTranslationCommand.ExecuteNonQueryAsync();
      }
    }

    transaction.Commit();

    var cards = await GetAllCards();
    return cards.FirstOrDefault(card => string.Equals(card.Id, normalizedId, StringComparison.OrdinalIgnoreCase));
  }

  // Toggles the "Written by AI" tag of a single card. Returns null when the
  // card does not exist.
  public async Task<TarotCard?> UpdateCardWrittenByAi(string cardId, bool writtenByAi)
  {
    await EnsureOpenConnection();

    var normalizedId = cardId.Trim();
    var now = DateTime.UtcNow.ToString("O");

    await using (var command = _connection.CreateCommand())
    {
      command.CommandText = @"
        UPDATE TarotCards
        SET writtenByAi = @writtenByAi,
            lastUpdated = @lastUpdated
        WHERE id = @cardId;";

      command.Parameters.AddWithValue("@cardId", normalizedId);
      command.Parameters.AddWithValue("@writtenByAi", writtenByAi ? 1 : 0);
      command.Parameters.AddWithValue("@lastUpdated", now);

      var affectedRows = await command.ExecuteNonQueryAsync();
      if (affectedRows == 0)
      {
        return null;
      }
    }

    var cards = await GetAllCards();
    return cards.FirstOrDefault(card => string.Equals(card.Id, normalizedId, StringComparison.OrdinalIgnoreCase));
  }

  private static IReadOnlyDictionary<string, TarotCardTranslation> NormalizeTranslations(TarotCard card)
  {
    var translations = new Dictionary<string, TarotCardTranslation>(StringComparer.OrdinalIgnoreCase);

    foreach (var pair in card.Translations)
    {
      var language = pair.Key.Trim().ToLowerInvariant();
      if (language.Length == 0)
      {
        continue;
      }

      translations[language] = new TarotCardTranslation
      {
        Title = pair.Value.Title.Trim(),
        Arcana = pair.Value.Arcana.Trim(),
        Description = pair.Value.Description
      };
    }

    if (!translations.ContainsKey(EnglishLanguage))
    {
      translations[EnglishLanguage] = new TarotCardTranslation
      {
        Title = card.Title,
        Arcana = card.Arcana,
        Description = card.Description
      };
    }

    return translations;
  }

  private static TarotCard ReadCard(SqliteDataReader reader)
  {
    return new TarotCard
    {
      Id = reader["id"]?.ToString() ?? string.Empty,
      Title = reader["title"]?.ToString() ?? string.Empty,
      Arcana = reader["arcana"]?.ToString() ?? string.Empty,
      Image = reader["image"]?.ToString() ?? string.Empty,
      Description = reader["description"]?.ToString() ?? string.Empty,
      SortOrder = reader["sortOrder"] is long sortOrder ? (int)sortOrder : Convert.ToInt32(reader["sortOrder"]),
      WrittenByAi = reader["writtenByAi"] is long writtenByAi && writtenByAi != 0
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
