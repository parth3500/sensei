#!/usr/bin/env bash
# ==============================================================================
# Sensei Study Engine - Linux Desktop Launcher
# Launches .NET 8 Backend + Next.js Frontend + Standalone Desktop App Window
# ==============================================================================

set -uo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
PID_DIR="$ROOT_DIR/.run"
mkdir -p "$PID_DIR"

# Text styles & Colors
if [ -t 1 ]; then
    BOLD="\033[1m"
    DIM="\033[2m"
    CYAN="\033[36m"
    GREEN="\033[32m"
    YELLOW="\033[33m"
    BLUE="\033[34m"
    MAGENTA="\033[35m"
    RED="\033[31m"
    RESET="\033[0m"
else
    BOLD=""
    DIM=""
    CYAN=""
    GREEN=""
    YELLOW=""
    BLUE=""
    MAGENTA=""
    RED=""
    RESET=""
fi

# Print Sensei ASCII Banner
print_banner() {
    echo -e "${CYAN}${BOLD}"
    cat << "EOF"
  ███████╗███████╗███╗   ██╗███████╗███████╗██╗
  ██╔════╝██╔════╝████╗  ██║██╔════╝██╔════╝██║
  ███████╗█████╗  ██╔██╗ ██║███████╗█████╗  ██║
  ╚════██║██╔══╝  ██║╚██╗██║╚════██║██╔══╝  ██║
  ███████║███████╗██║ ╚████║███████║███████╗██║
  ╚══════╝╚══════╝╚═╝  ╚═══╝╚══════╝╚══════╝╚═╝
EOF
    echo -e "${MAGENTA}${BOLD}   ★ S E N S E I   S T U D Y   E N G I N E ★${RESET}"
    echo -e "${DIM}   Personal AI Study Coach & Engineering Prep Platform${RESET}"
    echo -e "${CYAN}------------------------------------------------------------${RESET}"
}

# ------------------------------------------------------------------------------
# 1. Environment Detection & Configuration
# ------------------------------------------------------------------------------
setup_environment() {
    # .NET 8 Detection
    if [ -d "$ROOT_DIR/dotnet/usr/lib/dotnet" ]; then
        export DOTNET_ROOT="$ROOT_DIR/dotnet/usr/lib/dotnet"
        export PATH="$DOTNET_ROOT:$PATH"
    elif [ -d "/home/parth/prog/ai/dotnet/usr/lib/dotnet" ]; then
        export DOTNET_ROOT="/home/parth/prog/ai/dotnet/usr/lib/dotnet"
        export PATH="$DOTNET_ROOT:$PATH"
    elif [ -d "$HOME/.dotnet" ]; then
        export DOTNET_ROOT="$HOME/.dotnet"
        export PATH="$DOTNET_ROOT:$PATH"
    elif [ -d "/usr/share/dotnet" ]; then
        export DOTNET_ROOT="/usr/share/dotnet"
        export PATH="$DOTNET_ROOT:$PATH"
    fi

    # Node.js / NVM Detection (essential for desktop GUI launches)
    if ! command -v node >/dev/null 2>&1 || ! command -v npm >/dev/null 2>&1; then
        if [ -s "$HOME/.nvm/nvm.sh" ]; then
            export NVM_DIR="$HOME/.nvm"
            # shellcheck disable=SC1091
            [ -s "$NVM_DIR/nvm.sh" ] && \. "$NVM_DIR/nvm.sh"
        fi
        for nvm_path in "$HOME/.nvm/versions/node/"*"/bin" "$HOME/.local/bin" "/usr/local/bin"; do
            if [ -d "$nvm_path" ]; then
                export PATH="$nvm_path:$PATH"
            fi
        done
    fi

    # Source .env if present
    if [ -f "$ROOT_DIR/.env" ]; then
        set -a
        # shellcheck disable=SC1090
        source "$ROOT_DIR/.env"
        set +a
    fi

    export PORT="${PORT:-5000}"
    export FRONTEND_PORT="${FRONTEND_PORT:-3000}"
    export ASPNETCORE_URLS="http://0.0.0.0:${PORT}"
    export BACKEND_URL="http://localhost:${PORT}"
    export NEXT_PUBLIC_API_URL="http://localhost:${PORT}"
}

# ------------------------------------------------------------------------------
# ASCII Progress Bar
# ------------------------------------------------------------------------------
draw_progress_bar() {
    local current=$1
    local total=$2
    local label=$3
    local bar_len=24
    local filled=$(( current * bar_len / total ))
    local empty=$(( bar_len - filled ))

    local bar="["
    for ((i=0; i<filled; i++)); do bar+="="; done
    if [ "$filled" -lt "$bar_len" ]; then bar+=">"; fi
    for ((i=0; i<empty-1; i++)); do bar+=" "; done
    bar+="]"

    printf "\r  ${YELLOW}%s${RESET} %-36s ${CYAN}%s${RESET} (%2d/%2ds)" "$bar" "$label" "..." "$current" "$total"
}

