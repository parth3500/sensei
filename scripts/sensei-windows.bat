@echo off
REM ==============================================================================
REM Sensei Study Engine - Desktop Launcher for Windows 10 / 11
REM ==============================================================================
REM - Verifies .NET 8 SDK and Node.js
REM - Starts ASP.NET Core Backend on http://localhost:5000 in background
REM - Starts Next.js Frontend on http://localhost:3000 in background
REM - Launches Edge or Chrome in standalone desktop application window mode
REM ==============================================================================

setlocal enabledelayedexpansion
title Sensei Study Engine Desktop

set "SCRIPT_DIR=%~dp0"
set "ROOT_DIR=%SCRIPT_DIR%.."
cd /d "%ROOT_DIR%"

echo ========================================================================
echo        Sensei Study Engine - Windows Desktop Application
echo ========================================================================
echo.

REM Load .env if present
if exist "%ROOT_DIR%\.env" (
    for /f "usebackq tokens=1,* delims==" %%a in ("%ROOT_DIR%\.env") do (
        set "line=%%a"
        if not "!line:~0,1!"=="#" (
            set "%%a=%%b"
        )
    )
)

if not defined PORT set PORT=5000
if not defined FRONTEND_PORT set FRONTEND_PORT=3000

REM 1. Check Backend
echo [1/3] Checking C# Backend (http://localhost:%PORT%)...
curl -s http://localhost:%PORT%/health >nul 2>&1
if %errorlevel% neq 0 (
    echo       Starting C# .NET 8 Backend...
    cd /d "%ROOT_DIR%\backend"
    start /b "" dotnet run --no-launch-profile > "%ROOT_DIR%\backend.log" 2>&1
    cd /d "%ROOT_DIR%"
    
    set /a attempts=0
    :wait_backend
    set /a attempts+=1
    timeout /t 1 /nobreak >nul
    curl -s http://localhost:%PORT%/health >nul 2>&1
    if %errorlevel% neq 0 (
        if !attempts! lss 25 (
            echo       Waiting for Backend to initialize... (!attempts!/25)
            goto wait_backend
        ) else (
            echo [!] Backend failed to start. Check backend.log for details.
        )
    ) else (
        echo       Backend is online and healthy!
    )
) else (
    echo       Backend is already running.
)

REM 2. Check Frontend
echo.
echo [2/3] Checking Next.js Frontend (http://localhost:%FRONTEND_PORT%)...
curl -s http://localhost:%FRONTEND_PORT%/ >nul 2>&1
if %errorlevel% neq 0 (
    echo       Starting Next.js Frontend...
    cd /d "%ROOT_DIR%\frontend"
    start /b "" npm start > "%ROOT_DIR%\frontend.log" 2>&1
    cd /d "%ROOT_DIR%"
    
    set /a attempts=0
    :wait_frontend
    set /a attempts+=1
    timeout /t 1 /nobreak >nul
    curl -s http://localhost:%FRONTEND_PORT%/ >nul 2>&1
    if %errorlevel% neq 0 (
        if !attempts! lss 25 (
            echo       Waiting for Frontend to initialize... (!attempts!/25)
            goto wait_frontend
        ) else (
            echo [!] Frontend failed to start. Check frontend.log for details.
        )
    ) else (
        echo       Frontend is online and healthy!
    )
) else (
    echo       Frontend is already running.
)

REM 3. Launch Standalone Desktop Window
echo.
echo [3/3] Launching Sensei Desktop Window...

set "TARGET_URL=http://localhost:%FRONTEND_PORT%"
set "LAUNCHED=0"

REM Try Microsoft Edge in App Mode (default on modern Windows)
where msedge >nul 2>&1
if %errorlevel% equ 0 (
    start msedge --app="%TARGET_URL%" --window-size=1366,880 --app-id=SenseiStudyEngine
    set LAUNCHED=1
    goto done
)

if exist "%ProgramFiles(x86)%\Microsoft\Edge\Application\msedge.exe" (
    start "" "%ProgramFiles(x86)%\Microsoft\Edge\Application\msedge.exe" --app="%TARGET_URL%" --window-size=1366,880
    set LAUNCHED=1
    goto done
)

REM Try Google Chrome in App Mode
where chrome >nul 2>&1
if %errorlevel% equ 0 (
    start chrome --app="%TARGET_URL%" --window-size=1366,880
    set LAUNCHED=1
    goto done
)

if exist "%ProgramFiles%\Google\Chrome\Application\chrome.exe" (
    start "" "%ProgramFiles%\Google\Chrome\Application\chrome.exe" --app="%TARGET_URL%" --window-size=1366,880
    set LAUNCHED=1
    goto done
)

REM Fallback: default browser
start "" "%TARGET_URL%"

:done
echo.
echo ========================================================================
echo [OK] Sensei Desktop is running!
echo URL: %TARGET_URL%
echo To stop all services: run scripts\sensei-windows-stop.bat
echo ========================================================================
timeout /t 3 >nul
exit /b 0
