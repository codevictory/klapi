using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace App.Tests;

// Both test classes share one collection so they never run in parallel:
// WebApplicationFactory applies per-factory configuration through the deferred
// host builder, which is not safe when two factories start concurrently in the
// same process (config can leak and tests would hit the real klapi.db).
[CollectionDefinition("ApiTests", DisableParallelization = true)]
public class ApiTestsCollection;

[Collection("ApiTests")]
public class ApiEndpointsTests(DevelopmentApiTestFactory factory) : IClassFixture<DevelopmentApiTestFactory>
{
  private readonly HttpClient _client = factory.CreateClient();

  [Fact]
  public async Task GetVersion_ReturnsVersion()
  {
    var response = await _client.GetAsync("/");

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
    Assert.Equal("1.0.0", body.RootElement.GetProperty("version").GetString());
  }

  [Fact]
  public async Task HealthDb_ReturnsOk()
  {
    var response = await _client.GetAsync("/health/db");

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
    Assert.Equal("ok", body.RootElement.GetProperty("database").GetString());
    Assert.Equal(1, body.RootElement.GetProperty("result").GetInt32());
  }

  [Fact]
  public async Task OpenHours_Crud_Works_WithoutAuthInDevelopment()
  {
    var createPayload = new
    {
      id = 0,
      name = "test-version",
      version = DateTime.UtcNow.ToString("O"),
      paragraph1 = "p1",
      paragraph2 = "p2",
      paragraph3 = "p3",
      paragraph4 = "p4",
      kitchenNotice = "k1"
    };

    var createResponse = await _client.PostAsJsonAsync("/lok/open-hours", createPayload);

    Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
    Assert.Equal("/lok/open-hours", createResponse.Headers.Location?.ToString());

    var created = await createResponse.Content.ReadFromJsonAsync<LokOpenHoursDto>();
    Assert.NotNull(created);
    Assert.True(created.Id > 0);
    Assert.Equal(createPayload.name, created.Name);

    var getResponse = await _client.GetAsync("/lok/open-hours");
    Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

    var openHours = await getResponse.Content.ReadFromJsonAsync<List<LokOpenHoursDto>>();
    Assert.NotNull(openHours);
    Assert.Contains(openHours, item => item.Id == created.Id);
    Assert.True(openHours.Count(item => item.IsActive) <= 1);

    var createSecondPayload = new
    {
      id = 0,
      name = "test-version-2",
      version = DateTime.UtcNow.ToString("O"),
      paragraph1 = "p1b",
      paragraph2 = "p2b",
      paragraph3 = "p3b",
      paragraph4 = "p4b",
      kitchenNotice = "k1b"
    };

    var createSecondResponse = await _client.PostAsJsonAsync("/lok/open-hours", createSecondPayload);
    Assert.Equal(HttpStatusCode.Created, createSecondResponse.StatusCode);

    var createdSecond = await createSecondResponse.Content.ReadFromJsonAsync<LokOpenHoursDto>();
    Assert.NotNull(createdSecond);
    Assert.True(createdSecond.IsActive);

    var setActiveResponse = await _client.PutAsync($"/lok/open-hours/{created.Id}/active", null);
    Assert.Equal(HttpStatusCode.OK, setActiveResponse.StatusCode);

    var updatePayload = new
    {
      id = created.Id,
      name = "updated-version",
      version = DateTime.UtcNow.ToString("O"),
      paragraph1 = "updated-p1",
      paragraph2 = "updated-p2",
      paragraph3 = "updated-p3",
      paragraph4 = "updated-p4",
      kitchenNotice = "updated-k1"
    };

    var updateResponse = await _client.PutAsJsonAsync($"/lok/open-hours/{created.Id}", updatePayload);
    Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

    var updated = await updateResponse.Content.ReadFromJsonAsync<LokOpenHoursDto>();
    Assert.NotNull(updated);
    Assert.Equal(created.Id, updated.Id);
    Assert.Equal(updatePayload.name, updated.Name);
    Assert.Equal(updatePayload.paragraph1, updated.Paragraph1);

    var getAfterUpdateResponse = await _client.GetAsync("/lok/open-hours");
    Assert.Equal(HttpStatusCode.OK, getAfterUpdateResponse.StatusCode);

    var openHoursAfterUpdate = await getAfterUpdateResponse.Content.ReadFromJsonAsync<List<LokOpenHoursDto>>();
    Assert.NotNull(openHoursAfterUpdate);
    var updatedInList = openHoursAfterUpdate.Single(item => item.Id == created.Id);
    Assert.Equal(updatePayload.name, updatedInList.Name);
    Assert.Equal(updatePayload.paragraph4, updatedInList.Paragraph4);

    var activeCount = openHoursAfterUpdate.Count(item => item.IsActive);
    Assert.Equal(1, activeCount);
    Assert.False(openHoursAfterUpdate.Single(item => item.Id == createdSecond.Id).IsActive);
    Assert.True(openHoursAfterUpdate.Single(item => item.Id == created.Id).IsActive);

    var deleteResponse = await _client.DeleteAsync($"/lok/open-hours/{created.Id}");
    Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

    var deleteAgainResponse = await _client.DeleteAsync($"/lok/open-hours/{created.Id}");
    Assert.Equal(HttpStatusCode.NotFound, deleteAgainResponse.StatusCode);
  }

