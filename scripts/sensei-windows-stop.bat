@echo off
REM ==============================================================================
REM Sensei Study Engine - Windows Service Stopper
REM ==============================================================================
setlocal
echo Stopping Sensei Desktop Services...

REM Kill dotnet processes running backend
taskkill /F /FI "WINDOWTITLE eq *Sensei*" >nul 2>&1
for /f "tokens=5" %%a in ('netstat -aon ^| findstr ":5000" ^| findstr "LISTENING"') do (
    echo Stopping backend on PID %%a...
    taskkill /F /PID %%a >nul 2>&1
)

for /f "tokens=5" %%a in ('netstat -aon ^| findstr ":3000" ^| findstr "LISTENING"') do (
    echo Stopping frontend on PID %%a...
    taskkill /F /PID %%a >nul 2>&1
)

echo Sensei desktop services stopped cleanly.
exit /b 0
