public class TarotCardTranslation
{
  public string Title { get; set; } = string.Empty;

  public string Arcana { get; set; } = string.Empty;

  public string Description { get; set; } = string.Empty;
}

// English/Finnish title + description editable from the /admin portal.
// English maps to the canonical base columns (plus the "en" translation
// row); Finnish maps to the "fi" translation row.
public record TarotCardTextsUpdate(
  string EnglishTitle,
  string EnglishDescription,
  string FinnishTitle,
  string FinnishDescription);

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
