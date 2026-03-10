public static class AppRoles
{
  public const string Lok = "lok";
  public const string Admin = "admin";

  public static readonly IReadOnlyList<string> All = [Lok, Admin];
}

public class AppUser
{
  public long Id { get; set; }

  public string Username { get; set; } = string.Empty;

  public string Password { get; set; } = string.Empty;

  public DateTime Added { get; set; }

  public DateTime LastUpdated { get; set; }

  public string DisplayName { get; set; } = string.Empty;

  public List<string> Roles { get; set; } = [];
}

public class AppUserView
{
  public string Username { get; set; } = string.Empty;

  public DateTime Added { get; set; }

  public DateTime LastUpdated { get; set; }

  public string DisplayName { get; set; } = string.Empty;

  public List<string> Roles { get; set; } = [];
}

public class AppUserCreateRequest
{
  public string Username { get; set; } = string.Empty;

  public string Password { get; set; } = string.Empty;

  public string DisplayName { get; set; } = string.Empty;

  public List<string> Roles { get; set; } = [];
}

public class AppUserUpdateRequest
{
  public string? Password { get; set; }

  public string DisplayName { get; set; } = string.Empty;

  public List<string> Roles { get; set; } = [];
}
