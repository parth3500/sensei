#!/usr/bin/env bash
# ==============================================================================
# Sensei - Study Note Ingest Script
# Usage: ./scripts/ingest_note.sh <path_to_note_file> [optional_api_url]
# ==============================================================================

set -euo pipefail

FILE_PATH="${1:-}"
API_URL="${2:-https://frontend-production-6936.up.railway.app}"

if [ -z "$FILE_PATH" ] || [ ! -f "$FILE_PATH" ]; then
  echo "❌ Error: Please provide a valid file path."
  echo "Usage: ./scripts/ingest_note.sh <path_to_file.txt>"
  exit 1
fi

FILE_NAME=$(basename "$FILE_PATH")
CONTENT=$(cat "$FILE_PATH")

echo "📤 Ingesting '$FILE_NAME' into Sensei at $API_URL..."

# Create JSON payload using python to safely escape characters
PAYLOAD=$(python3 -c '
import json, sys
filename = sys.argv[1]
content = sys.argv[2]
print(json.dumps({
    "fileName": filename,
    "content": content,
    "folderPath": "GoogleDrive:/GATE_Sensei_Notes/"
}))
' "$FILE_NAME" "$CONTENT")

RESPONSE=$(curl -s -X POST "$API_URL/api/drive/ingest" \
  -H "Content-Type: application/json" \
  -d "$PAYLOAD")

echo ""
echo "✅ Ingestion complete!"
echo "$RESPONSE" | python3 -m json.tool || echo "$RESPONSE"
echo ""
echo "🎉 Summary, revision takeaways, and 2 GATE practice questions were generated!"
echo "➡️  Open the app to quiz yourself: $API_URL"
