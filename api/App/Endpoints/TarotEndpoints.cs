public static class TarotEndpoints
{
  public static void MapTarotEndpoints(WebApplication app)
  {
    app.MapGet("/tarot/cards", async (HttpContext httpContext) =>
    {
      var tarotService = httpContext.RequestServices.GetRequiredService<TarotService>();
      var cards = await tarotService.GetAllCards();

      await httpContext.Response.WriteAsJsonAsync(cards);
    })
    .RequireCors("FrontendWriteCors")
    .RequireAuthorization("HasTarotRole")
    .WithName("GetAllTarotCards");

    app.MapPut("/tarot/cards", async (HttpContext httpContext) =>
    {
      var tarotService = httpContext.RequestServices.GetRequiredService<TarotService>();
      var cards = await httpContext.Request.ReadFromJsonAsync<List<TarotCard>>();

      if (cards is null || cards.Count == 0)
      {
        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        await httpContext.Response.WriteAsJsonAsync(new
        {
          Message = "Request body must be a non-empty array of cards."
        });
        return;
      }

      var invalidCard = cards.FirstOrDefault(card =>
        string.IsNullOrWhiteSpace(card.Id) || string.IsNullOrWhiteSpace(card.Title));

      if (invalidCard is not null)
      {
        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        await httpContext.Response.WriteAsJsonAsync(new
        {
          Message = "Every card must have a non-empty id and title."
        });
        return;
      }

      foreach (var card in cards)
      {
        card.Id = card.Id.Trim();
        card.Title = card.Title.Trim();
      }

      var storedCards = await tarotService.SyncCards(cards);
      await httpContext.Response.WriteAsJsonAsync(storedCards);
    })
    .RequireCors("FrontendWriteCors")
    .RequireAuthorization("HasTarotRole")
    .WithName("SyncTarotCards");
  }
}
