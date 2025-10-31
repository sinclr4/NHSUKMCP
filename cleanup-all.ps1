#!/usr/bin/env pwsh
# Complete cleanup script - kills all func.exe and orphaned node.exe processes

Write-Host "=== Complete Cleanup Script ===" -ForegroundColor Cyan
Write-Host "This will stop all Azure Functions and MCP Inspector processes" -ForegroundColor Gray
Write-Host ""

$killedAny = $false

# 1. Kill all func.exe processes
Write-Host "[1/3] Checking for Azure Functions processes..." -ForegroundColor Yellow
$funcProcesses = Get-Process -Name "func" -ErrorAction SilentlyContinue

if ($funcProcesses) {
 Write-Host "  Found $($funcProcesses.Count) func.exe process(es)" -ForegroundColor Yellow
    foreach ($proc in $funcProcesses) {
        Write-Host "  Killing func.exe (PID: $($proc.Id))" -ForegroundColor Yellow
        try {
  Stop-Process -Id $proc.Id -Force
            Write-Host "  ? Killed func.exe" -ForegroundColor Green
            $killedAny = $true
        } catch {
     Write-Host "  ? Failed to kill func.exe: $_" -ForegroundColor Red
}
    }
} else {
    Write-Host "  ? No func.exe processes running" -ForegroundColor Green
}

Write-Host ""

# 2. Check for specific ports
Write-Host "[2/3] Checking common ports..." -ForegroundColor Yellow

$portsToCheck = @(7071, 6277, 5173)
$portsInUse = @()

foreach ($port in $portsToCheck) {
    $connection = netstat -ano | Select-String ":$port"
    if ($connection) {
        $portsInUse += $port
        Write-Host "  ? Port $port is in use" -ForegroundColor Yellow
        
        # Extract and kill the PID
        $connection | ForEach-Object {
       if ($_ -match '\s+(\d+)\s*$') {
       $pid = $matches[1]
  try {
  $process = Get-Process -Id $pid -ErrorAction SilentlyContinue
         if ($process) {
             Write-Host "    Killing $($process.ProcessName) (PID: $pid) on port $port" -ForegroundColor Yellow
         Stop-Process -Id $pid -Force
         Write-Host "    ? Killed process" -ForegroundColor Green
              $killedAny = $true
        }
       } catch {
 Write-Host "    ? Could not kill PID $pid" -ForegroundColor Gray
     }
       }
        }
    }
}

if ($portsInUse.Count -eq 0) {
    Write-Host "  ? All common ports are free (7071, 6277, 5173)" -ForegroundColor Green
}

Write-Host ""

# 3. Optional: Kill orphaned node processes
Write-Host "[3/3] Checking for orphaned Node.js processes..." -ForegroundColor Yellow
$nodeProcesses = Get-Process -Name "node" -ErrorAction SilentlyContinue

if ($nodeProcesses) {
    Write-Host "  Found $($nodeProcesses.Count) node.exe process(es)" -ForegroundColor Yellow
    Write-Host "  These might be MCP Inspector or other Node apps" -ForegroundColor Gray
  Write-Host ""
    Write-Host "  Kill all Node.js processes? (Y/N)" -ForegroundColor Yellow
    $response = Read-Host
    
    if ($response -eq 'Y' -or $response -eq 'y') {
 foreach ($proc in $nodeProcesses) {
    Write-Host "  Killing node.exe (PID: $($proc.Id))" -ForegroundColor Yellow
       try {
                Stop-Process -Id $proc.Id -Force
      Write-Host "  ? Killed node.exe" -ForegroundColor Green
        $killedAny = $true
 } catch {
   Write-Host "? Failed to kill node.exe: $_" -ForegroundColor Red
   }
        }
    } else {
        Write-Host "  Skipped killing Node.js processes" -ForegroundColor Gray
    }
} else {
    Write-Host "  ? No node.exe processes running" -ForegroundColor Green
}

Write-Host ""
Write-Host "=== Cleanup Complete ===" -ForegroundColor Cyan
Write-Host ""

if ($killedAny) {
    # Verify ports are now free
    Start-Sleep -Seconds 2
    
  Write-Host "Verifying ports are free..." -ForegroundColor Yellow
    $anyStillInUse = $false
    
    foreach ($port in $portsToCheck) {
    $stillInUse = netstat -ano | Select-String ":$port"
        if ($stillInUse) {
       Write-Host "  ? Port $port is still in use" -ForegroundColor Red
         $anyStillInUse = $true
  } else {
       Write-Host "  ? Port $port is free" -ForegroundColor Green
        }
    }
    
    Write-Host ""
    
    if ($anyStillInUse) {
        Write-Host "? Some ports are still in use" -ForegroundColor Red
        Write-Host "You may need to run this script as Administrator" -ForegroundColor Yellow
    } else {
        Write-Host "? All ports are free!" -ForegroundColor Green
        Write-Host ""
        Write-Host "You can now start Functions:" -ForegroundColor Cyan
        Write-Host "  func start" -ForegroundColor White
        Write-Host ""
        Write-Host "Or launch the Inspector:" -ForegroundColor Cyan
        Write-Host "  .\launch-inspector.bat" -ForegroundColor White
    }
} else {
    Write-Host "? Everything was already clean!" -ForegroundColor Green
    Write-Host ""
    Write-Host "You can start Functions:" -ForegroundColor Cyan
    Write-Host "  func start" -ForegroundColor White
}

Write-Host ""