  [Fact]
  public async Task TarotCards_RequireSiteApiKey_InDevelopment()
  {
    var getWithoutKey = await _client.GetAsync("/tarot/cards");
    Assert.Equal(HttpStatusCode.Unauthorized, getWithoutKey.StatusCode);

    var syncWithoutKey = await _client.PutAsJsonAsync("/tarot/cards", new[]
    {
      new { id = "fool", title = "The Fool", arcana = "Major Arcana", image = "cards/fool.jpg", description = "desc", sortOrder = 0 }
    });
    Assert.Equal(HttpStatusCode.Unauthorized, syncWithoutKey.StatusCode);

    using var wrongKeyClient = factory.CreateClient();
    wrongKeyClient.DefaultRequestHeaders.Add("X-API-Key", "wrong-key-wrong-key-wrong-key-00");
    var getWrongKey = await wrongKeyClient.GetAsync("/tarot/cards");
    Assert.Equal(HttpStatusCode.Unauthorized, getWrongKey.StatusCode);
  }

  [Fact]
  public async Task TarotCards_SiteApiKey_SyncAndGetAll_InDevelopment()
  {
    using var client = factory.CreateClient();
    client.DefaultRequestHeaders.Add("X-API-Key", ApiTestFactoryBase.SiteApiKey);

    var syncPayload = new object[]
    {
      new
      {
        id = "fool", title = "The Fool", arcana = "Major Arcana", image = "cards/fool.jpg", description = "A leap of faith.", sortOrder = 0,
        translations = new
        {
          en = new { title = "The Fool", arcana = "Major Arcana", description = "A leap of faith." },
          fi = new { title = "Narri", arcana = "Iso arkana", description = "Uskon hyppy tuntemattomaan." }
        }
      },
      new { id = "magician", title = "The Magician", arcana = "Major Arcana", image = "cards/magician.jpg", description = "Manifestation.", sortOrder = 1 }
    };

    var syncResponse = await client.PutAsJsonAsync("/tarot/cards", syncPayload);
    Assert.Equal(HttpStatusCode.OK, syncResponse.StatusCode);

    var getResponse = await client.GetAsync("/tarot/cards");
    Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

    var cards = await getResponse.Content.ReadFromJsonAsync<List<TarotCardDto>>();
    Assert.NotNull(cards);
    Assert.Equal(2, cards.Count);
    Assert.Equal("fool", cards[0].Id);
    Assert.Equal("A leap of faith.", cards[0].Description);
    Assert.Equal("magician", cards[1].Id);
    Assert.Equal(1, cards[1].SortOrder);

    // Supplied translations round-trip; a card without translations still
    // gets the canonical English row derived from the base columns.
    Assert.True(cards[0].Translations.ContainsKey("fi"));
    Assert.Equal("Narri", cards[0].Translations["fi"].Title);
    Assert.Equal("Iso arkana", cards[0].Translations["fi"].Arcana);
    Assert.Equal("Uskon hyppy tuntemattomaan.", cards[0].Translations["fi"].Description);
    Assert.True(cards[1].Translations.ContainsKey("en"));
    Assert.Equal("The Magician", cards[1].Translations["en"].Title);
    Assert.False(cards[1].Translations.ContainsKey("fi"));

    var syncAgainPayload = new[]
    {
      new { id = "fool", title = "The Fool", arcana = "Major Arcana", image = "cards/fool.jpg", description = "Updated description.", sortOrder = 0 }
    };

    var syncAgainResponse = await client.PutAsJsonAsync("/tarot/cards", syncAgainPayload);
    Assert.Equal(HttpStatusCode.OK, syncAgainResponse.StatusCode);

    var getAfterSyncResponse = await client.GetAsync("/tarot/cards");
    var cardsAfterSync = await getAfterSyncResponse.Content.ReadFromJsonAsync<List<TarotCardDto>>();
    Assert.NotNull(cardsAfterSync);
    Assert.Single(cardsAfterSync);
    Assert.Equal("Updated description.", cardsAfterSync[0].Description);
    // Re-syncing without translations replaces the old rows: only the
    // canonical English backfill remains.
    Assert.False(cardsAfterSync[0].Translations.ContainsKey("fi"));
    Assert.Equal("The Fool", cardsAfterSync[0].Translations["en"].Title);

    var emptySyncResponse = await client.PutAsJsonAsync("/tarot/cards", Array.Empty<object>());
    Assert.Equal(HttpStatusCode.BadRequest, emptySyncResponse.StatusCode);
  }

