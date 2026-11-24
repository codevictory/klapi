-- Resets the vihkotappelu game data (VihkotappeluUnits, VihkotappeluFactions
-- and their translation/link tables) to the canonical seed: the base units
-- plus the two factions Dalmen and Zhaglon, with English ('en') and Finnish
-- ('fi') names. This file IS the seed — the vihkotappelu repo holds no data
-- files; the game server reads this data via GET /vihkotappelu/data.
-- Run via: just reset-vihkotappelu   (from the klapi repo root)
-- Or manually: sqlite3 api/Database/klapi.db < api/Database/reset-vihkotappelu.sql

PRAGMA foreign_keys = ON;

BEGIN TRANSACTION;

DELETE FROM VihkotappeluFactionUnits;
DELETE FROM VihkotappeluFactionTranslations;
DELETE FROM VihkotappeluFactions;
DELETE FROM VihkotappeluUnitTranslations;
DELETE FROM VihkotappeluUnits;

INSERT INTO VihkotappeluUnits (unitKey, name, description, hp, attack, defence, movement, range, minRange, sprite, sortOrder, lastUpdated) VALUES
  ('spearman', 'Spearman', 'Sturdy front-line fighter with a long spear. Hits hard up close and at reach 2.', 30, 8, 3, 3, 2, 1, 'spearman', 0, strftime('%Y-%m-%dT%H:%M:%fZ', 'now')),
  ('archer', 'Archer', 'Fragile but shoots far. Cannot hit targets on adjacent squares.', 18, 7, 1, 3, 4, 2, 'archer', 1, strftime('%Y-%m-%dT%H:%M:%fZ', 'now')),
  ('mage', 'Mage', 'Powerful spellcaster. If a side''s mage falls, that side loses the battle.', 22, 12, 0, 2, 3, 1, 'mage', 2, strftime('%Y-%m-%dT%H:%M:%fZ', 'now')),
  ('soldiers_of_falroth', 'Soldiers of Falroth', '', 26, 9, 3, 3, 1, 1, 'soldiers_of_falroth', 3, strftime('%Y-%m-%dT%H:%M:%fZ', 'now')),
  ('knights_of_gremloth', 'Knights of Gremloth', '', 20, 12, 2, 5, 1, 1, 'knights_of_gremloth', 4, strftime('%Y-%m-%dT%H:%M:%fZ', 'now')),
  ('arbalesters', 'Arbalesters', '', 14, 8, 1, 3, 4, 2, 'arbalesters', 5, strftime('%Y-%m-%dT%H:%M:%fZ', 'now')),
  ('catapult', 'Catapult', '', 10, 10, 0, 2, 5, 3, 'catapult', 6, strftime('%Y-%m-%dT%H:%M:%fZ', 'now')),
  ('riders_of_nezek', 'Riders of Nezek', '', 13, 6, 2, 5, 4, 1, 'riders_of_nezek', 7, strftime('%Y-%m-%dT%H:%M:%fZ', 'now')),
  ('knights_of_zhaglon', 'Knights of Zhaglon', '', 22, 8, 3, 6, 1, 1, 'knights_of_zhaglon', 8, strftime('%Y-%m-%dT%H:%M:%fZ', 'now')),
  ('archers_of_grishtalon', 'Archers of Grishtalon', '', 15, 6, 1, 3, 6, 2, 'archers_of_grishtalon', 9, strftime('%Y-%m-%dT%H:%M:%fZ', 'now')),
  ('war_chariot', 'War chariot', '', 25, 7, 4, 6, 1, 1, 'war_chariot', 10, strftime('%Y-%m-%dT%H:%M:%fZ', 'now'));

INSERT INTO VihkotappeluUnitTranslations (unitKey, language, name, description) VALUES
  ('spearman', 'en', 'Spearman', 'Sturdy front-line fighter with a long spear. Hits hard up close and at reach 2.'),
  ('spearman', 'fi', 'Keihäsmies', 'Tukeva etulinjan taistelija pitkällä keihäällä. Iskee kovaa lähietäisyydeltä ja kahden ruudun päästä.'),
  ('archer', 'en', 'Archer', 'Fragile but shoots far. Cannot hit targets on adjacent squares.'),
  ('archer', 'fi', 'Jousimies', 'Hauras mutta ampuu kauas. Ei voi osua viereisen ruudun kohteisiin.'),
  ('mage', 'en', 'Mage', 'Powerful spellcaster. If a side''s mage falls, that side loses the battle.'),
  ('mage', 'fi', 'Magi', 'Voimakas loitsija. Jos puolen magi kaatuu, kyseinen puoli häviää taistelun.'),
  ('soldiers_of_falroth', 'en', 'Soldiers of Falroth', ''),
  ('soldiers_of_falroth', 'fi', 'Falrothin sotilaat', ''),
  ('knights_of_gremloth', 'en', 'Knights of Gremloth', ''),
  ('knights_of_gremloth', 'fi', 'Gremlothin ritarit', ''),
  ('arbalesters', 'en', 'Arbalesters', ''),
  ('arbalesters', 'fi', 'Varsijousimiehet', ''),
  ('catapult', 'en', 'Catapult', ''),
  ('catapult', 'fi', 'Katapultti', ''),
  ('riders_of_nezek', 'en', 'Riders of Nezek', ''),
  ('riders_of_nezek', 'fi', 'Nezekin ratsastajat', ''),
  ('knights_of_zhaglon', 'en', 'Knights of Zhaglon', ''),
  ('knights_of_zhaglon', 'fi', 'Zhaglonin ritarit', ''),
  ('archers_of_grishtalon', 'en', 'Archers of Grishtalon', ''),
  ('archers_of_grishtalon', 'fi', 'Grishtalonin jousimiehet', ''),
  ('war_chariot', 'en', 'War chariot', ''),
  ('war_chariot', 'fi', 'Sotavaunut', '');

INSERT INTO VihkotappeluFactions (id, name, color, sortOrder, lastUpdated) VALUES
  ('dalmen', 'Dalmen', '#22c55e', 0, strftime('%Y-%m-%dT%H:%M:%fZ', 'now')),
  ('zhaglon', 'Zhaglon', '#a855f7', 1, strftime('%Y-%m-%dT%H:%M:%fZ', 'now'));

INSERT INTO VihkotappeluFactionTranslations (factionId, language, name) VALUES
  ('dalmen', 'en', 'Dalmen'),
  ('dalmen', 'fi', 'Dalmen'),
  ('zhaglon', 'en', 'Zhaglon'),
  ('zhaglon', 'fi', 'Zhaglon');

INSERT INTO VihkotappeluFactionUnits (factionId, unitKey) VALUES
  ('dalmen', 'soldiers_of_falroth'),
  ('dalmen', 'knights_of_gremloth'),
  ('dalmen', 'arbalesters'),
  ('dalmen', 'catapult'),
  ('zhaglon', 'riders_of_nezek'),
  ('zhaglon', 'knights_of_zhaglon'),
  ('zhaglon', 'archers_of_grishtalon'),
  ('zhaglon', 'war_chariot');

COMMIT;
