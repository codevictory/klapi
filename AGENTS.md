# AGENTS.md — working on klapi

Instructions for Mistral Vibe (the AI agent) when carrying out requests in this
repository. Read this file before making changes.

## Keep this file up to date

**Every time something changes in the code, update this AGENTS.md too.** That
means: new endpoints, routes, components, database tables/columns, just
recipes, environment variables, auth roles, supported languages, changed
commands, or new conventions. If a change makes anything below inaccurate or
incomplete, fix it in the same commit as the code change. Do not let this file
drift out of sync with the repo.

## Project overview

klapi is a multi-user API and data store ("Tietokonepaja" service). End users
edit their own data through a browser UI and call it from other services of
theirs. The UI and its texts are primarily in Finnish; supported app languages
are `fi`, `en`, `sk`.

Layout:

```
api/                  .NET 10 ASP.NET Core API (solution: api/Api.slnx, project: api/App)
  App/Endpoints/      minimal-API endpoint groups (Admin, Auth, Feedback, Lok, System, Tarot, User, Vihkotappelu)
  App/Services/       business logic (LokService, TarotService, UserService, VihkotappeluService, ...)
  App/Models/         DTOs / models
  App/Auth/           auth handlers (JWT bearer, site-to-site API key, development handler)
  App/Program.cs      startup; also runs incremental SQLite migrations at boot
  App.Tests/          xunit tests
  Tests/Http/         .http request files for manual endpoint testing
  Database/init.sql   schema for `just db-setup` (SQLite, db file: Database/klapi.db)
ui/                   React 18 + Vite + react-router-dom v6 + Recoil + Tailwind 4
  src/routes/         file-based routes (index, about, login, feedback, management, ...)
  src/api/            the ONLY place UI code talks to the backend (index.ts, url.ts, url.test.ts)
  src/components/     shared components (Nav, RoleGuard, ConfirmDialog, ...)
  src/state/          Recoil state
  src/i18n.ts         translations (fi / en / sk)
scripts/              start.sh (tmux dev session), deploy.sh (production deploy)
justfile              all task recipes — prefer these over raw commands
```

## Toolchain and commands

Prerequisites: `just`, `dotnet` (SDK 10.0.100 per `api/global.json`), `bun`,
`sqlite3`, `tmux` (for `just dev`).

Always prefer the just recipes (`just --list`):

| Task | Command |
| --- | --- |
| Start dev session (tmux, API + UI) | `just dev` |
| API build / test / watch | `just api-build`, `just api-test`, `just api-watch` |
| UI install / dev / build / test / lint | `just ui-install`, `just ui-dev`, `just ui-build`, `just ui-test`, `just ui-lint` |
| Database setup / reset / shell | `just db-setup`, `just db-reset`, `just db-shell` |
| Reset TarotCards to the canonical 78 cards | `just reset-tarot-cards` |
| Reset vihkotappelu game data to the canonical seed | `just reset-vihkotappelu` |
| Production deploy | `just deploy` — **only when the user explicitly asks** |

## API conventions (.NET, `api/`)

- Minimal APIs. Add new endpoints in a suitable file under `App/Endpoints/`
  (create a new one if none fits), business logic in `App/Services/`, models in
  `App/Models/`.
- Data access is SQLite via `Microsoft.Data.Sqlite`. New tables go into
  `api/Database/init.sql` (used by `just db-setup`); incremental column changes
  are applied by migrations in `App/Program.cs` at startup.
- Auth: JWT bearer tokens for users, a site-to-site API key
  (`Auth:SiteToSite:ApiKey`) for machine clients, plus a development auth
  handler. Roles used by the UI: `lok`, `admin`, `tarot-admin`. The site-to-site
  key grants the machine-only roles `tarot` and `vihkotappelu` (one per
  consuming site); those roles are not assignable to portal users and the
  machine endpoints (`/tarot/cards`, `/vihkotappelu/data`) authenticate
  exclusively via the `SiteApiKey` scheme — never via portal JWT.
