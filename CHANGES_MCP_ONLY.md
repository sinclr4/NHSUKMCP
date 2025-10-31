# Changes: Simplified to MCP-Only Endpoint

## What Changed

The NHS UK MCP Server has been simplified to **only expose the MCP JSON-RPC endpoint**. All HTTP/SSE streaming endpoints have been removed.

## Changes Made

### 1. Removed Files
- ? **`Functions/McpFunctions.cs`** - Removed all HTTP/SSE streaming endpoints

### 2. Updated Files

#### `README.md`
- Removed references to SSE streaming endpoints
- Updated to show only MCP JSON-RPC endpoint
- Simplified API documentation
- Updated project structure

#### `diagnose-mcp-inspector.ps1`
- Simplified to only test `/api/mcp` endpoint
- Removed testing of alternative URL formats

#### `test-mcp-endpoint.ps1`
- Updated to only test `/api/mcp` endpoint
- Added Claude Desktop configuration example
- Removed fallback URL testing

#### `MCP_INSPECTOR_QUICK_HELP.md`
- Removed all SSE endpoint references
- Simplified to single endpoint
- Updated troubleshooting steps

## Current Architecture

### Single Endpoint
**POST** `/api/mcp` - MCP JSON-RPC 2.0 endpoint

This endpoint handles all MCP protocol operations:
- `initialize` - Initialize the MCP session
- `tools/list` - List available tools
- `tools/call` - Execute a tool

### Available Tools (via MCP)
1. `get_organisation_types` - Get all NHS organisation types
2. `convert_postcode_to_coordinates` - Convert UK postcode to coordinates
3. `search_organisations_by_postcode` - Search organisations by postcode
4. `search_organisations_by_coordinates` - Search organisations by coordinates
5. `get_health_topic` - Get NHS health topic information

## Testing the MCP Endpoint

### Quick Test
```powershell
.\test-mcp-endpoint.ps1
```

### Full Diagnostic
```powershell
.\diagnose-mcp-inspector.ps1
```

### Manual Testing
```powershell
# Initialize
curl.exe -X POST http://localhost:7071/api/mcp `
  -H "Content-Type: application/json" `
  -d '{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"initialize\",\"params\":{\"protocolVersion\":\"2024-11-05\"}}'

# List tools
curl.exe -X POST http://localhost:7071/api/mcp `
  -H "Content-Type: application/json" `
  -d '{\"jsonrpc\":\"2.0\",\"id\":2,\"method\":\"tools/list\",\"params\":{}}'

# Call a tool
curl.exe -X POST http://localhost:7071/api/mcp `
  -H "Content-Type: application/json" `
  -d '{\"jsonrpc\":\"2.0\",\"id\":3,\"method\":\"tools/call\",\"params\":{\"name\":\"get_organisation_types\",\"arguments\":{}}}'
```

### MCP Inspector
```bash
npx @modelcontextprotocol/inspector http://localhost:7071/api/mcp
```

### Claude Desktop
Add to your Claude Desktop configuration (`claude_desktop_config.json`):

**Windows**: `%APPDATA%\Claude\claude_desktop_config.json`
**macOS**: `~/Library/Application Support/Claude/claude_desktop_config.json`

```json
{
  "mcpServers": {
    "nhs-uk": {
      "command": "func",
  "args": ["start", "--port", "7071"],
      "cwd": "C:\\Users\\robsinclair\\Source\\Repos\\NHSUKMCP",
      "env": {
 "API_MANAGEMENT_SUBSCRIPTION_KEY": "your-key-here"
      },
   "transport": {
        "type": "http",
        "url": "http://localhost:7071/api/mcp"
      }
    }
  }
}
```

## Benefits of This Change

### ? Simplified
- Single endpoint to maintain
- Clearer purpose (MCP protocol only)
- Easier to test and troubleshoot

### ? MCP Standard Compliant
- Fully compliant with MCP specification
- Works with any MCP client
- Compatible with Claude Desktop, MCP Inspector, etc.

### ? Cleaner Codebase
- Less code to maintain
- Single responsibility (MCP protocol)
- Removed duplicate functionality

