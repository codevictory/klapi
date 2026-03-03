using System.Data;
using Microsoft.Data.Sqlite;

public class UserService
{
  private readonly SqliteConnection _connection;

  public UserService(SqliteConnection connection)
  {
    _connection = connection;
  }

  public async Task EnsureAdminUser(AuthAdminOptions admin)
  {
    await EnsureOpenConnection();

    var now = DateTime.UtcNow;

    await using var command = _connection.CreateCommand();
    command.CommandText = @"
      INSERT INTO Users (username, password, added, lastUpdated, isAdmin, displayName)
      VALUES (@username, @password, @added, @lastUpdated, @isAdmin, @displayName)
      ON CONFLICT(username) DO UPDATE SET
        password = excluded.password,
        lastUpdated = excluded.lastUpdated,
        isAdmin = 1,
        displayName = excluded.displayName;";

    command.Parameters.AddWithValue("@username", admin.Username.Trim().ToLowerInvariant());
    command.Parameters.AddWithValue("@password", admin.Password);
    command.Parameters.AddWithValue("@added", now.ToString("O"));
    command.Parameters.AddWithValue("@lastUpdated", now.ToString("O"));
    command.Parameters.AddWithValue("@isAdmin", 1);
    command.Parameters.AddWithValue("@displayName", admin.DisplayName.Trim());

    await command.ExecuteNonQueryAsync();
  }

  public async Task<AppUser?> Authenticate(string username, string password)
  {
    await EnsureOpenConnection();

    await using var command = _connection.CreateCommand();
    command.CommandText = @"
      SELECT id, username, password, added, lastUpdated, isAdmin, displayName
      FROM Users
      WHERE username = @username
      LIMIT 1;";

    command.Parameters.AddWithValue("@username", username.Trim().ToLowerInvariant());

    await using var reader = await command.ExecuteReaderAsync();
    if (!await reader.ReadAsync())
    {
      return null;
    }

    var user = ReadUser(reader);

    if (!string.Equals(user.Password, password, StringComparison.Ordinal))
    {
      return null;
    }

    return user;
  }

  public async Task<List<AppUserView>> GetUsers()
  {
    await EnsureOpenConnection();

    await using var command = _connection.CreateCommand();
    command.CommandText = @"
      SELECT username, added, lastUpdated, isAdmin, displayName
      FROM Users
      ORDER BY username ASC;";

    await using var reader = await command.ExecuteReaderAsync();

    var users = new List<AppUserView>();
    while (await reader.ReadAsync())
    {
      users.Add(new AppUserView
      {
        Username = reader["username"]?.ToString() ?? string.Empty,
        Added = ParseDate(reader["added"]?.ToString()),
        LastUpdated = ParseDate(reader["lastUpdated"]?.ToString()),
        IsAdmin = ParseBoolean(reader["isAdmin"]),
        DisplayName = reader["displayName"]?.ToString() ?? string.Empty
      });
    }

    return users;
  }

  public async Task<AppUserView> CreateUser(AppUserCreateRequest request)
  {
    await EnsureOpenConnection();

    var now = DateTime.UtcNow;
    var normalizedUsername = request.Username.Trim().ToLowerInvariant();

    await using var command = _connection.CreateCommand();
    command.CommandText = @"
      INSERT INTO Users (username, password, added, lastUpdated, isAdmin, displayName)
      VALUES (@username, @password, @added, @lastUpdated, @isAdmin, @displayName);";

    command.Parameters.AddWithValue("@username", normalizedUsername);
    command.Parameters.AddWithValue("@password", request.Password);
    command.Parameters.AddWithValue("@added", now.ToString("O"));
    command.Parameters.AddWithValue("@lastUpdated", now.ToString("O"));
    command.Parameters.AddWithValue("@isAdmin", request.IsAdmin ? 1 : 0);
    command.Parameters.AddWithValue("@displayName", request.DisplayName.Trim());

    await command.ExecuteNonQueryAsync();

    return new AppUserView
    {
      Username = normalizedUsername,
      Added = now,
      LastUpdated = now,
      IsAdmin = request.IsAdmin,
      DisplayName = request.DisplayName.Trim()
    };
  }

