using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

// Development-only authentication scheme: every request is treated as an
// authenticated user holding all roles, so local development and tests do not
// need to obtain a JWT. Never registered in production — there the default
// scheme is JWT bearer and unauthenticated requests are rejected.
public class DevelopmentAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
  public const string AuthenticationScheme = "Development";

  public DevelopmentAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : base(options, logger, encoder)
  {
  }

  protected override Task<AuthenticateResult> HandleAuthenticateAsync()
  {
    var claims = new List<Claim>
    {
      new(ClaimTypes.Name, "dev"),
      new("username", "dev"),
      new("display_name", "Development User"),
      new("preferred_language", AppLanguages.Finnish)
    };

    foreach (var role in AppRoles.All)
    {
      claims.Add(new Claim(ClaimTypes.Role, role));
    }

    var identity = new ClaimsIdentity(claims, AuthenticationScheme);
    var principal = new ClaimsPrincipal(identity);
    var ticket = new AuthenticationTicket(principal, AuthenticationScheme);

    return Task.FromResult(AuthenticateResult.Success(ticket));
  }
}
