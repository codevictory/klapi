public class TarotCardTranslation
{
  public string Title { get; set; } = string.Empty;

  public string Arcana { get; set; } = string.Empty;

  public string Description { get; set; } = string.Empty;
}

public class TarotCard
{
  public string Id { get; set; } = string.Empty;

  // Canonical (English) texts; also used as the stable arcana filter key.
  public string Title { get; set; } = string.Empty;

  public string Arcana { get; set; } = string.Empty;

  public string Image { get; set; } = string.Empty;

  public string Description { get; set; } = string.Empty;

  public int SortOrder { get; set; }

  // Localized texts keyed by language code (e.g. "en", "fi"); see
  // TarotCardTranslations table. Always contains at least "en".
  public Dictionary<string, TarotCardTranslation> Translations { get; set; } = new();
}
