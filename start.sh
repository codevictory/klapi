#!/usr/bin/env bash
set -euo pipefail

SESSION_NAME="klapi"
ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
API_DIR="$ROOT_DIR/api"
UI_DIR="$ROOT_DIR/ui"

if ! command -v tmux >/dev/null 2>&1; then
	echo "tmux is not installed or not in PATH" >&2
	exit 1
fi

if tmux has-session -t "$SESSION_NAME" 2>/dev/null; then
	echo "Session '$SESSION_NAME' already exists. Attaching..."
	exec tmux attach -t "$SESSION_NAME"
fi

tmux new-session -d -s "$SESSION_NAME" -n app -c "$UI_DIR"
tmux send-keys -t "$SESSION_NAME":app "bun dev" C-m

tmux split-window -v -t "$SESSION_NAME":app -c "$API_DIR"
tmux send-keys -t "$SESSION_NAME":app.1 "dotnet run --project $API_DIR/Public/Public.csproj" C-m

tmux select-pane -t "$SESSION_NAME":app.0
tmux select-window -t "$SESSION_NAME":app
exec tmux attach -t "$SESSION_NAME"
