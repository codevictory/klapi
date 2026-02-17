public static class LokEndpoints
{
  public static void MapLokEndpoints(WebApplication app)
  {
    app.MapGet("/lok/open-hours", async (LokService lokService) =>
    {
      var openHours = await lokService.GetOpenHoursAsync();

      if (openHours is null)
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