  public async Task<AppUserView?> UpdateUser(string username, AppUserUpdateRequest request)
  {
    await EnsureOpenConnection();

    var normalizedUsername = username.Trim().ToLowerInvariant();
    var now = DateTime.UtcNow;

    var currentUser = await GetUserByUsername(normalizedUsername);
    if (currentUser is null)
    {
      return null;
    }

    await using var command = _connection.CreateCommand();
    command.CommandText = @"
      UPDATE Users
      SET password = @password,
          lastUpdated = @lastUpdated,
          isAdmin = @isAdmin,
          displayName = @displayName
      WHERE username = @username;";

    command.Parameters.AddWithValue("@username", normalizedUsername);
    command.Parameters.AddWithValue("@password", string.IsNullOrWhiteSpace(request.Password) ? currentUser.Password : request.Password);
    command.Parameters.AddWithValue("@lastUpdated", now.ToString("O"));
    command.Parameters.AddWithValue("@isAdmin", request.IsAdmin ? 1 : 0);
    command.Parameters.AddWithValue("@displayName", request.DisplayName.Trim());

    var affectedRows = await command.ExecuteNonQueryAsync();
    if (affectedRows == 0)
    {
      return null;
    }

    return new AppUserView
    {
      Username = normalizedUsername,
      Added = currentUser.Added,
      LastUpdated = now,
      IsAdmin = request.IsAdmin,
      DisplayName = request.DisplayName.Trim()
    };
  }

  public async Task<bool> DeleteUser(string username)
  {
    await EnsureOpenConnection();

    await using var command = _connection.CreateCommand();
    command.CommandText = "DELETE FROM Users WHERE username = @username;";
    command.Parameters.AddWithValue("@username", username.Trim().ToLowerInvariant());

    var affectedRows = await command.ExecuteNonQueryAsync();
    return affectedRows > 0;
  }

  public async Task<int> GetAdminCount()
  {
    await EnsureOpenConnection();

    await using var command = _connection.CreateCommand();
    command.CommandText = "SELECT COUNT(*) FROM Users WHERE isAdmin = 1;";

    return Convert.ToInt32(await command.ExecuteScalarAsync());
  }

  public async Task<AppUserView?> GetUser(string username)
  {
    await EnsureOpenConnection();

    var user = await GetUserByUsername(username.Trim().ToLowerInvariant());
    if (user is null)
    {
      return null;
    }

    return new AppUserView
    {
      Username = user.Username,
      Added = user.Added,
      LastUpdated = user.LastUpdated,
      IsAdmin = user.IsAdmin,
      DisplayName = user.DisplayName
    };
  }

  private async Task<AppUser?> GetUserByUsername(string username)
  {
    await using var command = _connection.CreateCommand();
    command.CommandText = @"
      SELECT id, username, password, added, lastUpdated, isAdmin, displayName
      FROM Users
      WHERE username = @username
      LIMIT 1;";

    command.Parameters.AddWithValue("@username", username);

    await using var reader = await command.ExecuteReaderAsync();
    if (!await reader.ReadAsync())
    {
      return null;
    }

    return ReadUser(reader);
  }

  private static AppUser ReadUser(SqliteDataReader reader)
  {
    return new AppUser
    {
      Id = reader["id"] is long id ? id : Convert.ToInt64(reader["id"]),
      Username = reader["username"]?.ToString() ?? string.Empty,
      Password = reader["password"]?.ToString() ?? string.Empty,
      Added = ParseDate(reader["added"]?.ToString()),
      LastUpdated = ParseDate(reader["lastUpdated"]?.ToString()),
      IsAdmin = ParseBoolean(reader["isAdmin"]),
      DisplayName = reader["displayName"]?.ToString() ?? string.Empty
    };
  }

  private static DateTime ParseDate(string? value)
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

  private async Task EnsureOpenConnection()
  {
    if (_connection.State != ConnectionState.Open)
    {
      await _connection.OpenAsync();
    }
  }
}
