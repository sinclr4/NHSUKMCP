# Test NHS MCP Azure Functions (PowerShell)
# Usage: .\test-functions.ps1 [base-url]
# Example: .\test-functions.ps1 "http://localhost:7071"
# Example: .\test-functions.ps1 "https://nhsmcp-functions.azurewebsites.net"

param(
  [string]$BaseUrl = "http://localhost:7071"
)

Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "Testing NHS MCP Azure Functions" -ForegroundColor Cyan
Write-Host "Base URL: $BaseUrl" -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""

# Test 1: List Tools
Write-Host "Test 1: List Tools" -ForegroundColor Yellow
Write-Host "GET $BaseUrl/mcp/tools"
try {
    $response = Invoke-RestMethod -Uri "$BaseUrl/mcp/tools" -Method Get
    $response | ConvertTo-Json -Depth 10
} catch {
    Write-Host "Failed: $_" -ForegroundColor Red
}
Write-Host ""
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""

# Test 2: Get Organisation Types
Write-Host "Test 2: Get Organisation Types (SSE)" -ForegroundColor Yellow
Write-Host "GET $BaseUrl/mcp/tools/get_organisation_types"
try {
    $response = Invoke-WebRequest -Uri "$BaseUrl/mcp/tools/get_organisation_types" -Method Get
    Write-Host $response.Content
} catch {
    Write-Host "Failed: $_" -ForegroundColor Red
}
Write-Host ""
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""

# Test 3: Convert Postcode (GET)
Write-Host "Test 3: Convert Postcode to Coordinates (GET)" -ForegroundColor Yellow
Write-Host "GET $BaseUrl/mcp/tools/convert_postcode_to_coordinates?postcode=SW1A 1AA"
try {
    $response = Invoke-WebRequest -Uri "$BaseUrl/mcp/tools/convert_postcode_to_coordinates?postcode=SW1A%201AA" -Method Get
    Write-Host $response.Content
} catch {
    Write-Host "Failed: $_" -ForegroundColor Red
}
Write-Host ""
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""

# Test 4: Convert Postcode (POST)
Write-Host "Test 4: Convert Postcode to Coordinates (POST)" -ForegroundColor Yellow
Write-Host "POST $BaseUrl/mcp/tools/convert_postcode_to_coordinates"
try {
    $body = @{
        postcode = "M1 1AE"
    } | ConvertTo-Json
    
    $response = Invoke-WebRequest -Uri "$BaseUrl/mcp/tools/convert_postcode_to_coordinates" -Method Post -Body $body -ContentType "application/json"
    Write-Host $response.Content
} catch {
    Write-Host "Failed: $_" -ForegroundColor Red
}
Write-Host ""
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""

# Test 5: Search by Postcode (GET)
Write-Host "Test 5: Search Organisations by Postcode (GET)" -ForegroundColor Yellow
Write-Host "GET $BaseUrl/mcp/tools/search_organisations_by_postcode?organisationType=PHA&postcode=SW1A 1AA&maxResults=3"
try {
    $response = Invoke-WebRequest -Uri "$BaseUrl/mcp/tools/search_organisations_by_postcode?organisationType=PHA&postcode=SW1A%201AA&maxResults=3" -Method Get
    Write-Host $response.Content
} catch {
    Write-Host "Failed: $_" -ForegroundColor Red
}
Write-Host ""
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""

# Test 6: Search by Postcode (POST)
Write-Host "Test 6: Search Organisations by Postcode (POST)" -ForegroundColor Yellow
Write-Host "POST $BaseUrl/mcp/tools/search_organisations_by_postcode"
try {
    $body = @{
        organisationType = "GPB"
        postcode = "M1 1AE"
        maxResults = 3
 } | ConvertTo-Json
    
    $response = Invoke-WebRequest -Uri "$BaseUrl/mcp/tools/search_organisations_by_postcode" -Method Post -Body $body -ContentType "application/json"
    Write-Host $response.Content
} catch {
    Write-Host "Failed: $_" -ForegroundColor Red
}
Write-Host ""
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""

# Test 7: Search by Coordinates (GET)
Write-Host "Test 7: Search Organisations by Coordinates (GET)" -ForegroundColor Yellow
Write-Host "GET $BaseUrl/mcp/tools/search_organisations_by_coordinates?organisationType=HOS&latitude=51.5074&longitude=-0.1278&maxResults=3"
try {
    $response = Invoke-WebRequest -Uri "$BaseUrl/mcp/tools/search_organisations_by_coordinates?organisationType=HOS&latitude=51.5074&longitude=-0.1278&maxResults=3" -Method Get
Write-Host $response.Content
} catch {
    Write-Host "Failed: $_" -ForegroundColor Red
}
Write-Host ""
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""

# Test 8: Search by Coordinates (POST)
Write-Host "Test 8: Search Organisations by Coordinates (POST)" -ForegroundColor Yellow
Write-Host "POST $BaseUrl/mcp/tools/search_organisations_by_coordinates"
try {
    $body = @{
        organisationType = "DEN"
        latitude = 53.4808
        longitude = -2.2426
     maxResults = 3
 } | ConvertTo-Json
    
    $response = Invoke-WebRequest -Uri "$BaseUrl/mcp/tools/search_organisations_by_coordinates" -Method Post -Body $body -ContentType "application/json"
  Write-Host $response.Content
} catch {
    Write-Host "Failed: $_" -ForegroundColor Red
}
Write-Host ""
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""

# Test 9: Get Health Topic (GET)
Write-Host "Test 9: Get Health Topic (GET)" -ForegroundColor Yellow
Write-Host "GET $BaseUrl/mcp/tools/get_health_topic?topic=asthma"
try {
    $response = Invoke-WebRequest -Uri "$BaseUrl/mcp/tools/get_health_topic?topic=asthma" -Method Get
    Write-Host $response.Content
} catch {
    Write-Host "Failed: $_" -ForegroundColor Red
}
Write-Host ""
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""

# Test 10: Get Health Topic (POST)
Write-Host "Test 10: Get Health Topic (POST)" -ForegroundColor Yellow
Write-Host "POST $BaseUrl/mcp/tools/get_health_topic"
try {
    $body = @{
        topic = "diabetes"
    } | ConvertTo-Json
    
    $response = Invoke-WebRequest -Uri "$BaseUrl/mcp/tools/get_health_topic" -Method Post -Body $body -ContentType "application/json"
    Write-Host $response.Content
} catch {
    Write-Host "Failed: $_" -ForegroundColor Red
}
Write-Host ""
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "All tests completed!" -ForegroundColor Green
Write-Host ""
