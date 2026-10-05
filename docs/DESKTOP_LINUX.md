# 🐧 Sensei Desktop App - Linux Guide

Sensei provides a first-class native desktop integration for Linux environments (Ubuntu, Debian, Fedora, Arch, Pop!_OS, Linux Mint, KDE Plasma, GNOME, XFCE).

---

## 🚀 Quick Launch

You can run Sensei directly from the terminal or launcher script:

```bash
cd /home/parth/prog/sensei
./scripts/sensei-linux.sh
```

This script:
1. Validates and activates the .NET 8 runtime and Node.js environment.
2. Checks if backend (`http://localhost:5000/health`) and frontend (`http://localhost:3000`) are active. If not, boots them in the background with health checks.
3. Automatically launches a dedicated standalone desktop window (using Chrome, Chromium, Brave, or Edge in `--app` mode without browser address bar clutter).

---

## 🖥️ System-Wide Desktop Installation

To integrate Sensei into your Linux application menu, GNOME Dash, KDE Kickoff, or application search:

```bash
cd /home/parth/prog/sensei
./scripts/install-desktop-linux.sh
```

### What this does:
- Copies `sensei.desktop` to `~/.local/share/applications/sensei.desktop`
- Copies high-res icons to `~/.local/share/icons/hicolor/512x512/apps/sensei.png`
- Registers desktop actions:
  - **Launch Sensei**
  - **Stop Background Services**
  - **Check Service Health**

Now you can press <kbd>Super</kbd> (Windows key) and type **Sensei** to open the app!

---

## 🛑 Stopping Background Services

To gracefully stop the .NET backend and Next.js frontend:

```bash
./scripts/sensei-linux-stop.sh
```

Or right-click the Sensei app icon in your dock and select **"Stop Sensei Background Services"**.
