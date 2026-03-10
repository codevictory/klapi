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
    var normalizedUsername = admin.Username.Trim().ToLowerInvariant();

    await using var upsertCommand = _connection.CreateCommand();
    upsertCommand.CommandText = @"
      INSERT INTO Users (username, password, added, lastUpdated, displayName)
      VALUES (@username, @password, @added, @lastUpdated, @displayName)
      ON CONFLICT(username) DO UPDATE SET
        password = excluded.password,
        lastUpdated = excluded.lastUpdated,
        displayName = excluded.displayName;
      SELECT id FROM Users WHERE username = @username;";

    upsertCommand.Parameters.AddWithValue("@username", normalizedUsername);
    upsertCommand.Parameters.AddWithValue("@password", admin.Password);
    upsertCommand.Parameters.AddWithValue("@added", now.ToString("O"));
    upsertCommand.Parameters.AddWithValue("@lastUpdated", now.ToString("O"));
    upsertCommand.Parameters.AddWithValue("@displayName", admin.DisplayName.Trim());

    var userId = Convert.ToInt64(await upsertCommand.ExecuteScalarAsync());

    await using var roleCommand = _connection.CreateCommand();
    roleCommand.CommandText = @"
      INSERT OR IGNORE INTO UserRoles (userId, roleName) VALUES (@userId, @roleName);";
    roleCommand.Parameters.AddWithValue("@userId", userId);
    roleCommand.Parameters.AddWithValue("@roleName", AppRoles.Admin);
    await roleCommand.ExecuteNonQueryAsync();
  }

  public async Task<AppUser?> Authenticate(string username, string password)
  {
    await EnsureOpenConnection();

    await using var command = _connection.CreateCommand();
    command.CommandText = @"
      SELECT u.id, u.username, u.password, u.added, u.lastUpdated, u.displayName,
             GROUP_CONCAT(ur.roleName) AS roles
      FROM Users u
      LEFT JOIN UserRoles ur ON ur.userId = u.id
      WHERE u.username = @username
      GROUP BY u.id
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
      SELECT u.id, u.username, u.added, u.lastUpdated, u.displayName,
             GROUP_CONCAT(ur.roleName) AS roles
      FROM Users u
      LEFT JOIN UserRoles ur ON ur.userId = u.id
      GROUP BY u.id
      ORDER BY u.username ASC;";

    await using var reader = await command.ExecuteReaderAsync();

    var users = new List<AppUserView>();
    while (await reader.ReadAsync())
    {
      users.Add(new AppUserView
      {
        Username = reader["username"]?.ToString() ?? string.Empty,
        Added = ParseDate(reader["added"]?.ToString()),
        LastUpdated = ParseDate(reader["lastUpdated"]?.ToString()),
        DisplayName = reader["displayName"]?.ToString() ?? string.Empty,
        Roles = ParseRoles(reader["roles"]?.ToString())
      });
    }

    return users;
  }

  public async Task<AppUserView> CreateUser(AppUserCreateRequest request)
  {
    await EnsureOpenConnection();

    var now = DateTime.UtcNow;
    var normalizedUsername = request.Username.Trim().ToLowerInvariant();

    await using var insertCommand = _connection.CreateCommand();
    insertCommand.CommandText = @"
      INSERT INTO Users (username, password, added, lastUpdated, displayName)
      VALUES (@username, @password, @added, @lastUpdated, @displayName);
      SELECT last_insert_rowid();";

    insertCommand.Parameters.AddWithValue("@username", normalizedUsername);
    insertCommand.Parameters.AddWithValue("@password", request.Password);
    insertCommand.Parameters.AddWithValue("@added", now.ToString("O"));
    insertCommand.Parameters.AddWithValue("@lastUpdated", now.ToString("O"));
    insertCommand.Parameters.AddWithValue("@displayName", request.DisplayName.Trim());

    var insertedId = Convert.ToInt64(await insertCommand.ExecuteScalarAsync());

    var validRoles = request.Roles
      .Select(r => r.Trim().ToLowerInvariant())
      .Where(r => AppRoles.All.Contains(r))
      .ToList();

    if (validRoles.Count > 0)
    {
      await SetUserRoles(insertedId, validRoles);
    }

    return new AppUserView
    {
      Username = normalizedUsername,
      Added = now,
      LastUpdated = now,
      DisplayName = request.DisplayName.Trim(),
      Roles = validRoles
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
          displayName = @displayName
      WHERE username = @username;";

    command.Parameters.AddWithValue("@username", normalizedUsername);
    command.Parameters.AddWithValue("@password", string.IsNullOrWhiteSpace(request.Password) ? currentUser.Password : request.Password);
    command.Parameters.AddWithValue("@lastUpdated", now.ToString("O"));
    command.Parameters.AddWithValue("@displayName", request.DisplayName.Trim());

    var affectedRows = await command.ExecuteNonQueryAsync();
    if (affectedRows == 0)
    {
      return null;
    }

    var validRoles = request.Roles
      .Select(r => r.Trim().ToLowerInvariant())
      .Where(r => AppRoles.All.Contains(r))
      .ToList();

    await SetUserRoles(currentUser.Id, validRoles);

    return new AppUserView
    {
      Username = normalizedUsername,
      Added = currentUser.Added,
      LastUpdated = now,
      DisplayName = request.DisplayName.Trim(),
      Roles = validRoles
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

  public async Task<int> GetUsersWithRoleCount(string roleName)
  {
    await EnsureOpenConnection();

    await using var command = _connection.CreateCommand();
    command.CommandText = "SELECT COUNT(*) FROM UserRoles WHERE roleName = @roleName;";
    command.Parameters.AddWithValue("@roleName", roleName.Trim().ToLowerInvariant());

    return Convert.ToInt32(await command.ExecuteScalarAsync());
  }

  public async Task<AppUserView?> GetUser(string username)
  {
    await EnsureOpenConnection();

    await using var command = _connection.CreateCommand();
    command.CommandText = @"
      SELECT u.id, u.username, u.added, u.lastUpdated, u.displayName,
             GROUP_CONCAT(ur.roleName) AS roles
      FROM Users u
      LEFT JOIN UserRoles ur ON ur.userId = u.id
      WHERE u.username = @username
      GROUP BY u.id
      LIMIT 1;";

    command.Parameters.AddWithValue("@username", username.Trim().ToLowerInvariant());

    await using var reader = await command.ExecuteReaderAsync();
    if (!await reader.ReadAsync())
    {
      return null;
    }

    return new AppUserView
    {
      Username = reader["username"]?.ToString() ?? string.Empty,
      Added = ParseDate(reader["added"]?.ToString()),
      LastUpdated = ParseDate(reader["lastUpdated"]?.ToString()),
      DisplayName = reader["displayName"]?.ToString() ?? string.Empty,
      Roles = ParseRoles(reader["roles"]?.ToString())
    };
  }

  private async Task SetUserRoles(long userId, IReadOnlyList<string> roleNames)
  {
    await using var deleteCommand = _connection.CreateCommand();
    deleteCommand.CommandText = "DELETE FROM UserRoles WHERE userId = @userId;";
    deleteCommand.Parameters.AddWithValue("@userId", userId);
    await deleteCommand.ExecuteNonQueryAsync();

    foreach (var roleName in roleNames)
    {
      await using var insertCommand = _connection.CreateCommand();
      insertCommand.CommandText = "INSERT OR IGNORE INTO UserRoles (userId, roleName) VALUES (@userId, @roleName);";
      insertCommand.Parameters.AddWithValue("@userId", userId);
      insertCommand.Parameters.AddWithValue("@roleName", roleName);
      await insertCommand.ExecuteNonQueryAsync();
    }
  }

  private async Task<AppUser?> GetUserByUsername(string username)
  {
    await using var command = _connection.CreateCommand();
    command.CommandText = @"
      SELECT u.id, u.username, u.password, u.added, u.lastUpdated, u.displayName,
             GROUP_CONCAT(ur.roleName) AS roles
      FROM Users u
      LEFT JOIN UserRoles ur ON ur.userId = u.id
      WHERE u.username = @username
      GROUP BY u.id
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
      DisplayName = reader["displayName"]?.ToString() ?? string.Empty,
      Roles = ParseRoles(reader["roles"]?.ToString())
    };
  }

  private static List<string> ParseRoles(string? value)
  {
    if (string.IsNullOrEmpty(value)) return [];
    return [.. value.Split(',').Where(r => !string.IsNullOrWhiteSpace(r))];
  }

  private static DateTime ParseDate(string? value)
  {
    if (!string.IsNullOrWhiteSpace(value) && DateTime.TryParse(value, out var parsed))
    {
      return parsed;
    }

    return DateTime.MinValue;
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
