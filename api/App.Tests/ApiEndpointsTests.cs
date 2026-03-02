using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace App.Tests;

public class ApiEndpointsTests(ApiTestFactory factory) : IClassFixture<ApiTestFactory>
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
  public async Task OpenHours_Crud_Works()
  {
    var unauthorizedCreateResponse = await _client.PostAsJsonAsync("/lok/open-hours", new
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
    Assert.Equal(HttpStatusCode.Unauthorized, unauthorizedCreateResponse.StatusCode);

    var tokenResponse = await _client.PostAsJsonAsync("/auth/token", new
    {
      email = "admin@klapi.local",
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
}

public class ApiTestFactory : WebApplicationFactory<Program>
{
  private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"klapi-tests-{Guid.NewGuid():N}.db");

  protected override void ConfigureWebHost(IWebHostBuilder builder)
  {
    builder.ConfigureAppConfiguration((_, configBuilder) =>
    {
      configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["ConnectionStrings:DefaultConnection"] = $"Data Source={_dbPath}"
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

  public string Email { get; set; } = string.Empty;

  public string TokenType { get; set; } = string.Empty;

  public int ExpiresIn { get; set; }
}