  [Fact]
  public async Task AdminTarotCards_GetAndUpdateTexts_WorksInDevelopment()
  {
    using var seedClient = factory.CreateClient();
    seedClient.DefaultRequestHeaders.Add("X-API-Key", ApiTestFactoryBase.SiteApiKey);

    var syncResponse = await seedClient.PutAsJsonAsync("/tarot/cards", new[]
    {
      new
      {
        id = "fool", title = "The Fool", arcana = "Major Arcana", image = "cards/fool.jpg", description = "A leap of faith.", sortOrder = 0,
        translations = new
        {
          fi = new { title = "Narri", arcana = "Iso arkana", description = "Uskon hyppy." }
        }
      }
    });
    Assert.Equal(HttpStatusCode.OK, syncResponse.StatusCode);

    // The development scheme authenticates every request with all roles.
    var getResponse = await _client.GetAsync("/admin/tarot/cards");
    Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

    var cards = await getResponse.Content.ReadFromJsonAsync<List<TarotCardDto>>();
    Assert.NotNull(cards);
    var fool = cards.Single(card => card.Id == "fool");
    Assert.Equal("The Fool", fool.Title);
    Assert.Equal("Narri", fool.Translations["fi"].Title);

    var updateResponse = await _client.PutAsJsonAsync("/admin/tarot/cards/fool/texts", new
    {
      englishTitle = "The Fool (updated)",
      englishDescription = "Updated EN description.",
      finnishTitle = "Narri (päivitetty)",
      finnishDescription = "Päivitetty FI kuvaus."
    });
    Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

    var updated = await updateResponse.Content.ReadFromJsonAsync<TarotCardDto>();
    Assert.NotNull(updated);
    Assert.Equal("The Fool (updated)", updated.Title);
    Assert.Equal("Updated EN description.", updated.Description);
    Assert.Equal("The Fool (updated)", updated.Translations["en"].Title);
    Assert.Equal("Narri (päivitetty)", updated.Translations["fi"].Title);
    Assert.Equal("Päivitetty FI kuvaus.", updated.Translations["fi"].Description);

    // The change is visible through the machine-only endpoint as well.
    var machineGetResponse = await seedClient.GetAsync("/tarot/cards");
    var machineCards = await machineGetResponse.Content.ReadFromJsonAsync<List<TarotCardDto>>();
    Assert.NotNull(machineCards);
    var foolAfter = machineCards.Single(card => card.Id == "fool");
    Assert.Equal("The Fool (updated)", foolAfter.Title);
    Assert.Equal("Päivitetty FI kuvaus.", foolAfter.Translations["fi"].Description);
    // Upsert must not clobber the existing fi arcana.
    Assert.Equal("Iso arkana", foolAfter.Translations["fi"].Arcana);

    var notFoundResponse = await _client.PutAsJsonAsync("/admin/tarot/cards/nope/texts", new
    {
      englishTitle = "x",
      finnishTitle = "y"
    });
    Assert.Equal(HttpStatusCode.NotFound, notFoundResponse.StatusCode);

    var badRequestResponse = await _client.PutAsJsonAsync("/admin/tarot/cards/fool/texts", new
    {
      englishTitle = "  ",
      finnishTitle = "y"
    });
    Assert.Equal(HttpStatusCode.BadRequest, badRequestResponse.StatusCode);
  }
}