## What Was Removed

### HTTP/SSE Streaming Endpoints (No Longer Available)
- ? `GET /api/mcp/tools` - List tools (replaced by MCP `tools/list`)
- ? `GET/POST /api/mcp/tools/get_organisation_types` - Get types (use MCP `tools/call`)
- ? `GET/POST /api/mcp/tools/convert_postcode_to_coordinates` - Convert postcode (use MCP `tools/call`)
- ? `GET/POST /api/mcp/tools/search_organisations_by_postcode` - Search by postcode (use MCP `tools/call`)
- ? `GET/POST /api/mcp/tools/search_organisations_by_coordinates` - Search by coords (use MCP `tools/call`)
- ? `GET/POST /api/mcp/tools/get_health_topic` - Get health info (use MCP `tools/call`)

### Why These Were Removed
- Duplicate functionality (same tools available via MCP)
- Non-standard approach (MCP is the standard)
- Harder to maintain two interfaces
- SSE streaming not needed for this use case

## Migration Guide

If you were using the HTTP/SSE endpoints, migrate to MCP protocol:

### Before (HTTP/SSE)
```bash
curl "http://localhost:7071/api/mcp/tools/search_organisations_by_postcode?organisationType=PHA&postcode=SW1A%201AA&maxResults=5"
```

### After (MCP JSON-RPC)
```bash
curl -X POST http://localhost:7071/api/mcp \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 1,
    "method": "tools/call",
    "params": {
  "name": "search_organisations_by_postcode",
      "arguments": {
        "organisationType": "PHA",
   "postcode": "SW1A 1AA",
        "maxResults": 5
      }
    }
  }'
```

## Running the Server

### Start Functions
```powershell
func start
```

**Expected output:**
```
Functions:
        Mcp: [POST] http://localhost:7071/api/mcp
```

You should see **only one function** listed now!

### Verify It's Working
```powershell
.\test-mcp-endpoint.ps1
```

### Test with Inspector
```bash
npx @modelcontextprotocol/inspector http://localhost:7071/api/mcp
```

## Next Steps

1. **Test the endpoint**: Run `.\test-mcp-endpoint.ps1`
2. **Run diagnostics**: Run `.\diagnose-mcp-inspector.ps1`
3. **Use Inspector**: Test interactively with MCP Inspector
4. **Configure Claude**: Add to Claude Desktop configuration
5. **Deploy to Azure**: Deploy using the deployment scripts

## Troubleshooting

### Inspector Can't Connect

1. Make sure Functions is running: `func start`
2. Check you're using the correct URL: `http://localhost:7071/api/mcp`
3. Run diagnostics: `.\diagnose-mcp-inspector.ps1`
4. See: [MCP_INSPECTOR_TROUBLESHOOTING.md](MCP_INSPECTOR_TROUBLESHOOTING.md)

### Tools Not Showing

1. Rebuild: `dotnet clean && dotnet build`
2. Restart Functions: `func start`
3. Verify endpoint: `.\test-mcp-endpoint.ps1`

### API Key Issues

Update `local.settings.json`:
```json
{
  "Values": {
    "API_MANAGEMENT_SUBSCRIPTION_KEY": "your-actual-key-here"
  }
}
```

## Documentation

- [README.md](README.md) - Main documentation
- [MCP_JSON_RPC_GUIDE.md](MCP_JSON_RPC_GUIDE.md) - MCP protocol guide
- [TESTING_WITH_MCP_INSPECTOR.md](TESTING_WITH_MCP_INSPECTOR.md) - Testing guide
- [MCP_INSPECTOR_TROUBLESHOOTING.md](MCP_INSPECTOR_TROUBLESHOOTING.md) - Troubleshooting
- [MCP_INSPECTOR_QUICK_HELP.md](MCP_INSPECTOR_QUICK_HELP.md) - Quick reference

---

**Summary**: The server now exposes **only the MCP JSON-RPC endpoint** at `/api/mcp`. All functionality is available through standard MCP protocol operations. This makes the server simpler, more maintainable, and fully MCP-compliant. ?
