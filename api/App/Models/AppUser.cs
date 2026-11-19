public static class AppRoles
{
  public const string Lok = "lok";
  public const string Admin = "admin";

  // Machine role granted only to site-to-site API key principals; not
  // assignable to portal users.
  public const string Tarot = "tarot";

  public static readonly IReadOnlyList<string> All = [Lok, Admin];
}

public static class AppLanguages
{
  public const string Finnish = "fi";
  public const string English = "en";
  public const string Slovak = "sk";

  public static readonly IReadOnlyList<string> All = [Finnish, English, Slovak];

  public static string NormalizeOrDefault(string? language)
  {
    var normalized = language?.Trim().ToLowerInvariant();
    return !string.IsNullOrWhiteSpace(normalized) && All.Contains(normalized)
      ? normalized
      : Finnish;
  }
}

public class AppUser
{
  public long Id { get; set; }

  public string Username { get; set; } = string.Empty;

  public string Password { get; set; } = string.Empty;

  public DateTime Added { get; set; }

  public DateTime LastUpdated { get; set; }

  public string DisplayName { get; set; } = string.Empty;

  public string PreferredLanguage { get; set; } = AppLanguages.Finnish;

  public List<string> Roles { get; set; } = [];
}

public class AppUserView
{
  public string Username { get; set; } = string.Empty;

  public DateTime Added { get; set; }

  public DateTime LastUpdated { get; set; }

  public string DisplayName { get; set; } = string.Empty;

  public string PreferredLanguage { get; set; } = AppLanguages.Finnish;

  public List<string> Roles { get; set; } = [];
}

public class AppUserCreateRequest
{
  public string Username { get; set; } = string.Empty;

  public string Password { get; set; } = string.Empty;

  public string DisplayName { get; set; } = string.Empty;

  public string PreferredLanguage { get; set; } = AppLanguages.Finnish;

  public List<string> Roles { get; set; } = [];
}

public class AppUserUpdateRequest
{
  public string? Password { get; set; }

  public string DisplayName { get; set; } = string.Empty;

  public string PreferredLanguage { get; set; } = AppLanguages.Finnish;

  public List<string> Roles { get; set; } = [];
}