[Collection("ApiTests")]
public class ProductionAuthTests(ProductionApiTestFactory factory) : IClassFixture<ProductionApiTestFactory>
{
  private readonly HttpClient _client = factory.CreateClient();

  [Fact]
  public async Task WriteEndpoints_RequireAuthInProduction()
  {
    var createWithoutAuthResponse = await _client.PostAsJsonAsync("/lok/open-hours", new
    {
      id = 0,
      name = "unauthorized",
      version = DateTime.UtcNow.ToString("O"),
      paragraph1 = "p1",
      paragraph2 = "p2",
      paragraph3 = "p3",
      paragraph4 = "p4",
      kitchenNotice = "k1"
    });

    Assert.Equal(HttpStatusCode.Unauthorized, createWithoutAuthResponse.StatusCode);

    var activateWithoutAuthResponse = await _client.PutAsync("/lok/open-hours/1/active", null);
    Assert.Equal(HttpStatusCode.Unauthorized, activateWithoutAuthResponse.StatusCode);
  }

  [Fact]
  public async Task CreateOpenHours_WorksWithAuthInProduction()
  {
    var tokenResponse = await _client.PostAsJsonAsync("/auth/token", new
    {
      username = "admin",
      password = "changeme"
    });

    Assert.Equal(HttpStatusCode.OK, tokenResponse.StatusCode);
    var auth = await tokenResponse.Content.ReadFromJsonAsync<AuthTokenDto>();
    Assert.NotNull(auth);
    Assert.False(string.IsNullOrWhiteSpace(auth.AccessToken));

    _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

    var createPayload = new
    {
      id = 0,
      name = "authorized",
      version = DateTime.UtcNow.ToString("O"),
      paragraph1 = "p1",
      paragraph2 = "p2",
      paragraph3 = "p3",
      paragraph4 = "p4",
      kitchenNotice = "k1"
    };

    var createResponse = await _client.PostAsJsonAsync("/lok/open-hours", createPayload);

    Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
  }

  [Fact]
  public async Task UserManagement_Crud_WorksForAdminInProduction()
  {
    var tokenResponse = await _client.PostAsJsonAsync("/auth/token", new
    {
      username = "admin",
      password = "changeme"
    });

    Assert.Equal(HttpStatusCode.OK, tokenResponse.StatusCode);
    var auth = await tokenResponse.Content.ReadFromJsonAsync<AuthTokenDto>();
    Assert.NotNull(auth);

    _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

    var createResponse = await _client.PostAsJsonAsync("/users", new
    {
      username = "editor",
      password = "editorpass",
      displayName = "Editor User",
      preferredLanguage = "fi",
      roles = Array.Empty<string>()
    });

    Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

    var usersResponse = await _client.GetAsync("/users");
    Assert.Equal(HttpStatusCode.OK, usersResponse.StatusCode);
    var users = await usersResponse.Content.ReadFromJsonAsync<List<UserDto>>();
    Assert.NotNull(users);
    Assert.Contains(users, user => user.Username == "editor");

    var updateResponse = await _client.PutAsJsonAsync("/users/editor", new
    {
      password = "editorpass2",
      displayName = "Editor Admin",
      preferredLanguage = "en",
      roles = new[] { "admin" }
    });

    Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
    var updatedUser = await updateResponse.Content.ReadFromJsonAsync<UserDto>();
    Assert.NotNull(updatedUser);
    Assert.Contains("admin", updatedUser.Roles);
    Assert.Equal("en", updatedUser.PreferredLanguage);
    Assert.Equal("Editor Admin", updatedUser.DisplayName);

    var deleteResponse = await _client.DeleteAsync("/users/editor");
    Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
  }