# ------------------------------------------------------------------------------
# Service Health Checks
# ------------------------------------------------------------------------------
is_backend_healthy() {
    curl -fsS --max-time 1 "http://localhost:${PORT}/health" >/dev/null 2>&1
}

is_frontend_healthy() {
    curl -fsS --max-time 1 "http://localhost:${FRONTEND_PORT}/" >/dev/null 2>&1
}

# ------------------------------------------------------------------------------
# Start Backend (.NET 8 Web API)
# ------------------------------------------------------------------------------
start_backend() {
    if is_backend_healthy; then
        echo -e "  ${GREEN}✔${RESET} Backend is already running on ${CYAN}http://localhost:${PORT}${RESET}"
        return 0
    fi

    echo -e "  ${BLUE}▶${RESET} Starting C# .NET 8 Backend on port ${PORT}..."
    mkdir -p "$ROOT_DIR/backend/Data"

    (
        cd "$ROOT_DIR/backend"
        setsid dotnet run --urls "http://0.0.0.0:${PORT}" > "$ROOT_DIR/backend.log" 2>&1 &
        echo $! > "$PID_DIR/backend.pid"
    )

    local max_retries=30
    local retries=0
    while [ $retries -lt $max_retries ]; do
        retries=$((retries + 1))
        draw_progress_bar "$retries" "$max_retries" "Waiting for Backend Health"
        if is_backend_healthy; then
            echo -e "\r  ${GREEN}✔${RESET} C# .NET 8 Backend is healthy!             [http://localhost:${PORT}]"
            return 0
        fi
        sleep 1
    done

    echo -e "\n  ${RED}✖ Backend failed to report healthy within ${max_retries}s.${RESET}"
    echo -e "  ${DIM}Check logs at: $ROOT_DIR/backend.log${RESET}"
    return 1
}

# ------------------------------------------------------------------------------
# Start Frontend (Next.js 14)
# ------------------------------------------------------------------------------
start_frontend() {
    if is_frontend_healthy; then
        echo -e "  ${GREEN}✔${RESET} Frontend is already running on ${CYAN}http://localhost:${FRONTEND_PORT}${RESET}"
        return 0
    fi

    echo -e "  ${BLUE}▶${RESET} Starting Next.js Frontend on port ${FRONTEND_PORT}..."
    (
        cd "$ROOT_DIR/frontend"
        if [ -d "$ROOT_DIR/frontend/.next" ]; then
            setsid npm run start -- -p "${FRONTEND_PORT}" > "$ROOT_DIR/frontend.log" 2>&1 &
        else
            setsid npm run dev -- -p "${FRONTEND_PORT}" > "$ROOT_DIR/frontend.log" 2>&1 &
        fi
        echo $! > "$PID_DIR/frontend.pid"
    )

    local max_retries=30
    local retries=0
    while [ $retries -lt $max_retries ]; do
        retries=$((retries + 1))
        draw_progress_bar "$retries" "$max_retries" "Waiting for Frontend Server"
        if is_frontend_healthy; then
            echo -e "\r  ${GREEN}✔${RESET} Next.js Frontend is healthy!          [http://localhost:${FRONTEND_PORT}]"
            return 0
        fi
        sleep 1
    done

    echo -e "\n  ${RED}✖ Frontend failed to respond within ${max_retries}s.${RESET}"
    echo -e "  ${DIM}Check logs at: $ROOT_DIR/frontend.log${RESET}"
    return 1
}

# ------------------------------------------------------------------------------
# Detect Desktop Browser / Webview Runner
# ------------------------------------------------------------------------------
find_desktop_runner() {
    # Candidate list ordered by preference for standalone app mode
    local candidates=(
        "google-chrome"
        "google-chrome-stable"
        "chromium"
        "chromium-browser"
        "brave-browser"
        "brave"
        "microsoft-edge-stable"
        "microsoft-edge"
        "/snap/bin/chromium"
        "/snap/bin/brave"
    )

    for bin in "${candidates[@]}"; do
        if command -v "$bin" >/dev/null 2>&1; then
            echo "$bin"
            return 0
        fi
    done

    # Check Flatpak installations
    if command -v flatpak >/dev/null 2>&1; then
        if flatpak info com.google.Chrome >/dev/null 2>&1; then
            echo "flatpak:com.google.Chrome"
            return 0
        elif flatpak info org.chromium.Chromium >/dev/null 2>&1; then
            echo "flatpak:org.chromium.Chromium"
            return 0
        elif flatpak info com.brave.Browser >/dev/null 2>&1; then
            echo "flatpak:com.brave.Browser"
            return 0
        elif flatpak info com.microsoft.Edge >/dev/null 2>&1; then
            echo "flatpak:com.microsoft.Edge"
            return 0
        fi
    fi

    # Fallback to Firefox
    if command -v firefox >/dev/null 2>&1; then
        echo "firefox"
        return 0
    fi

    # Final fallback
    echo "xdg-open"
}

