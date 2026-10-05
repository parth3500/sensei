#!/usr/bin/env bash
# ==============================================================================
# Sensei Study Engine - Linux Desktop Integration Installer
# Installs desktop launcher, icon, and configures FreeDesktop.org environment
# ==============================================================================

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"

# Styling
if [ -t 1 ]; then
    BOLD="\033[1m"
    GREEN="\033[32m"
    CYAN="\033[36m"
    YELLOW="\033[33m"
    MAGENTA="\033[35m"
    RESET="\033[0m"
else
    BOLD=""
    GREEN=""
    CYAN=""
    YELLOW=""
    MAGENTA=""
    RESET=""
fi

echo -e "${CYAN}${BOLD}"
cat << "EOF"
  ███████╗███████╗███╗   ██╗███████╗███████╗██╗
  ██╔════╝██╔════╝████╗  ██║██╔════╝██╔════╝██║
  ███████╗█████╗  ██╔██╗ ██║███████╗█████╗  ██║
  ╚════██║██╔══╝  ██║╚██╗██║╚════██║██╔══╝  ██║
  ███████║███████╗██║ ╚████║███████║███████╗██║
  ╚══════╝╚══════╝╚═╝  ╚═══╝╚══════╝╚══════╝╚═╝
EOF
echo -e "${MAGENTA}${BOLD}   ★ LINUX DESKTOP INTEGRATION INSTALLER ★${RESET}"
echo -e "${CYAN}------------------------------------------------------------${RESET}"

# 1. Make all scripts executable
echo -e "  ${YELLOW}▶${RESET} Setting executable permissions on scripts..."
chmod +x "$ROOT_DIR/scripts/"*.sh "$ROOT_DIR/start.sh" "$ROOT_DIR/setup_dotnet.sh" 2>/dev/null || true
echo -e "  ${GREEN}✔${RESET} Scripts are executable."

# 2. Prepare directory destinations
APPS_DIR="$HOME/.local/share/applications"
ICON_DIR_512="$HOME/.local/share/icons/hicolor/512x512/apps"
ICON_DIR_192="$HOME/.local/share/icons/hicolor/192x192/apps"

mkdir -p "$APPS_DIR"
mkdir -p "$ICON_DIR_512"
mkdir -p "$ICON_DIR_192"

# 3. Copy application icons
SOURCE_ICON_512="$ROOT_DIR/frontend/public/icons/icon-512x512.png"
if [ ! -f "$SOURCE_ICON_512" ]; then
    SOURCE_ICON_512="$ROOT_DIR/frontend/public/icons/icon-512.png"
fi

SOURCE_ICON_192="$ROOT_DIR/frontend/public/icons/icon-192x192.png"
if [ ! -f "$SOURCE_ICON_192" ]; then
    SOURCE_ICON_192="$ROOT_DIR/frontend/public/icons/icon-192.png"
fi

if [ -f "$SOURCE_ICON_512" ]; then
    cp "$SOURCE_ICON_512" "$ICON_DIR_512/sensei.png"
    echo -e "  ${GREEN}✔${RESET} Installed 512x512 icon to: ${CYAN}$ICON_DIR_512/sensei.png${RESET}"
fi

if [ -f "$SOURCE_ICON_192" ]; then
    cp "$SOURCE_ICON_192" "$ICON_DIR_192/sensei.png"
    echo -e "  ${GREEN}✔${RESET} Installed 192x192 icon to: ${CYAN}$ICON_DIR_192/sensei.png${RESET}"
fi

# 4. Process and install sensei.desktop
DESKTOP_SRC="$ROOT_DIR/sensei.desktop"
DESKTOP_DEST="$APPS_DIR/sensei.desktop"

# Replace standard path with actual current repo root if moved
sed "s|/home/parth/prog/sensei|$ROOT_DIR|g" "$DESKTOP_SRC" > "$DESKTOP_DEST"
chmod +x "$DESKTOP_DEST"
echo -e "  ${GREEN}✔${RESET} Installed desktop entry to: ${CYAN}$DESKTOP_DEST${RESET}"

# 5. Validate desktop file
if command -v desktop-file-validate >/dev/null 2>&1; then
    desktop-file-validate "$DESKTOP_DEST" || true
fi

# 6. Update desktop database & icon cache
if command -v update-desktop-database >/dev/null 2>&1; then
    update-desktop-database "$APPS_DIR" >/dev/null 2>&1 || true
    echo -e "  ${GREEN}✔${RESET} Updated desktop database cache."
fi

if command -v gtk-update-icon-cache >/dev/null 2>&1; then
    gtk-update-icon-cache -f -t "$HOME/.local/share/icons/hicolor" >/dev/null 2>&1 || true
    echo -e "  ${GREEN}✔${RESET} Updated GTK icon cache."
fi

echo -e "\n${GREEN}${BOLD}🎉 Installation Complete!${RESET}"
echo -e "You can now:"
echo -e "  1. Search for ${CYAN}${BOLD}'Sensei'${RESET} in your Application Menu / GNOME Activities / KDE Kickoff."
echo -e "  2. Pin Sensei to your favorite dock, dash, or panel."
echo -e "  3. Launch from terminal at any time via: ${CYAN}$ROOT_DIR/scripts/sensei-linux.sh${RESET}"
echo -e "  4. Stop background services anytime via: ${CYAN}$ROOT_DIR/scripts/sensei-linux-stop.sh${RESET}\n"