  [Fact]
  public async Task TarotCards_RequireSiteApiKey_InProduction()
  {
    var getWithoutKey = await _client.GetAsync("/tarot/cards");
    Assert.Equal(HttpStatusCode.Unauthorized, getWithoutKey.StatusCode);

    var syncWithoutKey = await _client.PutAsJsonAsync("/tarot/cards", new[]
    {
      new { id = "fool", title = "The Fool", arcana = "Major Arcana", image = "cards/fool.jpg", description = "desc", sortOrder = 0 }
    });
    Assert.Equal(HttpStatusCode.Unauthorized, syncWithoutKey.StatusCode);

    using var wrongKeyClient = factory.CreateClient();
    wrongKeyClient.DefaultRequestHeaders.Add("X-API-Key", "wrong-key-wrong-key-wrong-key-00");
    var getWrongKey = await wrongKeyClient.GetAsync("/tarot/cards");
    Assert.Equal(HttpStatusCode.Unauthorized, getWrongKey.StatusCode);
  }

  [Fact]
  public async Task TarotCards_SiteApiKey_CanSyncAndGetAll_InProduction()
  {
    using var client = factory.CreateClient();
    client.DefaultRequestHeaders.Add("X-API-Key", ApiTestFactoryBase.SiteApiKey);

    var syncPayload = new[]
    {
      new { id = "fool", title = "The Fool", arcana = "Major Arcana", image = "cards/fool.jpg", description = "A leap of faith.", sortOrder = 0 },
      new { id = "world", title = "The World", arcana = "Major Arcana", image = "cards/world.jpg", description = "Completion.", sortOrder = 21 }
    };

    var syncResponse = await client.PutAsJsonAsync("/tarot/cards", syncPayload);
    Assert.Equal(HttpStatusCode.OK, syncResponse.StatusCode);

    var getResponse = await client.GetAsync("/tarot/cards");
    Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

    var cards = await getResponse.Content.ReadFromJsonAsync<List<TarotCardDto>>();
    Assert.NotNull(cards);
    Assert.Equal(2, cards.Count);
    Assert.Equal("fool", cards[0].Id);
    Assert.Equal("world", cards[1].Id);
  }

  [Fact]
  public async Task TarotCards_PortalJwt_Rejected_InProduction()
  {
    // The tarot endpoints are machine-only: even a valid admin JWT must not
    // open them without the site-to-site API key.
    var tokenResponse = await _client.PostAsJsonAsync("/auth/token", new
    {
      username = "admin",
      password = "changeme"
    });
    Assert.Equal(HttpStatusCode.OK, tokenResponse.StatusCode);
    var auth = await tokenResponse.Content.ReadFromJsonAsync<AuthTokenDto>();
    Assert.NotNull(auth);

    using var jwtClient = factory.CreateClient();
    jwtClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

    var getResponse = await jwtClient.GetAsync("/tarot/cards");
    Assert.Equal(HttpStatusCode.Unauthorized, getResponse.StatusCode);
  }

  [Fact]
  public async Task AdminTarotCards_RequireTarotAdminRole_InProduction()
  {
    var getWithoutAuth = await _client.GetAsync("/admin/tarot/cards");
    Assert.Equal(HttpStatusCode.Unauthorized, getWithoutAuth.StatusCode);

    var putWithoutAuth = await _client.PutAsJsonAsync("/admin/tarot/cards/fool/texts", new
    {
      englishTitle = "x",
      finnishTitle = "y"
    });
    Assert.Equal(HttpStatusCode.Unauthorized, putWithoutAuth.StatusCode);

    // A portal user without the tarot-admin role gets 403.
    var adminTokenResponse = await _client.PostAsJsonAsync("/auth/token", new
    {
      username = "admin",
      password = "changeme"
    });
    var adminAuth = await adminTokenResponse.Content.ReadFromJsonAsync<AuthTokenDto>();
    Assert.NotNull(adminAuth);
    _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminAuth.AccessToken);

    var createLokUserResponse = await _client.PostAsJsonAsync("/users", new
    {
      username = "lokuser",
      password = "lokpass123",
      displayName = "Lok User",
      preferredLanguage = "fi",
      roles = new[] { "lok" }
    });
    Assert.Equal(HttpStatusCode.Created, createLokUserResponse.StatusCode);

