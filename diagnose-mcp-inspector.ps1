#!/usr/bin/env pwsh
# MCP Inspector Diagnostic Script
# This script checks your setup and identifies common issues

Write-Host "=== MCP Inspector Diagnostic Script ===" -ForegroundColor Cyan
Write-Host ""

$hasErrors = $false

# 1. Check if Functions host is running
Write-Host "[1/7] Checking if Azure Functions is running..." -ForegroundColor Yellow
try {
    $functionsProcess = Get-Process -Name "func" -ErrorAction SilentlyContinue
  if ($functionsProcess) {
        Write-Host "  ? Functions host is running (PID: $($functionsProcess.Id))" -ForegroundColor Green
        Write-Host "  If you want to restart it, run: .\clear-port-7071.ps1" -ForegroundColor Gray
    } else {
        Write-Host "  ? Functions host is NOT running" -ForegroundColor Yellow
        Write-Host "    Run: func start" -ForegroundColor Gray
        
        # Check if port is still in use by something else
        $portCheck = netstat -ano | Select-String ":7071"
     if ($portCheck) {
     Write-Host "    ? WARNING: Port 7071 is in use by another process!" -ForegroundColor Red
     Write-Host "    Run: .\clear-port-7071.ps1" -ForegroundColor Gray
 $hasErrors = $true
        }
    }
} catch {
    Write-Host "  ? Error checking Functions process: $_" -ForegroundColor Red
    $hasErrors = $true
}

Write-Host ""

# 2. Check if port 7071 is listening
Write-Host "[2/7] Checking if port 7071 is listening..." -ForegroundColor Yellow
$portCheck = netstat -ano | Select-String ":7071"
if ($portCheck) {
    Write-Host "  ? Port 7071 is listening" -ForegroundColor Green
} else {
    Write-Host "  ? Port 7071 is NOT listening" -ForegroundColor Red
    Write-Host "    Start Functions with: func start" -ForegroundColor Gray
    $hasErrors = $true
}

Write-Host ""

# 3. Check Node.js version
Write-Host "[3/7] Checking Node.js version..." -ForegroundColor Yellow
try {
    $nodeVersion = node --version 2>$null
    if ($nodeVersion) {
        $versionNumber = [int]($nodeVersion -replace 'v|\..*', '')
     if ($versionNumber -ge 18) {
    Write-Host "  ? Node.js $nodeVersion (18+ required)" -ForegroundColor Green
        } else {
     Write-Host "  ? Node.js $nodeVersion is too old (18+ required)" -ForegroundColor Red
   $hasErrors = $true
        }
    } else {
      Write-Host "  ? Node.js is not installed" -ForegroundColor Red
        Write-Host "    Download from: https://nodejs.org/" -ForegroundColor Gray
 $hasErrors = $true
    }
} catch {
    Write-Host "  ? Node.js is not installed" -ForegroundColor Red
    Write-Host "    Download from: https://nodejs.org/" -ForegroundColor Gray
    $hasErrors = $true
}

Write-Host ""

# 4. Test the endpoint with curl
Write-Host "[4/7] Testing MCP endpoint..." -ForegroundColor Yellow

$testUrl = "http://localhost:7071/api/mcp"
$workingUrl = $null

Write-Host "  Testing: $testUrl" -ForegroundColor Gray
try {
    $body = '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05"}}'
    $response = curl.exe -X POST $testUrl `
  -H "Content-Type: application/json" `
        -d $body `
        --silent `
        --max-time 5 2>&1
    
    if ($response -match '"jsonrpc"' -and $response -match '"result"') {
        Write-Host "  ? MCP endpoint is responding correctly" -ForegroundColor Green
        $workingUrl = $testUrl
    } else {
        Write-Host "  ? MCP endpoint failed - Response: $response" -ForegroundColor Red
        $hasErrors = $true
    }
} catch {
    Write-Host "  ? MCP endpoint error: $_" -ForegroundColor Red
    $hasErrors = $true
}

if (-not $workingUrl) {
    Write-Host "  ? MCP endpoint is not responding correctly" -ForegroundColor Red
    Write-Host "    Make sure Functions is running: func start" -ForegroundColor Gray
    $hasErrors = $true
}

Write-Host ""

