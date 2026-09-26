#!/usr/bin/env bash
# ==============================================================================
# Sensei - Startup Orchestrator (C# .NET 8 Backend + Next.js Frontend)
# ==============================================================================

set -e

DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
if [ -d "$DIR/dotnet/usr/lib/dotnet" ]; then
    export DOTNET_ROOT="$DIR/dotnet/usr/lib/dotnet"
    export PATH="$DOTNET_ROOT:$PATH"
elif [ -d "/home/parth/prog/ai/dotnet/usr/lib/dotnet" ]; then
    export DOTNET_ROOT="/home/parth/prog/ai/dotnet/usr/lib/dotnet"
    export PATH="$DOTNET_ROOT:$PATH"
fi

if [ -f "$DIR/.env" ]; then
    set -a
    source "$DIR/.env"
    set +a
fi

echo "=========================================================="
echo " Starting Sensei (Component Assembly Architecture)"
echo " Backend: C# (.NET 8.0 Web API) on http://localhost:5000"
echo " Frontend: Next.js 14 on http://localhost:3000"
echo "=========================================================="

# Check backend
if ! curl -s http://localhost:5000/health > /dev/null 2>&1; then
    echo "Starting C# Backend..."
    (cd "$DIR/backend" && setsid dotnet run > "$DIR/backend.log" 2>&1 &)
    for i in {1..20}; do
        if curl -s http://localhost:5000/health > /dev/null 2>&1; then
            echo "C# Backend is healthy!"
            break
        fi
        sleep 1
    done
fi

# Check frontend
if ! curl -s http://localhost:3000/ > /dev/null 2>&1; then
    echo "Starting Next.js Frontend..."
    (cd "$DIR/frontend" && setsid npm run start > "$DIR/frontend.log" 2>&1 &)
    for i in {1..20}; do
        if curl -s http://localhost:3000/ > /dev/null 2>&1; then
            echo "Next.js Frontend is healthy!"
            break
        fi
        sleep 1
    done
fi

echo ""
echo "✅ Both services are running!"
echo "➡️  Open your browser at: http://localhost:3000"
echo "🔑 Default Passphrase: your-secure-passphrase-here (or check .env)"
echo "=========================================================="