    var lokTokenResponse = await _client.PostAsJsonAsync("/auth/token", new
    {
      username = "lokuser",
      password = "lokpass123"
    });
    var lokAuth = await lokTokenResponse.Content.ReadFromJsonAsync<AuthTokenDto>();
    Assert.NotNull(lokAuth);

    using var lokClient = factory.CreateClient();
    lokClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", lokAuth.AccessToken);

    var forbiddenGet = await lokClient.GetAsync("/admin/tarot/cards");
    Assert.Equal(HttpStatusCode.Forbidden, forbiddenGet.StatusCode);

    var forbiddenPut = await lokClient.PutAsJsonAsync("/admin/tarot/cards/fool/texts", new
    {
      englishTitle = "x",
      finnishTitle = "y"
    });
    Assert.Equal(HttpStatusCode.Forbidden, forbiddenPut.StatusCode);

    // The klapi admin role alone must NOT open the tarot admin endpoints:
    // tarot has its own credentials.
    using var adminClient = factory.CreateClient();
    adminClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminAuth.AccessToken);

    var adminForbiddenGet = await adminClient.GetAsync("/admin/tarot/cards");
    Assert.Equal(HttpStatusCode.Forbidden, adminForbiddenGet.StatusCode);

    var adminForbiddenPut = await adminClient.PutAsJsonAsync("/admin/tarot/cards/fool/texts", new
    {
      englishTitle = "x",
      finnishTitle = "y"
    });
    Assert.Equal(HttpStatusCode.Forbidden, adminForbiddenPut.StatusCode);

    // The dedicated tarot admin user (seeded from Auth:TarotAdmin) can.
    var tarotTokenResponse = await _client.PostAsJsonAsync("/auth/token", new
    {
      username = "hermes-trismegistos",
      password = "tarotpass"
    });
    Assert.Equal(HttpStatusCode.OK, tarotTokenResponse.StatusCode);
    var tarotAuth = await tarotTokenResponse.Content.ReadFromJsonAsync<AuthTokenDto>();
    Assert.NotNull(tarotAuth);
    Assert.Contains("tarot-admin", tarotAuth.Roles);
    Assert.DoesNotContain("admin", tarotAuth.Roles);

    using var tarotClient = factory.CreateClient();
    tarotClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tarotAuth.AccessToken);

    var tarotGet = await tarotClient.GetAsync("/admin/tarot/cards");
    Assert.Equal(HttpStatusCode.OK, tarotGet.StatusCode);

    var deleteResponse = await _client.DeleteAsync("/users/lokuser");
    Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
  }

  [Fact]
  public async Task TarotAdmin_CannotAccessKlapiAdminEndpoints_InProduction()
  {
    // Separation works both ways: the tarot admin JWT must not open klapi's
    // lok or user-management endpoints.
    var tarotTokenResponse = await _client.PostAsJsonAsync("/auth/token", new
    {
      username = "hermes-trismegistos",
      password = "tarotpass"
    });
    Assert.Equal(HttpStatusCode.OK, tarotTokenResponse.StatusCode);
    var tarotAuth = await tarotTokenResponse.Content.ReadFromJsonAsync<AuthTokenDto>();
    Assert.NotNull(tarotAuth);

    using var tarotClient = factory.CreateClient();
    tarotClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tarotAuth.AccessToken);

    var usersResponse = await tarotClient.GetAsync("/users");
    Assert.Equal(HttpStatusCode.Forbidden, usersResponse.StatusCode);

    var lokResponse = await tarotClient.PostAsJsonAsync("/lok/open-hours", new
    {
      id = 0,
      name = "forbidden",
      version = DateTime.UtcNow.ToString("O"),
      paragraph1 = "p1",
      paragraph2 = "p2",
      paragraph3 = "p3",
      paragraph4 = "p4",
      kitchenNotice = "k1"
    });
    Assert.Equal(HttpStatusCode.Forbidden, lokResponse.StatusCode);
  }

  [Fact]
  public async Task AdminTarotCards_TarotAdminJwt_CanGetAndUpdateTexts_InProduction()
  {
    using var seedClient = factory.CreateClient();
    seedClient.DefaultRequestHeaders.Add("X-API-Key", ApiTestFactoryBase.SiteApiKey);

    var syncResponse = await seedClient.PutAsJsonAsync("/tarot/cards", new[]
    {
      new { id = "world", title = "The World", arcana = "Major Arcana", image = "cards/world.jpg", description = "Completion.", sortOrder = 21 }
    });
    Assert.Equal(HttpStatusCode.OK, syncResponse.StatusCode);

    var tokenResponse = await _client.PostAsJsonAsync("/auth/token", new
    {
      username = "hermes-trismegistos",
      password = "tarotpass"
    });
    Assert.Equal(HttpStatusCode.OK, tokenResponse.StatusCode);
    var auth = await tokenResponse.Content.ReadFromJsonAsync<AuthTokenDto>();
    Assert.NotNull(auth);

    using var tarotAdminClient = factory.CreateClient();
    tarotAdminClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

    var getResponse = await tarotAdminClient.GetAsync("/admin/tarot/cards");
    Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

    var cards = await getResponse.Content.ReadFromJsonAsync<List<TarotCardDto>>();
    Assert.NotNull(cards);
    Assert.Single(cards);

    var updateResponse = await tarotAdminClient.PutAsJsonAsync("/admin/tarot/cards/world/texts", new
    {
      englishTitle = "The World!",
      englishDescription = "Completion, updated.",
      finnishTitle = "Maailma",
      finnishDescription = "Täydellisyys."
    });
    Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

    var updated = await updateResponse.Content.ReadFromJsonAsync<TarotCardDto>();
    Assert.NotNull(updated);
    Assert.Equal("The World!", updated.Title);
    Assert.Equal("Completion, updated.", updated.Description);
    Assert.Equal("Maailma", updated.Translations["fi"].Title);
    Assert.Equal("Täydellisyys.", updated.Translations["fi"].Description);

    // The machine-only endpoint sees the same change.
    var machineGetResponse = await seedClient.GetAsync("/tarot/cards");
    var machineCards = await machineGetResponse.Content.ReadFromJsonAsync<List<TarotCardDto>>();
    Assert.NotNull(machineCards);
    var world = machineCards.Single(card => card.Id == "world");
    Assert.Equal("The World!", world.Title);
    Assert.Equal("Maailma", world.Translations["fi"].Title);
  }
}

