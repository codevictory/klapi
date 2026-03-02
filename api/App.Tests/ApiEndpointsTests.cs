using System.Net;
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

  public DateTime Version { get; set; }

  public string Paragraph1 { get; set; } = string.Empty;

  public string Paragraph2 { get; set; } = string.Empty;

  public string Paragraph3 { get; set; } = string.Empty;

  public string Paragraph4 { get; set; } = string.Empty;

  public string KitchenNotice { get; set; } = string.Empty;
}
