using System.Data;
using Microsoft.Data.Sqlite;

public class TarotService
{
  private readonly SqliteConnection _connection;

  public TarotService(SqliteConnection connection)
  {
    _connection = connection;
  }

  public async Task<List<TarotCard>> GetAllCards()
  {
    await EnsureOpenConnection();

    await using var command = _connection.CreateCommand();
    command.CommandText = @"
      SELECT id, title, arcana, image, description, sortOrder
      FROM TarotCards
      ORDER BY sortOrder ASC, id ASC;";

    await using var reader = await command.ExecuteReaderAsync();

    var cards = new List<TarotCard>();
    while (await reader.ReadAsync())
    {
      cards.Add(ReadCard(reader));
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
      upsertCommand.CommandText = @"
        INSERT INTO TarotCards (id, title, arcana, image, description, sortOrder, lastUpdated)
        VALUES (@id, @title, @arcana, @image, @description, @sortOrder, @lastUpdated)
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

      foreach (var card in cards)
      {
        idParam.Value = card.Id;
        titleParam.Value = card.Title;
        arcanaParam.Value = card.Arcana;
        imageParam.Value = card.Image;
        descriptionParam.Value = card.Description;
        sortOrderParam.Value = card.SortOrder;
        lastUpdatedParam.Value = now;
        await upsertCommand.ExecuteNonQueryAsync();
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

  private static TarotCard ReadCard(SqliteDataReader reader)
  {
    return new TarotCard
    {
      Id = reader["id"]?.ToString() ?? string.Empty,
      Title = reader["title"]?.ToString() ?? string.Empty,
      Arcana = reader["arcana"]?.ToString() ?? string.Empty,
      Image = reader["image"]?.ToString() ?? string.Empty,
      Description = reader["description"]?.ToString() ?? string.Empty,
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
