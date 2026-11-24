CREATE TABLE IF NOT EXISTS LokOpenHours (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  name TEXT NOT NULL DEFAULT '',
  isActive INTEGER NOT NULL DEFAULT 0,
  version TEXT NOT NULL,
  paragraph1 TEXT NOT NULL DEFAULT '',
  paragraph2 TEXT NOT NULL DEFAULT '',
  paragraph3 TEXT NOT NULL DEFAULT '',
  paragraph4 TEXT NOT NULL DEFAULT '',
  kitchenNotice TEXT NOT NULL DEFAULT ''
);

CREATE TABLE IF NOT EXISTS Users (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  username TEXT NOT NULL UNIQUE,
  password TEXT NOT NULL,
  added TEXT NOT NULL,
  lastUpdated TEXT NOT NULL,
  displayName TEXT NOT NULL DEFAULT '',
  preferredLanguage TEXT NOT NULL DEFAULT 'fi'
);

CREATE TABLE IF NOT EXISTS UserRoles (
  userId INTEGER NOT NULL REFERENCES Users(id) ON DELETE CASCADE,
  roleName TEXT NOT NULL,
  PRIMARY KEY (userId, roleName)
);

CREATE TABLE IF NOT EXISTS TarotCards (
  id TEXT PRIMARY KEY,
  title TEXT NOT NULL DEFAULT '',
  arcana TEXT NOT NULL DEFAULT '',
  image TEXT NOT NULL DEFAULT '',
  description TEXT NOT NULL DEFAULT '',
  sortOrder INTEGER NOT NULL DEFAULT 0,
  lastUpdated TEXT NOT NULL DEFAULT '',
  -- Whether the card is tagged "Written by AI" on the tarot site.
  -- 1 = show the tag (default for every card), 0 = hide it.
  writtenByAi INTEGER NOT NULL DEFAULT 1
);

-- Localized card texts. The base TarotCards columns hold the canonical
-- (English) title/arcana/description; every supported language (see
-- AppLanguages in the API: fi, en, sk) can additionally have a row here.
CREATE TABLE IF NOT EXISTS TarotCardTranslations (
  cardId TEXT NOT NULL REFERENCES TarotCards(id) ON DELETE CASCADE,
  language TEXT NOT NULL DEFAULT '',
  title TEXT NOT NULL DEFAULT '',
  arcana TEXT NOT NULL DEFAULT '',
  description TEXT NOT NULL DEFAULT '',
  PRIMARY KEY (cardId, language)
);

-- Vihkotappelu (fantasy battle game) unit definitions. The base columns hold
-- the canonical (English) name/description plus the language-independent
-- stats; localized texts live in VihkotappeluUnitTranslations.
CREATE TABLE IF NOT EXISTS VihkotappeluUnits (
  unitKey TEXT PRIMARY KEY,
  name TEXT NOT NULL DEFAULT '',
  description TEXT NOT NULL DEFAULT '',
  hp INTEGER NOT NULL DEFAULT 0,
  attack INTEGER NOT NULL DEFAULT 0,
  defence INTEGER NOT NULL DEFAULT 0,
  movement INTEGER NOT NULL DEFAULT 0,
  range INTEGER NOT NULL DEFAULT 1,
  minRange INTEGER NOT NULL DEFAULT 1,
  sprite TEXT NOT NULL DEFAULT '',
  sortOrder INTEGER NOT NULL DEFAULT 0,
  lastUpdated TEXT NOT NULL DEFAULT ''
);

-- Localized unit names/descriptions (see AppLanguages in the API: fi, en, sk).
CREATE TABLE IF NOT EXISTS VihkotappeluUnitTranslations (
  unitKey TEXT NOT NULL REFERENCES VihkotappeluUnits(unitKey) ON DELETE CASCADE,
  language TEXT NOT NULL DEFAULT '',
  name TEXT NOT NULL DEFAULT '',
  description TEXT NOT NULL DEFAULT '',
  PRIMARY KEY (unitKey, language)
);

-- Vihkotappelu factions (the sides of a match). Base name column is the
-- canonical (English) name; localized names in VihkotappeluFactionTranslations.
CREATE TABLE IF NOT EXISTS VihkotappeluFactions (
  id TEXT PRIMARY KEY,
  name TEXT NOT NULL DEFAULT '',
  color TEXT NOT NULL DEFAULT '',
  sortOrder INTEGER NOT NULL DEFAULT 0,
  lastUpdated TEXT NOT NULL DEFAULT ''
);

CREATE TABLE IF NOT EXISTS VihkotappeluFactionTranslations (
  factionId TEXT NOT NULL REFERENCES VihkotappeluFactions(id) ON DELETE CASCADE,
  language TEXT NOT NULL DEFAULT '',
  name TEXT NOT NULL DEFAULT '',
  PRIMARY KEY (factionId, language)
);

-- Which units belong to which faction.
CREATE TABLE IF NOT EXISTS VihkotappeluFactionUnits (
  factionId TEXT NOT NULL REFERENCES VihkotappeluFactions(id) ON DELETE CASCADE,
  unitKey TEXT NOT NULL REFERENCES VihkotappeluUnits(unitKey) ON DELETE CASCADE,
  PRIMARY KEY (factionId, unitKey)
);

-- Vihkotappelu sprites: pixel-art icons drawn by the vihkotappelu browser
-- client. `pixels` holds the whole grid as JSON: height rows x width cells,
-- each cell null (transparent), a hex color '#rrggbb'/'#rrggbbaa', or the
-- sentinel 'side' (the owning side's faction color, see VihkotappeluFactions.color).
-- Served as part of the bulk GET /vihkotappelu/data payload.
CREATE TABLE IF NOT EXISTS VihkotappeluSprites (
  spriteKey TEXT PRIMARY KEY,
  width INTEGER NOT NULL DEFAULT 16,
  height INTEGER NOT NULL DEFAULT 16,
  pixels TEXT NOT NULL DEFAULT '[]',
  sortOrder INTEGER NOT NULL DEFAULT 0,
  lastUpdated TEXT NOT NULL DEFAULT ''
);
