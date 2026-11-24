using System.Text.RegularExpressions;

public static class VihkotappeluEndpoints
{
  // A sprite pixel cell: null (transparent) or 'side' (the owning side's
  // faction color) are handled separately; otherwise a hex color.
  private static readonly Regex PixelColorRegex = new(@"^#(?:[0-9a-fA-F]{6}|[0-9a-fA-F]{8})$", RegexOptions.Compiled);

  public static void MapVihkotappeluEndpoints(WebApplication app)
  {
    // The vihkotappelu game server fetches its ENTIRE game data (units +
    // factions + sprites) in this single bulk request, never piece by piece.
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

      // Sprites are optional in the payload. When present, validate the pixel
      // grid and require every unit.sprite to reference one of the supplied
      // sprites (when absent, Sync keeps the stored sprites unchanged).
      if (data.Sprites is not null)
      {
        var spriteKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var sprite in data.Sprites)
        {
          if (string.IsNullOrWhiteSpace(sprite.SpriteKey))
          {
            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            await httpContext.Response.WriteAsJsonAsync(new
            {
              Message = "Every sprite must have a non-empty spriteKey."
            });
            return;
          }

          if (sprite.Width < 1 || sprite.Width > 64 || sprite.Height < 1 || sprite.Height > 64)
          {
            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            await httpContext.Response.WriteAsJsonAsync(new
            {
              Message = $"Sprite \"{sprite.SpriteKey}\" width/height must be integers in 1..64."
            });
            return;
          }

          if (sprite.Pixels is null || sprite.Pixels.Count != sprite.Height)
          {
            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            await httpContext.Response.WriteAsJsonAsync(new
            {
              Message = $"Sprite \"{sprite.SpriteKey}\" pixels must have exactly {sprite.Height} rows."
            });
            return;
          }

          foreach (var row in sprite.Pixels)
          {
            if (row is null || row.Count != sprite.Width)
            {
              httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
              await httpContext.Response.WriteAsJsonAsync(new
              {
                Message = $"Sprite \"{sprite.SpriteKey}\" pixel rows must have exactly {sprite.Width} cells."
              });
              return;
            }

            foreach (var cell in row)
            {
              if (cell is null || cell == "side") continue;
              if (!PixelColorRegex.IsMatch(cell))
              {
                httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                await httpContext.Response.WriteAsJsonAsync(new
                {
                  Message = $"Sprite \"{sprite.SpriteKey}\" has an invalid pixel color \"{cell}\" (want null, \"side\" or #rrggbb/#rrggbbaa)."
                });
                return;
              }
            }
          }

          sprite.SpriteKey = sprite.SpriteKey.Trim();
          spriteKeys.Add(sprite.SpriteKey);
        }

        var danglingSprite = data.Units.FirstOrDefault(unit => !spriteKeys.Contains(unit.Sprite));
        if (danglingSprite is not null)
        {
          httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
          await httpContext.Response.WriteAsJsonAsync(new
          {
            Message = $"Unit \"{danglingSprite.UnitKey}\" references unknown sprite \"{danglingSprite.Sprite}\"."
          });
          return;
        }
      }

      var stored = await service.Sync(data);
      await httpContext.Response.WriteAsJsonAsync(stored);
    })
    .RequireCors("FrontendWriteCors")
    .RequireAuthorization("HasVihkotappeluRole")
    .WithName("SyncVihkotappeluGameData");
  }
}
