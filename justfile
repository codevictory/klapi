set shell := ["bash", "-eu", "-o", "pipefail", "-c"]

root_dir := justfile_directory()
api_project := root_dir + "/api/App/App.csproj"
api_solution := root_dir + "/api/Api.slnx"
ui_dir := root_dir + "/ui"
db_dir := root_dir + "/api/Database"
db_file := db_dir + "/klapi.db"
db_init_sql := db_dir + "/init.sql"
db_reset_tarot_cards_sql := db_dir + "/reset-tarot-cards.sql"

default:
	@just --list

dev:
	{{root_dir}}/scripts/start.sh

api-restore:
	dotnet restore {{api_project}}

api-build:
	dotnet build {{api_project}}

api-clean:
	dotnet clean {{api_project}}

api-run:
	dotnet run --project {{api_project}}

api-watch:
	dotnet watch --project {{api_project}} run

api-test:
	dotnet test {{api_solution}}

api-publish:
	dotnet publish {{api_project}} -c Release -o {{root_dir}}/api/publish

deploy:
	{{root_dir}}/scripts/deploy.sh

ui-install:
	cd {{ui_dir}} && bun install

ui-dev:
	cd {{ui_dir}} && bun dev

ui-build:
	cd {{ui_dir}} && bun run build

ui-test:
	cd {{ui_dir}} && bun run test

ui-lint:
	cd {{ui_dir}} && bun run lint

db-setup:
	mkdir -p {{db_dir}}
	if ! command -v sqlite3 >/dev/null 2>&1; then echo "sqlite3 is required for db-setup" >&2; exit 1; fi
	sqlite3 {{db_file}} < {{db_init_sql}}

db-reset:
	rm -f {{db_file}}
	just db-setup

db-shell:
	if ! command -v sqlite3 >/dev/null 2>&1; then echo "sqlite3 is required for db-shell" >&2; exit 1; fi
	sqlite3 {{db_file}}

# Reset the TarotCards table to exactly the 78 cards from the tarot repo's
# data/cards.json (see reset-tarot-cards.sql): everything already in the
# table is deleted, then the json entries are inserted.
reset-tarot-cards:
	@if ! command -v sqlite3 >/dev/null 2>&1; then echo "sqlite3 is required for reset-tarot-cards" >&2; exit 1; fi
	sqlite3 {{db_file}} < {{db_reset_tarot_cards_sql}}
	@echo "TarotCards reset in {{db_file}}: $(sqlite3 {{db_file}} 'SELECT COUNT(*) FROM TarotCards;') cards"