# ------------------------------------------------------------------------------
# Launch Desktop App Window
# ------------------------------------------------------------------------------
launch_desktop_window() {
    local target_url="http://localhost:${FRONTEND_PORT}"
    local runner
    runner="$(find_desktop_runner)"
    local user_data_dir="$HOME/.config/sensei/browser-profile"
    mkdir -p "$user_data_dir"

    echo -e "  ${MAGENTA}★${RESET} Desktop Runner detected: ${BOLD}${runner}${RESET}"
    echo -e "  ${GREEN}🚀${RESET} Launching Sensei Standalone App Window..."
    echo -e "${CYAN}------------------------------------------------------------${RESET}"

    case "$runner" in
        flatpak:*)
            local app_id="${runner#flatpak:}"
            flatpak run "$app_id" --app="$target_url" --user-data-dir="$user_data_dir" --class="Sensei" --name="Sensei" --window-size=1280,850 >/dev/null 2>&1 &
            ;;
        google-chrome*|chromium*|brave*|microsoft-edge*)
            "$runner" \
                --app="$target_url" \
                --user-data-dir="$user_data_dir" \
                --class="Sensei" \
                --name="Sensei" \
                --window-size=1280,850 \
                --enable-features=OverlayScrollbar \
                >/dev/null 2>&1 &
            ;;
        firefox)
            firefox --new-window "$target_url" >/dev/null 2>&1 &
            ;;
        *)
            xdg-open "$target_url" >/dev/null 2>&1 &
            ;;
    esac
}

# ------------------------------------------------------------------------------
# CLI Commands
# ------------------------------------------------------------------------------
show_status() {
    print_banner
    echo -e "${BOLD}Current Service Status:${RESET}"
    if is_backend_healthy; then
        echo -e "  Backend  (PORT $PORT):          ${GREEN}● RUNNING${RESET} (http://localhost:${PORT}/health)"
    else
        echo -e "  Backend  (PORT $PORT):          ${RED}○ STOPPED${RESET}"
    fi

    if is_frontend_healthy; then
        echo -e "  Frontend (PORT $FRONTEND_PORT):      ${GREEN}● RUNNING${RESET} (http://localhost:${FRONTEND_PORT})"
    else
        echo -e "  Frontend (PORT $FRONTEND_PORT):      ${RED}○ STOPPED${RESET}"
    fi
    echo ""
}

# ------------------------------------------------------------------------------
# Main
# ------------------------------------------------------------------------------
main() {
    setup_environment

    local action="launch"
    while [[ $# -gt 0 ]]; do
        case "$1" in
            --status|-s)
                action="status"
                shift
                ;;
            --stop)
                action="stop"
                shift
                ;;
            --headless)
                action="headless"
                shift
                ;;
            --browser-only)
                action="browser-only"
                shift
                ;;
            --help|-h)
                echo "Usage: $0 [OPTIONS]"
                echo ""
                echo "Options:"
                echo "  (no args)       Start services (if not already running) & launch desktop window"
                echo "  --headless      Start backend & frontend without opening desktop browser"
                echo "  --browser-only  Launch desktop app window pointing to existing running frontend"
                echo "  --status, -s    Display health and status of Sensei services"
                echo "  --stop          Stop running Sensei background services"
                echo "  --help, -h      Show this help message"
                exit 0
                ;;
            *)
                shift
                ;;
        esac
    done

    case "$action" in
        status)
            show_status
            exit 0
            ;;
        stop)
            if [ -x "$SCRIPT_DIR/sensei-linux-stop.sh" ]; then
                exec "$SCRIPT_DIR/sensei-linux-stop.sh"
            else
                echo "Stop script not found."
                exit 1
            fi
            ;;
        headless)
            print_banner
            echo -e "${BOLD}Starting Sensei in headless mode...${RESET}\n"
            start_backend
            start_frontend
            echo -e "\n${GREEN}✔ Sensei services are running in the background.${RESET}"
            echo -e "  Backend:  ${CYAN}http://localhost:${PORT}${RESET}"
            echo -e "  Frontend: ${CYAN}http://localhost:${FRONTEND_PORT}${RESET}\n"
            exit 0
            ;;
        browser-only)
            launch_desktop_window
            exit 0
            ;;
        launch)
            print_banner
            start_backend
            start_frontend
            launch_desktop_window
            echo -e "\n${GREEN}✔ Sensei is ready!${RESET}"
            echo -e "  ${DIM}Backend logs:  $ROOT_DIR/backend.log${RESET}"
            echo -e "  ${DIM}Frontend logs: $ROOT_DIR/frontend.log${RESET}"
            echo -e "  ${DIM}To stop:       $SCRIPT_DIR/sensei-linux-stop.sh${RESET}\n"
            ;;
    esac
}

main "$@"