# 5. Check local.settings.json for API key
Write-Host "[5/7] Checking local.settings.json..." -ForegroundColor Yellow
$settingsFile = "local.settings.json"
if (Test-Path $settingsFile) {
    Write-Host "  ? local.settings.json exists" -ForegroundColor Green
    try {
    $settings = Get-Content $settingsFile | ConvertFrom-Json
        $apiKey = $settings.Values.API_MANAGEMENT_SUBSCRIPTION_KEY
        if ($apiKey -and $apiKey -ne "your-subscription-key-here") {
  Write-Host "  ? API_MANAGEMENT_SUBSCRIPTION_KEY is set" -ForegroundColor Green
  } else {
            Write-Host "  ? API_MANAGEMENT_SUBSCRIPTION_KEY is not set or is placeholder" -ForegroundColor Yellow
            Write-Host "    Tool calls may fail without a valid API key" -ForegroundColor Gray
        }
    } catch {
        Write-Host "  ? Error reading local.settings.json: $_" -ForegroundColor Red
    }
} else {
    Write-Host "  ? local.settings.json not found" -ForegroundColor Red
    Write-Host "    Create it from local.settings.json.example" -ForegroundColor Gray
    $hasErrors = $true
}

Write-Host ""

# 6. Test tools/list
Write-Host "[6/7] Testing tools/list endpoint..." -ForegroundColor Yellow
if ($workingUrl) {
    try {
        $body = '{"jsonrpc":"2.0","id":2,"method":"tools/list","params":{}}'
   $response = curl.exe -X POST $workingUrl `
   -H "Content-Type: application/json" `
            -d $body `
  --silent `
     --max-time 5 2>&1
        
        if ($response -match '"tools"') {
 $toolCount = ([regex]::Matches($response, '"name"')).Count
   Write-Host "  ? tools/list works - Found $toolCount tools" -ForegroundColor Green
 } else {
  Write-Host "  ? tools/list failed - Response: $response" -ForegroundColor Red
      $hasErrors = $true
        }
    } catch {
        Write-Host "  ? Error testing tools/list: $_" -ForegroundColor Red
        $hasErrors = $true
    }
} else {
 Write-Host "  ? Skipped (endpoint not working)" -ForegroundColor Gray
}

Write-Host ""

# 7. Check if MCP Inspector is installed
Write-Host "[7/7] Checking MCP Inspector..." -ForegroundColor Yellow
try {
    $inspectorCheck = npm list -g @modelcontextprotocol/inspector 2>&1
    if ($inspectorCheck -match "@modelcontextprotocol/inspector@") {
        $version = $inspectorCheck -replace '.*@modelcontextprotocol/inspector@([^\s]+).*', '$1'
        Write-Host "  ? MCP Inspector is installed (version: $version)" -ForegroundColor Green
    } else {
  Write-Host "  ? MCP Inspector might not be installed globally" -ForegroundColor Yellow
    Write-Host "    Install with: npm install -g @modelcontextprotocol/inspector" -ForegroundColor Gray
    }
} catch {
    Write-Host "  ? Could not check Inspector installation" -ForegroundColor Yellow
    Write-Host "    You can still use: npx @modelcontextprotocol/inspector" -ForegroundColor Gray
}

Write-Host ""
Write-Host "=== Diagnostic Summary ===" -ForegroundColor Cyan
Write-Host ""

if ($hasErrors) {
    Write-Host "? ISSUES FOUND - See errors above and fix them first" -ForegroundColor Red
    Write-Host ""
    Write-Host "Common fixes:" -ForegroundColor Yellow
    Write-Host "  1. Start Functions: func start" -ForegroundColor Gray
    Write-Host "  2. Rebuild project: dotnet build" -ForegroundColor Gray
    Write-Host "  3. Set API key in local.settings.json" -ForegroundColor Gray
    Write-Host ""
} else {
    Write-Host "? Everything looks good! Ready to use MCP Inspector" -ForegroundColor Green
    Write-Host ""
    
    if ($workingUrl) {
      Write-Host "Launch Inspector with:" -ForegroundColor Yellow
     Write-Host "  npx @modelcontextprotocol/inspector $workingUrl" -ForegroundColor Cyan
        Write-Host ""
        Write-Host "Or run it now? (Y/N)" -ForegroundColor Yellow
        $response = Read-Host
        if ($response -eq 'Y' -or $response -eq 'y') {
            Write-Host ""
 Write-Host "Launching MCP Inspector..." -ForegroundColor Green
         npx @modelcontextprotocol/inspector $workingUrl
   }
    }
}

Write-Host ""
Write-Host "For more help, see: MCP_INSPECTOR_TROUBLESHOOTING.md" -ForegroundColor Gray
