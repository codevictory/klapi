public static class VihkotappeluEndpoints
{
  public static void MapVihkotappeluEndpoints(WebApplication app)
  {
    // The vihkotappelu game server fetches its ENTIRE game data (units +
    // factions) in this single bulk request, never piece by piece.
    app.MapGet("/vihkotappelu/data", async (HttpContext httpContext) =>
    {
      var service = httpContext.RequestServices.GetRequiredService<VihkotappeluService>();
      var data = await service.GetAll();

      await httpContext.Response.WriteAsJsonAsync(data);
    })
    .RequireCors("FrontendWriteCors")
    .RequireAuthorization("HasVihkotappeluRole")
    .WithName("GetVihkotappeluGameData");

    // Bulk sync (PUT) used by the vihkotappelu repo's tools/sync_to_klapi.js
    // to push units.json + factions/*.json into the database.
    app.MapPut("/vihkotappelu/data", async (HttpContext httpContext) =>
    {
      var service = httpContext.RequestServices.GetRequiredService<VihkotappeluService>();
      var data = await httpContext.Request.ReadFromJsonAsync<VihkotappeluGameData>();

      if (data is null || (data.Units.Count == 0 && data.Factions.Count == 0))
      {
        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        await httpContext.Response.WriteAsJsonAsync(new
        {
          Message = "Request body must be game data with at least one unit or faction."
        });
        return;
      }

      var invalidUnit = data.Units.FirstOrDefault(unit => string.IsNullOrWhiteSpace(unit.UnitKey));
      if (invalidUnit is not null)
      {
        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        await httpContext.Response.WriteAsJsonAsync(new
        {
          Message = "Every unit must have a non-empty unitKey."
        });
        return;
      }

      var invalidFaction = data.Factions.FirstOrDefault(faction => string.IsNullOrWhiteSpace(faction.Id));
      if (invalidFaction is not null)
      {
        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        await httpContext.Response.WriteAsJsonAsync(new
        {
          Message = "Every faction must have a non-empty id."
        });
        return;
      }

      var unitKeys = new HashSet<string>(data.Units.Select(u => u.UnitKey.Trim()), StringComparer.OrdinalIgnoreCase);
      var danglingLink = data.Factions
        .SelectMany(f => f.Units.Select(unitKey => (Faction: f, UnitKey: unitKey)))
        .FirstOrDefault(link => !unitKeys.Contains(link.UnitKey.Trim()));
      if (danglingLink.Faction is not null)
      {
        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        await httpContext.Response.WriteAsJsonAsync(new
        {
          Message = $"Faction \"{danglingLink.Faction.Id}\" references unknown unit \"{danglingLink.UnitKey}\"."
        });
        return;
      }

      foreach (var unit in data.Units)
      {
        unit.UnitKey = unit.UnitKey.Trim();
        unit.Name = unit.Name.Trim();
        unit.Sprite = unit.Sprite.Trim();
      }

      foreach (var faction in data.Factions)
      {
        faction.Id = faction.Id.Trim();
        faction.Name = faction.Name.Trim();
        faction.Color = faction.Color.Trim();
        faction.Units = faction.Units.Select(u => u.Trim()).Where(u => u.Length > 0).ToList();
      }

      var stored = await service.Sync(data);
      await httpContext.Response.WriteAsJsonAsync(stored);
    })
    .RequireCors("FrontendWriteCors")
    .RequireAuthorization("HasVihkotappeluRole")
    .WithName("SyncVihkotappeluGameData");
  }
}
