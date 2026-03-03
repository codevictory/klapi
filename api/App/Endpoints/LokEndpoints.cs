public static class LokEndpoints
{
  public static void MapLokEndpoints(WebApplication app)
  {
    var createLokOpenHoursEndpoint = app.MapPost("/lok/open-hours", async (HttpContext httpContext) =>
    {
      var lokService = httpContext.RequestServices.GetRequiredService<LokService>();
      var openHours = await httpContext.Request.ReadFromJsonAsync<LokOpenHours>();

      if (openHours is null)
      {
        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        await httpContext.Response.WriteAsJsonAsync(new
        {
          Message = "Request body is required."
        });
        return;
      }

      if (string.IsNullOrWhiteSpace(openHours.Name))
      {
        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        await httpContext.Response.WriteAsJsonAsync(new
        {
          Message = "Open hours version name is required."
        });
        return;
      }

      var createdOpenHours = await lokService.InsertOpenHours(openHours);
      httpContext.Response.StatusCode = StatusCodes.Status201Created;
      httpContext.Response.Headers.Location = "/lok/open-hours";
      await httpContext.Response.WriteAsJsonAsync(createdOpenHours);
    })
    .RequireCors("FrontendWriteCors")
    .WithName("CreateLokOpenHours");

    if (!app.Environment.IsDevelopment())
    {
      createLokOpenHoursEndpoint.RequireAuthorization("OpenHoursWrite");
    }

    app.MapGet("/lok/open-hours", async (HttpContext httpContext) =>
    {
      var lokService = httpContext.RequestServices.GetRequiredService<LokService>();
      var openHours = await lokService.GetOpenHours();

      if (openHours.Count == 0)
      {
        httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
        await httpContext.Response.WriteAsJsonAsync(new
        {
          Message = "Open hours not found."
        });
        return;
      }

      await httpContext.Response.WriteAsJsonAsync(openHours);
    })
    .RequireCors("PublicReadCors")
    .WithName("GetLokOpenHours");

    var deleteLokOpenHoursEndpoint = app.MapDelete("/lok/open-hours/{id:long}", async (HttpContext httpContext, long id) =>
    {
      var lokService = httpContext.RequestServices.GetRequiredService<LokService>();
      var deleted = await lokService.DeleteOpenHours(id);

      if (!deleted)
      {
        httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
        await httpContext.Response.WriteAsJsonAsync(new
        {
          Message = "Open hours version not found."
        });
        return;
      }

      httpContext.Response.StatusCode = StatusCodes.Status204NoContent;
    })
    .RequireCors("FrontendWriteCors")
    .WithName("DeleteLokOpenHours");

    if (!app.Environment.IsDevelopment())
    {
      deleteLokOpenHoursEndpoint.RequireAuthorization("OpenHoursWrite");
    }

    var updateLokOpenHoursEndpoint = app.MapPut("/lok/open-hours/{id:long}", async (HttpContext httpContext, long id) =>
    {
      var lokService = httpContext.RequestServices.GetRequiredService<LokService>();
      var openHours = await httpContext.Request.ReadFromJsonAsync<LokOpenHours>();

      if (openHours is null)
      {
        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        await httpContext.Response.WriteAsJsonAsync(new
        {
          Message = "Request body is required."
        });
        return;
      }

      if (string.IsNullOrWhiteSpace(openHours.Name))
      {
        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        await httpContext.Response.WriteAsJsonAsync(new
        {
          Message = "Open hours version name is required."
        });
        return;
      }

      var updatedOpenHours = await lokService.UpdateOpenHours(id, openHours);

      if (updatedOpenHours is null)
      {
        httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
        await httpContext.Response.WriteAsJsonAsync(new
        {
          Message = "Open hours version not found."
        });
        return;
      }

      await httpContext.Response.WriteAsJsonAsync(updatedOpenHours);
    })
    .RequireCors("FrontendWriteCors")
    .WithName("UpdateLokOpenHours");

    if (!app.Environment.IsDevelopment())
    {
      updateLokOpenHoursEndpoint.RequireAuthorization("OpenHoursWrite");
    }

    var setActiveLokOpenHoursEndpoint = app.MapPut("/lok/open-hours/{id:long}/active", async (HttpContext httpContext, long id) =>
    {
      var lokService = httpContext.RequestServices.GetRequiredService<LokService>();
      var activated = await lokService.SetActiveOpenHours(id);

      if (!activated)
      {
        httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
        await httpContext.Response.WriteAsJsonAsync(new
        {
          Message = "Open hours version not found."
        });
        return;
      }

      await httpContext.Response.WriteAsJsonAsync(new
      {
        Id = id,
        IsActive = true
      });
    })
    .RequireCors("FrontendWriteCors")
    .WithName("SetActiveLokOpenHours");

    if (!app.Environment.IsDevelopment())
    {
      setActiveLokOpenHoursEndpoint.RequireAuthorization("OpenHoursWrite");
    }
  }
}