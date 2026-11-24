using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

// Site-to-site (machine-to-machine) authentication: the client sends a shared
// API key in the X-API-Key header, validated against Auth:SiteToSite:ApiKey
// (env var KLAPI_SITE_API_KEY in production). Successful principals get the
// machine-only site roles ("tarot", "vihkotappelu") — one per consuming site,
// so each site's backend endpoints stay scoped to that site. The key is
// compared as a SHA-256 hash in fixed time to avoid timing leaks. No portal
// user exists for site-to-site clients.
public class SiteApiKeyAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
  public const string AuthenticationScheme = "SiteApiKey";
  public const string ApiKeyHeaderName = "X-API-Key";

  private readonly AuthOptions _authOptions;

  public SiteApiKeyAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IOptions<AuthOptions> authOptions)
    : base(options, logger, encoder)
  {
    _authOptions = authOptions.Value;
  }

  protected override Task<AuthenticateResult> HandleAuthenticateAsync()
  {
    var configuredKey = _authOptions.SiteToSite.ApiKey;

    if (string.IsNullOrWhiteSpace(configuredKey))
    {
      return Task.FromResult(AuthenticateResult.NoResult());
    }

    if (!Request.Headers.TryGetValue(ApiKeyHeaderName, out var providedKey)
        || string.IsNullOrWhiteSpace(providedKey))
    {
      return Task.FromResult(AuthenticateResult.NoResult());
    }

    if (!FixedTimeEquals(providedKey.ToString(), configuredKey))
    {
      return Task.FromResult(AuthenticateResult.Fail("Invalid site-to-site API key."));
    }

    var claims = new List<Claim>
    {
      new(ClaimTypes.Name, "site-to-site"),
      new("username", "site-to-site"),
      new(ClaimTypes.Role, AppRoles.Tarot),
      new(ClaimTypes.Role, AppRoles.Vihkotappelu)
    };

    var identity = new ClaimsIdentity(claims, AuthenticationScheme);
    var principal = new ClaimsPrincipal(identity);
    var ticket = new AuthenticationTicket(principal, AuthenticationScheme);

    return Task.FromResult(AuthenticateResult.Success(ticket));
  }

  private static bool FixedTimeEquals(string provided, string configured)
  {
    var providedHash = SHA256.HashData(Encoding.UTF8.GetBytes(provided));
    var configuredHash = SHA256.HashData(Encoding.UTF8.GetBytes(configured));
    return CryptographicOperations.FixedTimeEquals(providedHash, configuredHash);
  }
}
