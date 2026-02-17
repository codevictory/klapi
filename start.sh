#!/usr/bin/env bash
set -euo pipefail

SESSION_NAME="klapi"
ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
API_DIR="$ROOT_DIR/api"
UI_DIR="$ROOT_DIR/ui"
RESTART=false

if [[ "${1:-}" == "--restart" ]]; then
	RESTART=true
elif [[ -n "${1:-}" ]]; then
	echo "Usage: $0 [--restart]" >&2
	exit 1
fi

if ! command -v tmux >/dev/null 2>&1; then
	echo "tmux is not installed or not in PATH" >&2
	exit 1
fi

ensure_app_window() {
	if ! tmux list-windows -t "$SESSION_NAME" -F '#{window_name}' | grep -qx 'app'; then
		tmux new-window -t "$SESSION_NAME" -n app -c "$UI_DIR"
	fi

	local pane_count
	pane_count="$(tmux list-panes -t "$SESSION_NAME":app | wc -l | tr -d ' ')"
	if [[ "$pane_count" -lt 2 ]]; then
		tmux split-window -v -t "$SESSION_NAME":app -c "$API_DIR"
	fi
}

start_or_restart_services() {
	ensure_app_window
	tmux respawn-pane -k -t "$SESSION_NAME":app.0 -c "$UI_DIR" "bun dev"
	tmux respawn-pane -k -t "$SESSION_NAME":app.1 -c "$API_DIR" "dotnet run --project $API_DIR/App/App.csproj"
	tmux select-pane -t "$SESSION_NAME":app.0
	tmux select-window -t "$SESSION_NAME":app
}

if tmux has-session -t "$SESSION_NAME" 2>/dev/null; then
	if [[ "$RESTART" == "true" ]]; then
		echo "Restarting services in existing session '$SESSION_NAME'..."
		start_or_restart_services
		exec tmux attach -t "$SESSION_NAME"
	else
		echo "Session '$SESSION_NAME' already exists. Attaching..."
		exec tmux attach -t "$SESSION_NAME"
	fi
fi

tmux new-session -d -s "$SESSION_NAME" -n app -c "$UI_DIR"
start_or_restart_services
exec tmux attach -t "$SESSION_NAME"
