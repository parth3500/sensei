#!/usr/bin/env bash
# ==============================================================================
# Sensei Study Engine - Linux Desktop Stop Script
# Cleanly terminates Sensei backend & frontend background processes
# ==============================================================================

set -uo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
PID_DIR="$ROOT_DIR/.run"

# Text styles & Colors
if [ -t 1 ]; then
    BOLD="\033[1m"
    DIM="\033[2m"
    CYAN="\033[36m"
    GREEN="\033[32m"
    YELLOW="\033[33m"
    RED="\033[31m"
    RESET="\033[0m"
else
    BOLD=""
    DIM=""
    CYAN=""
    GREEN=""
    YELLOW=""
    RED=""
    RESET=""
fi

# Load .env if present
if [ -f "$ROOT_DIR/.env" ]; then
    set -a
    # shellcheck disable=SC1090
    source "$ROOT_DIR/.env"
    set +a
fi

PORT="${PORT:-5000}"
FRONTEND_PORT="${FRONTEND_PORT:-3000}"

echo -e "${YELLOW}${BOLD}Stopping Sensei Desktop Background Services...${RESET}"

stop_pid() {
    local pid=$1
    local name=$2

    if [ -z "$pid" ]; then
        return 0
    fi

    if kill -0 "$pid" 2>/dev/null; then
        echo -e "  ${YELLOW}▶${RESET} Sending SIGTERM to $name (PID: $pid)..."
        kill "$pid" 2>/dev/null || true

        # Wait up to 5 seconds
        for ((i=0; i<5; i++)); do
            if ! kill -0 "$pid" 2>/dev/null; then
                echo -e "  ${GREEN}✔${RESET} $name (PID: $pid) stopped gracefully."
                return 0
            fi
            sleep 1
        done

        # Force kill if still running
        if kill -0 "$pid" 2>/dev/null; then
            echo -e "  ${RED}⚠${RESET} Force-killing $name (PID: $pid)..."
            kill -9 "$pid" 2>/dev/null || true
        fi
    fi
}

stop_port() {
    local port=$1
    local name=$2

    local pids=""
    if command -v lsof >/dev/null 2>&1; then
        pids=$(lsof -ti :"$port" 2>/dev/null || true)
    elif command -v fuser >/dev/null 2>&1; then
        pids=$(fuser "$port/tcp" 2>/dev/null | tr -d ' ' || true)
    fi

    if [ -n "$pids" ]; then
        for pid in $pids; do
            stop_pid "$pid" "$name listening on port $port"
        done
    fi
}

# 1. Stop recorded PIDs
if [ -f "$PID_DIR/backend.pid" ]; then
    backend_pid=$(cat "$PID_DIR/backend.pid" 2>/dev/null || true)
    stop_pid "$backend_pid" "Backend process (from pidfile)"
    rm -f "$PID_DIR/backend.pid"
fi

if [ -f "$PID_DIR/frontend.pid" ]; then
    frontend_pid=$(cat "$PID_DIR/frontend.pid" 2>/dev/null || true)
    stop_pid "$frontend_pid" "Frontend process (from pidfile)"
    rm -f "$PID_DIR/frontend.pid"
fi

# 2. Stop any remaining processes bound to the ports
stop_port "$PORT" "Sensei Backend"
stop_port "$FRONTEND_PORT" "Sensei Frontend"

# 3. Clean up any orphaned dotnet/node processes rooted in this directory
orphan_backend=$(pgrep -f "dotnet.*backend" 2>/dev/null || true)
for pid in $orphan_backend; do
    # Only kill if working dir matches ROOT_DIR
    if pwdx "$pid" 2>/dev/null | grep -q "$ROOT_DIR" 2>/dev/null; then
        stop_pid "$pid" "Orphaned backend instance"
    fi
done

echo -e "\n${GREEN}✔ All Sensei background services have been stopped.${RESET}"
