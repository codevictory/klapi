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

## Tuotantoon julkaisu

```bash
just deploy
```

Skripti ([scripts/deploy.sh](scripts/deploy.sh)) suorittaa:

1. `git pull` — hakee uusimmat muutokset
2. `dotnet publish` — kääntää API:n release-tilaan
3. `db-setup` — ajaa `init.sql`:n tietokantaan (luo puuttuvat taulut)
4. `systemctl restart klapi-api` — käynnistää API-palvelun uudelleen
5. `bun install && bun run build` — asentaa riippuvuudet ja kääntää UI:n
6. `pm2 restart klapi-ui` — käynnistää UI-prosessin uudelleen

> Inkrementaaliset skeemamuutokset (sarakkeiden lisäykset ym.) ajetaan automaattisesti API:n käynnistyksessä `Program.cs`-migraatioiden kautta.
