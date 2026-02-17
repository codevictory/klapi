public static class LokEndpoints
{
  public static void MapLokEndpoints(WebApplication app)
  {
    app.MapPost("/lok/open-hours", async (LokOpenHours openHours, LokService lokService) =>
    {
      var createdOpenHours = await lokService.InsertOpenHours(openHours);
      return Results.Created("/lok/open-hours", createdOpenHours);
    })
    .WithName("CreateLokOpenHours");

    app.MapGet("/lok/open-hours", async (LokService lokService) =>
    {
      var openHours = await lokService.GetOpenHours();

      if (openHours.Count == 0)
      {
        return Results.NotFound(new
        {
          Message = "Open hours not found."
        });
      }

      return Results.Ok(openHours);
    })
    .WithName("GetLokOpenHours");
  }
}