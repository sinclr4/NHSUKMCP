#!/usr/bin/env pwsh
# Script to clear any port by killing the process using it
# Usage: .\clear-port.ps1 [port_number]

param(
    [Parameter(Mandatory=$false)]
    [int]$Port = 7071
)

Write-Host "=== Port $Port Cleanup Script ===" -ForegroundColor Cyan
Write-Host ""

# Find processes using the specified port
Write-Host "[1/3] Checking what's using port $Port..." -ForegroundColor Yellow

$connections = netstat -ano | Select-String ":$Port"

if (-not $connections) {
    Write-Host "  ? Port $Port is not in use - you're good to go!" -ForegroundColor Green
    Write-Host ""
    Write-Host "You can now start Functions:" -ForegroundColor Cyan
    Write-Host "  func start --port $Port" -ForegroundColor White
    exit 0
}

Write-Host "  Found processes using port ${Port}:" -ForegroundColor Yellow
$connections | ForEach-Object { Write-Host "    $_" -ForegroundColor Gray }
Write-Host ""

# Extract PIDs
Write-Host "[2/3] Identifying processes..." -ForegroundColor Yellow

$pids = $connections | ForEach-Object {
    if ($_ -match '\s+(\d+)\s*$') {
        $matches[1]
    }
} | Select-Object -Unique

$processesToKill = @()

foreach ($pid in $pids) {
 try {
        $process = Get-Process -Id $pid -ErrorAction SilentlyContinue
        if ($process) {
            $processesToKill += $process
       Write-Host "  Found: $($process.ProcessName) (PID: $pid)" -ForegroundColor Yellow
  }
    } catch {
     Write-Host "  Warning: Could not get info for PID $pid" -ForegroundColor Gray
  }
}

if ($processesToKill.Count -eq 0) {
    Write-Host "  ? Could not identify processes to kill" -ForegroundColor Red
    Write-Host ""
    Write-Host "Manual cleanup required:" -ForegroundColor Yellow
    Write-Host "  1. Open Task Manager (Ctrl+Shift+Esc)" -ForegroundColor Gray
    Write-Host "  2. Find 'func.exe' or 'dotnet.exe'" -ForegroundColor Gray
    Write-Host "  3. Right-click and select 'End Task'" -ForegroundColor Gray
    exit 1
}

Write-Host ""

# Kill the processes
Write-Host "[3/3] Killing processes..." -ForegroundColor Yellow

foreach ($process in $processesToKill) {
    Write-Host "  Killing: $($process.ProcessName) (PID: $($process.Id))" -ForegroundColor Yellow
    try {
      Stop-Process -Id $process.Id -Force -ErrorAction Stop
        Write-Host "  ? Killed $($process.ProcessName)" -ForegroundColor Green
    } catch {
        Write-Host "  ? Failed to kill $($process.ProcessName): $_" -ForegroundColor Red
        Write-Host "    You may need to run this script as Administrator" -ForegroundColor Gray
    }
}

Write-Host ""

# Verify port is free
Start-Sleep -Seconds 2
$stillInUse = netstat -ano | Select-String ":$Port"

if ($stillInUse) {
    Write-Host "? Port $Port is still in use" -ForegroundColor Red
    Write-Host ""
    Write-Host "Try these steps:" -ForegroundColor Yellow
    Write-Host "  1. Run this script as Administrator:" -ForegroundColor Gray
    Write-Host "     Right-click PowerShell ? Run as Administrator" -ForegroundColor Gray
    Write-Host "     Then run: .\clear-port.ps1 $Port" -ForegroundColor Gray
    Write-Host ""
    Write-Host "  2. Or manually kill in Task Manager:" -ForegroundColor Gray
    Write-Host "     Ctrl+Shift+Esc ? Find 'func.exe' ? End Task" -ForegroundColor Gray
    exit 1
} else {
    Write-Host "? SUCCESS - Port $Port is now free!" -ForegroundColor Green
    Write-Host ""
    Write-Host "You can now start Functions:" -ForegroundColor Cyan
  Write-Host "  func start --port $Port" -ForegroundColor White
    Write-Host ""
  if ($Port -ne 7071) {
        Write-Host "Or use the default port:" -ForegroundColor Cyan
        Write-Host "  func start" -ForegroundColor White
 Write-Host ""
    }
    Write-Host "To use with Inspector:" -ForegroundColor Cyan
    Write-Host "  npx @modelcontextprotocol/inspector http://localhost:${Port}/api/mcp" -ForegroundColor White
}

Write-Host ""