public abstract class ApiTestFactoryBase : WebApplicationFactory<Program>
{
  private static readonly string[] TestConfigKeys =
  [
    "ConnectionStrings__DefaultConnection",
    "Auth__Issuer",
    "Auth__Audience",
    "Auth__SigningKey",
    "Auth__AllowedOrigins__0",
    "Auth__Admin__Username",
    "Auth__Admin__Password",
    "Auth__Admin__DisplayName",
    "Auth__TarotAdmin__Username",
    "Auth__TarotAdmin__Password",
    "Auth__TarotAdmin__DisplayName",
    "Auth__SiteToSite__ApiKey"
  ];

  public const string SiteApiKey = "test-site-api-key-0123456789abcdef";

  private readonly string _environmentName;
  private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"klapi-tests-{Guid.NewGuid():N}.db");

  protected ApiTestFactoryBase(string environmentName)
  {
    _environmentName = environmentName;

    // Program.cs reads configuration synchronously while the host is being
    // built, so the overrides must come from a config source that is present
    // from the start: environment variables. (In-memory configuration added
    // via ConfigureAppConfiguration below is applied too late for those reads.)
    Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", $"Data Source={_dbPath}");
    Environment.SetEnvironmentVariable("Auth__Issuer", "klapi-api");
    Environment.SetEnvironmentVariable("Auth__Audience", "klapi-ui");
    Environment.SetEnvironmentVariable("Auth__SigningKey", "change-this-to-a-long-random-32-char-minimum-key");
    Environment.SetEnvironmentVariable("Auth__AllowedOrigins__0", "http://localhost:5173");
    Environment.SetEnvironmentVariable("Auth__Admin__Username", "admin");
    Environment.SetEnvironmentVariable("Auth__Admin__Password", "changeme");
    Environment.SetEnvironmentVariable("Auth__Admin__DisplayName", "Administrator");
    Environment.SetEnvironmentVariable("Auth__TarotAdmin__Username", "hermes-trismegistos");
    Environment.SetEnvironmentVariable("Auth__TarotAdmin__Password", "tarotpass");
    Environment.SetEnvironmentVariable("Auth__TarotAdmin__DisplayName", "Hermes Trismegistos");
    Environment.SetEnvironmentVariable("Auth__SiteToSite__ApiKey", SiteApiKey);
  }

  protected override void ConfigureWebHost(IWebHostBuilder builder)
  {
    builder.UseEnvironment(_environmentName);

    builder.ConfigureAppConfiguration((_, configBuilder) =>
    {
      configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["ConnectionStrings:DefaultConnection"] = $"Data Source={_dbPath}",
        ["Auth:Issuer"] = "klapi-api",
        ["Auth:Audience"] = "klapi-ui",
        ["Auth:SigningKey"] = "change-this-to-a-long-random-32-char-minimum-key",
        ["Auth:AllowedOrigins:0"] = "http://localhost:5173",
        ["Auth:Admin:Username"] = "admin",
        ["Auth:Admin:Password"] = "changeme",
        ["Auth:Admin:DisplayName"] = "Administrator",
        ["Auth:TarotAdmin:Username"] = "hermes-trismegistos",
        ["Auth:TarotAdmin:Password"] = "tarotpass",
        ["Auth:TarotAdmin:DisplayName"] = "Hermes Trismegistos",
        ["Auth:SiteToSite:ApiKey"] = SiteApiKey
      });
    });
  }

  protected override void Dispose(bool disposing)
  {
    base.Dispose(disposing);

    if (!disposing)
    {
      return;
    }

    foreach (var key in TestConfigKeys)
    {
      Environment.SetEnvironmentVariable(key, null);
    }

    try
    {
      if (File.Exists(_dbPath))
      {
        File.Delete(_dbPath);
      }
    }
    catch
    {
    }
  }
}

