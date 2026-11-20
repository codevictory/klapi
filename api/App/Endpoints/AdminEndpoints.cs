// Administration endpoints behind the /admin portal. Unlike the machine-only
// /tarot endpoints (site-to-site API key), these are for human portal users
// and require a JWT with the admin role.
public record AdminCardTextsUpdateRequest(
  string? EnglishTitle,
  string? EnglishDescription,
  string? FinnishTitle,
  string? FinnishDescription);

public static class AdminEndpoints
{
  public static void MapAdminEndpoints(WebApplication app)
  {
    app.MapGet("/admin/tarot/cards", async (HttpContext httpContext) =>
    {
      var tarotService = httpContext.RequestServices.GetRequiredService<TarotService>();
      var cards = await tarotService.GetAllCards();

      await httpContext.Response.WriteAsJsonAsync(cards);
    })
    .RequireCors("FrontendWriteCors")
    .RequireAuthorization("HasAdminRole")
    .WithName("GetAdminTarotCards");

    app.MapPut("/admin/tarot/cards/{cardId}/texts", async (HttpContext httpContext, string cardId) =>
    {
      var tarotService = httpContext.RequestServices.GetRequiredService<TarotService>();
      var request = await httpContext.Request.ReadFromJsonAsync<AdminCardTextsUpdateRequest>();

      if (request is null)
      {
        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        await httpContext.Response.WriteAsJsonAsync(new
        {
          Message = "Request body is required."
        });
        return;
      }

      var normalizedCardId = cardId.Trim();

      if (normalizedCardId.Length == 0)
      {
        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        await httpContext.Response.WriteAsJsonAsync(new
        {
          Message = "Card id is required."
        });
        return;
      }

      var englishTitle = request.EnglishTitle ?? string.Empty;
      var finnishTitle = request.FinnishTitle ?? string.Empty;

      if (string.IsNullOrWhiteSpace(englishTitle))
      {
        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        await httpContext.Response.WriteAsJsonAsync(new
        {
          Message = "English title is required."
        });
        return;
      }

      if (string.IsNullOrWhiteSpace(finnishTitle))
      {
        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        await httpContext.Response.WriteAsJsonAsync(new
        {
          Message = "Finnish title is required."
        });
        return;
      }

      var updatedCard = await tarotService.UpdateCardLocalizedTexts(normalizedCardId, new TarotCardTextsUpdate(
        englishTitle,
        request.EnglishDescription ?? string.Empty,
        finnishTitle,
        request.FinnishDescription ?? string.Empty));

      if (updatedCard is null)
      {
        httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
        await httpContext.Response.WriteAsJsonAsync(new
        {
          Message = "Card not found."
        });
        return;
      }

      await httpContext.Response.WriteAsJsonAsync(updatedCard);
    })
    .RequireCors("FrontendWriteCors")
    .RequireAuthorization("HasAdminRole")
    .WithName("UpdateAdminTarotCardTexts");
  }
}
