# 🪟 Sensei Desktop App - Windows Guide

Sensei provides a native-feeling standalone desktop application integration for Windows 10 and Windows 11.

---

## 🚀 Quick Launch (Batch / One-Click)

From Command Prompt or double-clicking the file in Windows Explorer:

```cmd
scripts\sensei-windows.bat
```

### What this does:
1. Validates the .NET 8 runtime and Node.js environment.
2. Checks if the C# Backend (`http://localhost:5000/health`) and Next.js Frontend (`http://localhost:3000`) are active. If not, boots them silently in background processes.
3. Automatically launches **Microsoft Edge** or **Google Chrome** in dedicated **Standalone Application Window Mode** (`--app=http://localhost:3000 --window-size=1366,880`).
   - No browser URL address bar clutter.
   - Separate taskbar icon with the Sensei branding.
   - Fast, native desktop feel.

---

## ⚡ Modern PowerShell Launcher

If you prefer PowerShell 5 or PowerShell 7:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\sensei-windows.ps1
```

Includes process supervision, colored status monitoring, and auto-fallback between Edge and Chrome.

---

## 📌 Create a Desktop Shortcut

To place an official **"Sensei Study Engine"** icon directly onto your Windows Desktop:

Double-click or run:
```cmd
cscript //nologo scripts\create-windows-shortcut.vbs
```

This generates `Sensei Study Engine.lnk` on your Desktop with the official high-resolution `sensei.ico` icon!

---

## 🛑 Stopping Services

To cleanly terminate all Sensei backend and frontend background processes:

```cmd
scripts\sensei-windows-stop.bat
```