- Site-to-site data endpoints: `GET/PUT /tarot/cards` (TarotCards +
  TarotCardTranslations tables) and `GET/PUT /vihkotappelu/data`
  (VihkotappeluUnits, VihkotappeluUnitTranslations, VihkotappeluFactions,
  VihkotappeluFactionTranslations, VihkotappeluFactionUnits,
  VihkotappeluSprites tables) hold the card texts / game unit+faction+sprite
  data consumed by the tarot and vihkotappelu sites. Base columns are the
  canonical English texts; per-language texts live in the translation tables.
  Consumers fetch everything in ONE bulk call and push updates with the bulk
  PUT. The canonical SEED for these datasets lives in this repo as SQL:
  `api/Database/reset-tarot-cards.sql` and
  `api/Database/reset-vihkotappelu.sql` (run via `just reset-tarot-cards` /
  `just reset-vihkotappelu`); the vihkotappelu repo holds no data files — its
  `tools/fetch_game_data.js` / `tools/sync_to_klapi.js` dump and push a JSON
  payload over the bulk endpoints.
- Vihkotappelu sprites: `VihkotappeluSprites.pixels` stores each sprite as a
  JSON grid (height rows x width cells; cell = null transparent, `#rrggbb` /
  `#rrggbbaa`, or the sentinel `"side"` = the owning side's faction color).
  GET returns them as `sprites: [{ spriteKey, width, height, sortOrder,
  pixels }]`. On PUT, `sprites` is optional: when present it fully replaces
  the table and every `unit.sprite` must reference one of its spriteKeys
  (400 otherwise, along with malformed grids/colors); when absent the stored
  sprites are kept. Single-pixel edits in SQL:
  `UPDATE VihkotappeluSprites SET pixels = json_set(pixels, '$[y][x]', '#ff0000') WHERE spriteKey = 'soldiers_of_falroth';`
- Secrets come from configuration / environment variables
  (`KLAPI_ADMIN_PASSWORD`, `KLAPI_TAROT_ADMIN_PASSWORD`, `KLAPI_SITE_API_KEY`,
  `Auth:SigningKey` ≥ 32 chars). Never hardcode secrets or commit them.
- `api/Database/klapi.db` is gitignored — never commit database files.
- Add a `.http` file under `api/Tests/Http/` for new endpoint groups, and cover
  new behavior in `api/App.Tests/`.

## UI conventions (React, `ui/`)

- **All backend calls go through `ui/src/api/index.ts`**, which builds URLs with
  `buildApiUrl()` from `ui/src/api/url.ts`:

  ```ts
  const API_BASE_URL =
    (typeof process !== "undefined" ? process.env?.API_BASE_URL : undefined) ??
    import.meta.env.VITE_API_BASE_URL ??
    "/api";

  export const buildApiUrl = (path: string) =>
    `${API_BASE_URL.replace(/\/+$/, "")}/${path.replace(/^\/+/, "")}`;
  ```

  Never hardcode or duplicate API URLs in components — add a typed function to
  `src/api/index.ts` and call that. The base URL comes from
  `API_BASE_URL` / `VITE_API_BASE_URL` and defaults to `/api`; do not change
  this fallback behavior without updating `src/api/url.test.ts` and this file.
- Authenticated calls send `Authorization: Bearer <token>` with the token from
  `localStorage["session-token"]` (see `getAccessToken()` in `src/api/index.ts`).
- New pages are route files in `src/routes/` (react-router v6 file routing);
  register navigation in `src/components/Nav.tsx` and guard by role with
  `src/components/RoleGuard.tsx` / `src/auth/roles.ts` when needed.
- All user-visible strings go through `useT()` from `src/i18n.ts` with keys for
  all three languages (`fi`, `en`, `sk`).
- Styling: Tailwind 4 via `src/app.css`. Lint/format with Biome
  (`just ui-lint`, `bun run lint:fix` in `ui/`). Tests: Vitest
  (`just ui-test`, e.g. `src/api/url.test.ts`).

## Working rules for the agent

1. Read the relevant files (and this AGENTS.md) before editing. Make minimal
   diffs and match the existing style of the file you are editing.
2. After changing API code, run `just api-build` and `just api-test`. After
   changing UI code, run `just ui-test`, `just ui-lint`, and `just ui-build`.
   Fix what you broke before finishing.
3. When adding an API endpoint, also add the UI api function (if the UI needs
   it), i18n keys, an `.http` test file, and unit tests where they fit.
4. When changing the database schema, update `init.sql` and add a startup
   migration in `Program.cs` for existing databases.
5. Do not run `just deploy`, restart services, or modify anything outside the
   repo unless the user explicitly asks.
6. Commit messages: short, English, imperative (e.g. "Add feedback endpoint"),
   matching the existing history. Do not commit `klapi.db` or secrets.
7. If a request is ambiguous, resolve it from the code and this file first;
   ask only when the answer would materially change the result.
