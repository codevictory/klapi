public class VihkotappeluUnitTranslation
{
  public string Name { get; set; } = string.Empty;

  public string Description { get; set; } = string.Empty;
}

public class VihkotappeluUnit
{
  public string UnitKey { get; set; } = string.Empty;

  // Canonical (English) texts; also used as the fallback when a language has
  // no translation row.
  public string Name { get; set; } = string.Empty;

  public string Description { get; set; } = string.Empty;

  // Language-independent battle stats.
  public int Hp { get; set; }

  public int Attack { get; set; }

  public int Defence { get; set; }

  public int Movement { get; set; }

  public int Range { get; set; } = 1;

  public int MinRange { get; set; } = 1;

  // Key into the SPRITES table of the vihkotappelu browser client.
  public string Sprite { get; set; } = string.Empty;

  public int SortOrder { get; set; }

  // Localized texts keyed by language code (e.g. "en", "fi"); see
  // VihkotappeluUnitTranslations table. Always contains at least "en".
  public Dictionary<string, VihkotappeluUnitTranslation> Translations { get; set; } = new();
}

public class VihkotappeluFactionTranslation
{
  public string Name { get; set; } = string.Empty;
}

public class VihkotappeluFaction
{
  public string Id { get; set; } = string.Empty;

  // Canonical (English) name; localized names are in Translations.
  public string Name { get; set; } = string.Empty;

  // Hex color used for the side on the vihkotappelu board.
  public string Color { get; set; } = string.Empty;

  public int SortOrder { get; set; }

  public Dictionary<string, VihkotappeluFactionTranslation> Translations { get; set; } = new();

  // Unit keys belonging to this faction (see VihkotappeluFactionUnits).
  public List<string> Units { get; set; } = [];
}

public class VihkotappeluSprite
{
  public string SpriteKey { get; set; } = string.Empty;

  public int Width { get; set; } = 16;

  public int Height { get; set; } = 16;

  // The pixel grid as height rows x width cells. Each cell is null
  // (transparent), a hex color "#rrggbb"/"#rrggbbaa", or the sentinel
  // "side" (the owning side's faction color, see VihkotappeluFactions.Color).
  public List<List<string?>> Pixels { get; set; } = [];

  public int SortOrder { get; set; }
}

// The whole vihkotappelu game data in one payload: the unit definitions, the
// factions (sides) and the sprites. Fetched by the vihkotappelu server in ONE
// bulk call. Sprites is null when the payload does not carry sprites (PUT
// then keeps the stored sprites unchanged).
public class VihkotappeluGameData
{
  public List<VihkotappeluUnit> Units { get; set; } = [];

  public List<VihkotappeluFaction> Factions { get; set; } = [];

  public List<VihkotappeluSprite>? Sprites { get; set; }
}
