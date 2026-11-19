using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

public record AuthTokenRequest(string? Username, string Password);

public class AuthAdminOptions
{
  public string Username { get; set; } = string.Empty;
  public string Password { get; set; } = string.Empty;
  public string DisplayName { get; set; } = string.Empty;
}

// Site-to-site (machine-to-machine) credentials for service clients such as
// the tarot site. No portal user is created for these; the API key is checked
// directly against configuration by the SiteApiKey authentication scheme.
public class AuthSiteToSiteOptions
{
  public string ApiKey { get; set; } = string.Empty;
}

public class AuthOptions
{
  public string Issuer { get; set; } = "klapi-api";
  public string Audience { get; set; } = "klapi-ui";
  public string SigningKey { get; set; } = string.Empty;
  public List<string> AllowedOrigins { get; set; } = [];
  public AuthAdminOptions Admin { get; set; } = new();
  public AuthSiteToSiteOptions SiteToSite { get; set; } = new();
}

public static class AuthEndpoints
{
  public static void MapAuthEndpoints(WebApplication app)
  {
    app.MapPost("/auth/token", async (
      HttpContext httpContext,
      IOptions<AuthOptions> authOptions,
      UserService userService,
      AuthTokenRequest request) =>
    {
      if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
      {
        return Results.BadRequest(new { Message = "Username and password are required." });
      }

      var options = authOptions.Value;
      var authenticatedUser = await userService.Authenticate(request.Username, request.Password);

      if (authenticatedUser is null)
      {
        return Results.Unauthorized();
      }

      var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey));
      var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
      var claims = new List<Claim>
      {
        new(JwtRegisteredClaimNames.Sub, authenticatedUser.Username),
        new(ClaimTypes.Name, authenticatedUser.Username),
        new("username", authenticatedUser.Username),
        new("display_name", authenticatedUser.DisplayName),
        new("preferred_language", authenticatedUser.PreferredLanguage),
      };

      foreach (var role in authenticatedUser.Roles)
      {
        claims.Add(new Claim(ClaimTypes.Role, role));
      }

      var token = new JwtSecurityToken(
        issuer: options.Issuer,
        audience: options.Audience,
        claims: claims,
        expires: DateTime.UtcNow.AddHours(12),
        signingCredentials: credentials);

      var tokenValue = new JwtSecurityTokenHandler().WriteToken(token);

      return Results.Ok(new
      {
        AccessToken = tokenValue,
        Username = authenticatedUser.Username,
        DisplayName = authenticatedUser.DisplayName,
        PreferredLanguage = authenticatedUser.PreferredLanguage,
        Roles = authenticatedUser.Roles,
        TokenType = "Bearer",
        ExpiresIn = 43200
      });
    })
    .RequireCors("FrontendWriteCors")
    .WithName("CreateAuthToken");
  }
}
