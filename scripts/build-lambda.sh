#!/usr/bin/env bash
# Packages the API project as a Lambda-ready ZIP.
# Usage: ./scripts/build-lambda.sh [output-dir]
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
OUTPUT_DIR="${1:-$REPO_ROOT/artifacts}"

PUBLISH_DIR="$OUTPUT_DIR/publish"
ZIP_PATH="$OUTPUT_DIR/lambda.zip"

echo "==> Building Lambda artifact..."
mkdir -p "$OUTPUT_DIR"

dotnet publish "$REPO_ROOT/src/Api/Api.csproj" \
  --configuration Release \
  --runtime linux-x64 \
  --no-self-contained \
  --output "$PUBLISH_DIR"

echo "==> Packaging ZIP..."
(cd "$PUBLISH_DIR" && zip -r "$ZIP_PATH" .)

echo "==> Artifact: $ZIP_PATH ($(du -sh "$ZIP_PATH" | cut -f1))"
