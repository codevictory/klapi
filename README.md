# klapi

Klapi on Tietokonepajan tarjoama monikäyttöinen API rajapinta sekä tietovarasto. Loppukäyttäjä voi muokata omistamaansa dataa selainkäyttöliittymän kautta ja kutsua sitä muista palveluistaan.

## Kehitys ja ajokomennot (`just`)

Projektissa on valmiit reseptit [justfile](justfile)-tiedostossa.

Esivaatimukset:
- `just`
- `dotnet`
- `bun`
- `sqlite3` (DB-resepteihin)

Listaa kaikki reseptit:
- `just --list`

Sovelluksen käynnistys:
- `just dev`

API (.NET):
- `just api-restore`
- `just api-build`
- `just api-clean`
- `just api-run`
- `just api-watch`
- `just api-test`
- `just api-publish`

UI:
- `just ui-install`
- `just ui-dev`
- `just ui-build`
- `just ui-test`
- `just ui-lint`

Tietokanta (SQLite):
- `just db-setup`
- `just db-reset`
- `just db-shell`
