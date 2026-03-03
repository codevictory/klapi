public class AppUser
{
  public long Id { get; set; }

  public string Username { get; set; } = string.Empty;

  public string Password { get; set; } = string.Empty;

  public DateTime Added { get; set; }

  public DateTime LastUpdated { get; set; }

  public bool IsAdmin { get; set; }

  public string DisplayName { get; set; } = string.Empty;
}

public class AppUserView
{
  public string Username { get; set; } = string.Empty;

  public DateTime Added { get; set; }

  public DateTime LastUpdated { get; set; }

  public bool IsAdmin { get; set; }

  public string DisplayName { get; set; } = string.Empty;
}

public class AppUserCreateRequest
{
  public string Username { get; set; } = string.Empty;

  public string Password { get; set; } = string.Empty;

  public bool IsAdmin { get; set; }

  public string DisplayName { get; set; } = string.Empty;
}

public class AppUserUpdateRequest
{
  public string? Password { get; set; }

  public bool IsAdmin { get; set; }

  public string DisplayName { get; set; } = string.Empty;
}
