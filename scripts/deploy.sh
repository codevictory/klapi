#!/usr/bin/env bash
set -euo pipefail

REPO_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

echo "==> Pulling latest changes..."
git -C "$REPO_DIR" pull

echo "==> Publishing API..."
dotnet publish "$REPO_DIR/api/App/App.csproj" \
  -c Release \
  -o "$REPO_DIR/api/publish"

echo "==> Restarting API service..."
sudo systemctl restart klapi-api

echo "==> Installing UI dependencies..."
cd "$REPO_DIR/ui" && bun install --frozen-lockfile

echo "==> Building UI..."
bun run build

echo "==> Restarting UI process..."
pm2 restart klapi-ui

echo "==> Done."
