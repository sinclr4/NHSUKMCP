#!/usr/bin/env pwsh
# Quick test to verify MCP endpoint is working

$baseUrl = "http://localhost:7071"
$mcpEndpoint = "$baseUrl/api/mcp"

Write-Host "Testing MCP endpoint at $mcpEndpoint..." -ForegroundColor Cyan
Write-Host ""

# Test with /api prefix


Write-Host "[Testing] $mcpEndpoint" -ForegroundColor Yellow
try {
    $response = curl.exe -X POST $mcpEndpoint `
        -H "Content-Type: application/json" `
        -d '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05"}}' `
  --silent --max-time 5 2>&1
    
    if ($response -match '"jsonrpc"' -and $response -match '"result"') {
        Write-Host "  ? SUCCESS - Endpoint is working!" -ForegroundColor Green
        Write-Host ""
        Write-Host "Use this URL with MCP Inspector:" -ForegroundColor Cyan
        Write-Host "  npx @modelcontextprotocol/inspector $mcpEndpoint" -ForegroundColor White
   Write-Host ""
        Write-Host "Or test with Claude Desktop - add to config:" -ForegroundColor Cyan
        Write-Host '  "mcpServers": {' -ForegroundColor Gray
        Write-Host '    "nhs-uk": {' -ForegroundColor Gray
 Write-Host '      "url": "' -NoNewline -ForegroundColor Gray
        Write-Host $mcpEndpoint -NoNewline -ForegroundColor White
        Write-Host '"' -ForegroundColor Gray
        Write-Host '    }' -ForegroundColor Gray
        Write-Host '  }' -ForegroundColor Gray
        exit 0
    } else {
     Write-Host "  ? FAILED - Got: $response" -ForegroundColor Red
    }
} catch {
    Write-Host "  ? ERROR: $_" -ForegroundColor Red
}

Write-Host ""
Write-Host "? MCP endpoint is not working" -ForegroundColor Red
Write-Host ""
Write-Host "Possible issues:" -ForegroundColor Yellow
Write-Host "  1. Azure Functions is not running" -ForegroundColor Gray
Write-Host "     Fix: Run 'func start' in your project directory" -ForegroundColor Gray
Write-Host ""
Write-Host "  2. Functions is running on a different port" -ForegroundColor Gray
Write-Host "     Fix: Check the 'func start' output for the correct port" -ForegroundColor Gray
Write-Host ""
Write-Host "  3. Function build failed" -ForegroundColor Gray
Write-Host "     Fix: Run 'dotnet build' and check for errors" -ForegroundColor Gray
Write-Host ""
Write-Host "Run the full diagnostic:" -ForegroundColor Cyan
Write-Host "  .\diagnose-mcp-inspector.ps1" -ForegroundColor White
Write-Host ""

exit 1