public sealed class DevelopmentApiTestFactory : ApiTestFactoryBase
{
  public DevelopmentApiTestFactory() : base(Environments.Development)
  {
  }
}

public sealed class ProductionApiTestFactory : ApiTestFactoryBase
{
  public ProductionApiTestFactory() : base(Environments.Production)
  {
  }
}

public class LokOpenHoursDto
{
  public long Id { get; set; }

  public string Name { get; set; } = string.Empty;

  public bool IsActive { get; set; }

  public DateTime Version { get; set; }

  public string Paragraph1 { get; set; } = string.Empty;

  public string Paragraph2 { get; set; } = string.Empty;

  public string Paragraph3 { get; set; } = string.Empty;

  public string Paragraph4 { get; set; } = string.Empty;

  public string KitchenNotice { get; set; } = string.Empty;
}

public class AuthTokenDto
{
  public string AccessToken { get; set; } = string.Empty;

  public string Username { get; set; } = string.Empty;

  public string DisplayName { get; set; } = string.Empty;

  public string PreferredLanguage { get; set; } = string.Empty;

  public List<string> Roles { get; set; } = [];

  public string TokenType { get; set; } = string.Empty;

  public int ExpiresIn { get; set; }
}

public class UserDto
{
  public string Username { get; set; } = string.Empty;

  public DateTime Added { get; set; }

  public DateTime LastUpdated { get; set; }

  public string DisplayName { get; set; } = string.Empty;

  public string PreferredLanguage { get; set; } = string.Empty;

  public List<string> Roles { get; set; } = [];
}

public class TarotCardTranslationDto
{
  public string Title { get; set; } = string.Empty;

  public string Arcana { get; set; } = string.Empty;

  public string Description { get; set; } = string.Empty;
}

public class TarotCardDto
{
  public string Id { get; set; } = string.Empty;

  public string Title { get; set; } = string.Empty;

  public string Arcana { get; set; } = string.Empty;

  public string Image { get; set; } = string.Empty;

  public string Description { get; set; } = string.Empty;

  public int SortOrder { get; set; }

  public Dictionary<string, TarotCardTranslationDto> Translations { get; set; } = new();
}
