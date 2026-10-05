<#
.SYNOPSIS
    Sensei Study Engine - Modern Windows PowerShell Desktop Launcher
.DESCRIPTION
    Launches ASP.NET Core backend and Next.js frontend services, supervises their health,
    and opens Sensei in standalone desktop app mode with Microsoft Edge or Chrome.
#>

[CmdletBinding()]
param (
    [int]$BackendPort = 5000,
    [int]$FrontendPort = 3000,
    [switch]$NoBrowser = $false
)

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RootDir = Split-Path -Parent $ScriptDir
Set-Location -Path $RootDir

Write-Host "========================================================================" -ForegroundColor Cyan
Write-Host "       🥋 Sensei Study Engine - Desktop Orchestrator (Windows)         " -ForegroundColor White
Write-Host "========================================================================" -ForegroundColor Cyan
Write-Host ""

# Load .env if present
$EnvFile = Join-Path $RootDir ".env"
if (Test-Path $EnvFile) {
    Get-Content $EnvFile | ForEach-Object {
        $line = $_.Trim()
        if ($line -and -not $line.StartsWith("#") -and $line.Contains("=")) {
            $parts = $line.Split("=", 2)
            [System.Environment]::SetEnvironmentVariable($parts[0].Trim(), $parts[1].Trim(), "Process")
        }
    }
}

if ($env:PORT) { $BackendPort = [int]$env:PORT }
if ($env:FRONTEND_PORT) { $FrontendPort = [int]$env:FRONTEND_PORT }

function Test-Endpoint {
    param ([string]$Url)
    try {
        $resp = Invoke-WebRequest -Uri $Url -Method Get -TimeoutSec 2 -UseBasicParsing -ErrorAction SilentlyContinue
        return ($resp.StatusCode -ge 200 -and $resp.StatusCode -lt 400)
    } catch {
        return $false
    }
}

# 1. Check / Start Backend
Write-Host "[1/3] Checking C# .NET 8 Backend (http://localhost:$BackendPort)..." -ForegroundColor Yellow
if (-not (Test-Endpoint "http://localhost:$BackendPort/health")) {
    Write-Host "      Starting Backend process in background..." -ForegroundColor Gray
    $backendProc = Start-Process -FilePath "dotnet" -ArgumentList "run --no-launch-profile" `
        -WorkingDirectory (Join-Path $RootDir "backend") `
        -RedirectStandardOutput (Join-Path $RootDir "backend.log") `
        -RedirectStandardError (Join-Path $RootDir "backend-err.log") `
        -PassThru -WindowStyle Hidden

    $healthy = $false
    for ($i = 1; $i -le 25; $i++) {
        Start-Sleep -Seconds 1
        if (Test-Endpoint "http://localhost:$BackendPort/health") {
            $healthy = $true
            break
        }
        Write-Host "      Waiting for Backend... ($i/25)" -ForegroundColor DarkGray
    }
    if ($healthy) {
        Write-Host "      Backend is healthy and listening on port $BackendPort!" -ForegroundColor Green
    } else {
        Write-Warning "Backend health check timed out. Check backend.log for details."
    }
} else {
    Write-Host "      Backend is already running." -ForegroundColor Green
}

# 2. Check / Start Frontend
Write-Host ""
Write-Host "[2/3] Checking Next.js Frontend (http://localhost:$FrontendPort)..." -ForegroundColor Yellow
if (-not (Test-Endpoint "http://localhost:$FrontendPort/")) {
    Write-Host "      Starting Next.js Frontend process in background..." -ForegroundColor Gray
    $frontendProc = Start-Process -FilePath "npm.cmd" -ArgumentList "start" `
        -WorkingDirectory (Join-Path $RootDir "frontend") `
        -RedirectStandardOutput (Join-Path $RootDir "frontend.log") `
        -RedirectStandardError (Join-Path $RootDir "frontend-err.log") `
        -PassThru -WindowStyle Hidden

    $healthy = $false
    for ($i = 1; $i -le 25; $i++) {
        Start-Sleep -Seconds 1
        if (Test-Endpoint "http://localhost:$FrontendPort/") {
            $healthy = $true
            break
        }
        Write-Host "      Waiting for Frontend... ($i/25)" -ForegroundColor DarkGray
    }
    if ($healthy) {
        Write-Host "      Frontend is healthy and listening on port $FrontendPort!" -ForegroundColor Green
    } else {
        Write-Warning "Frontend health check timed out. Check frontend.log for details."
    }
} else {
    Write-Host "      Frontend is already running." -ForegroundColor Green
}

# 3. Launch Desktop Window
Write-Host ""
Write-Host "[3/3] Launching Sensei Desktop Window..." -ForegroundColor Yellow
$TargetUrl = "http://localhost:$FrontendPort"

if (-not $NoBrowser) {
    # Check for Edge
    $edgePath = (Get-Command msedge.exe -ErrorAction SilentlyContinue)?.Source
    if (-not $edgePath) {
        $testPath = "${env:ProgramFiles(x86)}\Microsoft\Edge\Application\msedge.exe"
        if (Test-Path $testPath) { $edgePath = $testPath }
    }

    # Check for Chrome
    $chromePath = (Get-Command chrome.exe -ErrorAction SilentlyContinue)?.Source
    if (-not $chromePath) {
        $testPath = "$env:ProgramFiles\Google\Chrome\Application\chrome.exe"
        if (Test-Path $testPath) { $chromePath = $testPath }
    }

    if ($edgePath) {
        Write-Host "      Launching via Microsoft Edge Standalone App Mode..." -ForegroundColor Gray
        Start-Process $edgePath -ArgumentList "--app=`"$TargetUrl`"", "--window-size=1366,880", "--app-id=SenseiStudyEngine"
    } elseif ($chromePath) {
        Write-Host "      Launching via Google Chrome Standalone App Mode..." -ForegroundColor Gray
        Start-Process $chromePath -ArgumentList "--app=`"$TargetUrl`"", "--window-size=1366,880"
    } else {
        Write-Host "      Opening in default web browser..." -ForegroundColor Gray
        Start-Process $TargetUrl
    }
}

Write-Host ""
Write-Host "========================================================================" -ForegroundColor Cyan
Write-Host " [OK] Sensei Desktop Application is active!" -ForegroundColor Green
Write-Host " Web Portal: $TargetUrl" -ForegroundColor White
Write-Host " To stop all background services, run: scripts\sensei-windows-stop.bat" -ForegroundColor Gray
Write-Host "========================================================================" -ForegroundColor Cyan
