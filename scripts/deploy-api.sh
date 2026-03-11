#!/usr/bin/env bash
set -euo pipefail

REPO_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

echo "==> Pulling latest changes..."
git -C "$REPO_DIR" pull

echo "==> Publishing API..."
dotnet publish "$REPO_DIR/api/App/App.csproj" \
  -c Release \
  -o "$REPO_DIR/api/publish"

echo "==> Restarting service..."
sudo systemctl restart klapi-api

echo "==> Done."
