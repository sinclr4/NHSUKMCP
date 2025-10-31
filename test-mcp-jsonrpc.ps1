# Test NHS MCP JSON-RPC Implementation (PowerShell)
# Usage: .\test-mcp-jsonrpc.ps1 [base-url]

param(
  [string]$BaseUrl = "http://localhost:7071"
)

$McpEndpoint = "$BaseUrl/mcp"

Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "Testing NHS MCP JSON-RPC Implementation" -ForegroundColor Cyan
Write-Host "Endpoint: $McpEndpoint" -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""

function Invoke-McpRequest {
    param(
        [string]$Method,
        [hashtable]$Params,
     [int]$Id
    )
    
    $body = @{
     jsonrpc = "2.0"
        id = $Id
        method = $Method
   params = $Params
    } | ConvertTo-Json -Depth 10
    
    try {
   $response = Invoke-RestMethod -Uri $McpEndpoint -Method Post -Body $body -ContentType "application/json"
        $response | ConvertTo-Json -Depth 10
    } catch {
    Write-Host "Error: $_" -ForegroundColor Red
    }
}

# Test 1: Initialize
Write-Host "Test 1: Initialize" -ForegroundColor Yellow
Invoke-McpRequest -Method "initialize" -Params @{
  protocolVersion = "2024-11-05"
    clientInfo = @{
        name = "powershell-client"
        version = "1.0.0"
    }
} -Id 1
Write-Host ""
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""

# Test 2: List Tools
Write-Host "Test 2: List Tools" -ForegroundColor Yellow
Invoke-McpRequest -Method "tools/list" -Params @{} -Id 2
Write-Host ""
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""

# Test 3: Get Organisation Types
Write-Host "Test 3: Get Organisation Types" -ForegroundColor Yellow
Invoke-McpRequest -Method "tools/call" -Params @{
  name = "get_organisation_types"
    arguments = @{}
} -Id 3
Write-Host ""
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""

# Test 4: Convert Postcode
Write-Host "Test 4: Convert Postcode to Coordinates" -ForegroundColor Yellow
Invoke-McpRequest -Method "tools/call" -Params @{
    name = "convert_postcode_to_coordinates"
    arguments = @{
        postcode = "SW1A 1AA"
    }
} -Id 4
Write-Host ""
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""

# Test 5: Search by Postcode
Write-Host "Test 5: Search Organisations by Postcode" -ForegroundColor Yellow
Invoke-McpRequest -Method "tools/call" -Params @{
    name = "search_organisations_by_postcode"
    arguments = @{
 organisationType = "PHA"
        postcode = "SW1A 1AA"
   maxResults = 3
    }
} -Id 5
Write-Host ""
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""

# Test 6: Search by Coordinates
Write-Host "Test 6: Search Organisations by Coordinates" -ForegroundColor Yellow
Invoke-McpRequest -Method "tools/call" -Params @{
    name = "search_organisations_by_coordinates"
    arguments = @{
    organisationType = "GPB"
        latitude = 51.5074
  longitude = -0.1278
    maxResults = 3
    }
} -Id 6
Write-Host ""
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""

# Test 7: Get Health Topic
Write-Host "Test 7: Get Health Topic" -ForegroundColor Yellow
Invoke-McpRequest -Method "tools/call" -Params @{
    name = "get_health_topic"
arguments = @{
      topic = "asthma"
  }
} -Id 7
Write-Host ""
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""

# Test 8: Ping
Write-Host "Test 8: Ping" -ForegroundColor Yellow
Invoke-McpRequest -Method "ping" -Params @{} -Id 8
Write-Host ""
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""

# Test 9: Error Handling - Invalid Tool
Write-Host "Test 9: Error Handling - Invalid Tool" -ForegroundColor Yellow
Invoke-McpRequest -Method "tools/call" -Params @{
 name = "invalid_tool"
    arguments = @{}
} -Id 9
Write-Host ""
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""

# Test 10: Error Handling - Invalid Postcode
Write-Host "Test 10: Error Handling - Invalid Postcode" -ForegroundColor Yellow
Invoke-McpRequest -Method "tools/call" -Params @{
  name = "convert_postcode_to_coordinates"
    arguments = @{
 postcode = "INVALID"
    }
} -Id 10
Write-Host ""
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "All tests completed!" -ForegroundColor Green
Write-Host ""
