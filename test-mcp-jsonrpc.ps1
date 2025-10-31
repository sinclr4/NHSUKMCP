# Test NHS MCP JSON-RPC Implementation (PowerShell)
# Usage: .\test-mcp-jsonrpc.ps1 [base-url]

param(
 [string]$BaseUrl = "http://localhost:7071"
)

$PrimaryEndpoint = "$BaseUrl/api/mcp"
$FallbackEndpoint = "$BaseUrl/mcp"
$McpEndpoint = $null

Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "Testing NHS MCP JSON-RPC Implementation" -ForegroundColor Cyan
Write-Host "Base URL: $BaseUrl" -ForegroundColor Cyan
Write-Host "Attempting to detect correct MCP endpoint..." -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""

function Test-Endpoint($url) {
 try {
 $body = '{"jsonrpc":"2.0","id":1,"method":"ping","params":{}}'
 $resp = Invoke-RestMethod -Uri $url -Method Post -Body $body -ContentType 'application/json' -TimeoutSec 5 -ErrorAction Stop
 if ($resp.jsonrpc -eq '2.0') { return $true }
 } catch { return $false }
 return $false
}

if (Test-Endpoint $PrimaryEndpoint) {
 $McpEndpoint = $PrimaryEndpoint
 Write-Host "? Using endpoint: $McpEndpoint" -ForegroundColor Green
} elseif (Test-Endpoint $FallbackEndpoint) {
 $McpEndpoint = $FallbackEndpoint
 Write-Host "? Using fallback endpoint (no /api prefix): $McpEndpoint" -ForegroundColor Yellow
 Write-Host " Consider using Azure Functions default: $PrimaryEndpoint" -ForegroundColor DarkYellow
} else {
 Write-Host "? Unable to reach MCP endpoint at either:" -ForegroundColor Red
 Write-Host " $PrimaryEndpoint" -ForegroundColor Red
 Write-Host " $FallbackEndpoint" -ForegroundColor Red
 Write-Host "Check that Azure Functions is running (func start)." -ForegroundColor Red
 return
}

function Invoke-McpRequest {
 param(
 [string]$Method,
 [hashtable]$Params,
 [int]$Id
 )
 $bodyObject = [ordered]@{
 jsonrpc = '2.0'
 id = $Id
 method = $Method
 params = $Params
 }
 $body = $bodyObject | ConvertTo-Json -Depth 10
 try {
 $response = Invoke-RestMethod -Uri $McpEndpoint -Method Post -Body $body -ContentType 'application/json' -TimeoutSec 15 -ErrorAction Stop
 ($response | ConvertTo-Json -Depth 10)
 } catch {
 Write-Host "Error calling $Method: $($_.Exception.Message)" -ForegroundColor Red
 if ($_.ErrorDetails) { Write-Host ($_.ErrorDetails | Out-String) -ForegroundColor DarkRed }
 }
}

function Divider() { Write-Host ""; Write-Host "==================================================" -ForegroundColor Cyan; Write-Host "" }

# Test1: Initialize
Write-Host "Test1: initialize" -ForegroundColor Yellow
Invoke-McpRequest -Method 'initialize' -Params @{ protocolVersion = '2024-11-05'; clientInfo = @{ name='powershell-client'; version='1.0.0' } } -Id 1
Divider

# Test2: tools/list
Write-Host "Test2: tools/list" -ForegroundColor Yellow
Invoke-McpRequest -Method 'tools/list' -Params @{} -Id 2
Divider

# Test3: get_organisation_types
Write-Host "Test3: tools/call get_organisation_types" -ForegroundColor Yellow
Invoke-McpRequest -Method 'tools/call' -Params @{ name='get_organisation_types'; arguments=@{} } -Id 3
Divider

# Test4: convert_postcode_to_coordinates
Write-Host "Test4: tools/call convert_postcode_to_coordinates" -ForegroundColor Yellow
Invoke-McpRequest -Method 'tools/call' -Params @{ name='convert_postcode_to_coordinates'; arguments=@{ postcode='SW1A1AA' } } -Id 4
Divider

# Test5: search_organisations_by_postcode
Write-Host "Test5: tools/call search_organisations_by_postcode" -ForegroundColor Yellow
Invoke-McpRequest -Method 'tools/call' -Params @{ name='search_organisations_by_postcode'; arguments=@{ organisationType='PHA'; postcode='SW1A1AA'; maxResults=3 } } -Id 5
Divider

# Test6: search_organisations_by_coordinates
Write-Host "Test6: tools/call search_organisations_by_coordinates" -ForegroundColor Yellow
Invoke-McpRequest -Method 'tools/call' -Params @{ name='search_organisations_by_coordinates'; arguments=@{ organisationType='GPB'; latitude=51.5074; longitude=-0.1278; maxResults=3 } } -Id 6
Divider

# Test7: get_health_topic
Write-Host "Test7: tools/call get_health_topic" -ForegroundColor Yellow
Invoke-McpRequest -Method 'tools/call' -Params @{ name='get_health_topic'; arguments=@{ topic='asthma' } } -Id 7
Divider

# Test8: ping
Write-Host "Test8: ping" -ForegroundColor Yellow
Invoke-McpRequest -Method 'ping' -Params @{} -Id 8
Divider

# Test9: invalid tool name (error expected)
Write-Host "Test9: invalid tool (expect error)" -ForegroundColor Yellow
Invoke-McpRequest -Method 'tools/call' -Params @{ name='invalid_tool'; arguments=@{} } -Id 9
Divider

# Test10: invalid postcode (error path validation)" -ForegroundColor Yellow
Write-Host "Test10: invalid postcode (expect error)" -ForegroundColor Yellow
Invoke-McpRequest -Method 'tools/call' -Params @{ name='convert_postcode_to_coordinates'; arguments=@{ postcode='INVALID' } } -Id 10
Divider

Write-Host 'All tests attempted.' -ForegroundColor Green
