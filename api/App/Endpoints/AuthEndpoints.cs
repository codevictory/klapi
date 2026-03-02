using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

public record AuthTokenRequest(string Email, string Password);

public class AuthUser
{
  public string Email { get; set; } = string.Empty;
  public string Password { get; set; } = string.Empty;
}

public class AuthOptions
{
  public string Issuer { get; set; } = "klapi-api";
  public string Audience { get; set; } = "klapi-ui";
  public string SigningKey { get; set; } = string.Empty;
  public List<string> AllowedOrigins { get; set; } = [];
  public List<AuthUser> Users { get; set; } = [];
}

public static class AuthEndpoints
{
  public static void MapAuthEndpoints(WebApplication app)
  {
    app.MapPost("/auth/token", (
      HttpContext httpContext,
      IOptions<AuthOptions> authOptions,
      AuthTokenRequest request) =>
    {
      if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
      {
        return Results.BadRequest(new { Message = "Email and password are required." });
      }

      var options = authOptions.Value;
      var user = options.Users.FirstOrDefault(item =>
        string.Equals(item.Email, request.Email.Trim(), StringComparison.OrdinalIgnoreCase));

      if (user is null || !string.Equals(user.Password, request.Password, StringComparison.Ordinal))
      {
        return Results.Unauthorized();
      }

      var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey));
      var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
      var claims = new List<Claim>
      {
        new(JwtRegisteredClaimNames.Sub, user.Email),
        new(JwtRegisteredClaimNames.Email, user.Email),
        new(ClaimTypes.Name, user.Email),
        new("scope", "openhours:write")
      };

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
        Email = user.Email,
        TokenType = "Bearer",
        ExpiresIn = 43200
      });
    })
    .RequireCors("FrontendWriteCors")
    .WithName("CreateAuthToken");
  }
}
