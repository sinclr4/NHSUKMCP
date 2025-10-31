# NHS Organisations MCP Server - Azure Functions

This is an **Azure Functions** implementation that provides Model Context Protocol (MCP) tools via native JSON-RPC endpoint for searching NHS organizations and health information.

## ?? Quick Start

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Azure Functions Core Tools v4](https://docs.microsoft.com/azure/azure-functions/functions-run-local)
- Azure API Management subscription key

### Local Development

1. **Clone the repository**:
   ```bash
   git clone https://github.com/sinclr4/NHSUKMCP.git
   cd NHSUKMCP
   ```

2. **Configure settings**:
   Create/update `local.settings.json`:
   ```json
   {
     "IsEncrypted": false,
 "Values": {
     "AzureWebJobsStorage": "UseDevelopmentStorage=true",
       "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
       "API_MANAGEMENT_ENDPOINT": "https://nhsuk-apim-int-uks.azure-api.net/service-search",
       "API_MANAGEMENT_SUBSCRIPTION_KEY": "your-subscription-key-here"
  }
   }
 ```

3. **Run locally**:
   ```bash
   func start
   ```
   
   **Note**: If you get "port is already in use" error:
   ```powershell
   # Clear specific port
   .\clear-port-7071.ps1
   
   # Or clear all processes
   .\cleanup-all.ps1
   ```
   See [CLEARING_PORTS.md](CLEARING_PORTS.md) for details.

4. **Test endpoint**:
   ```bash
   # MCP JSON-RPC (Native Protocol)
   curl -X POST http://localhost:7071/api/mcp \
     -H "Content-Type: application/json" \
     -d '{"jsonrpc":"2.0","id":1,"method":"tools/list","params":{}}'

   # Or use the test script
   ./test-mcp-jsonrpc.sh    # Test JSON-RPC
   ```

## ? Features

- ?? **Native MCP Protocol**: JSON-RPC 2.0 implementation (`/api/mcp` endpoint)
- ?? **Tool Discovery**: MCP `tools/list` for discovering available tools
- ?? **Organisation Search**: Find NHS organizations by postcode or coordinates
- ?? **Organisation Types**: Get all available NHS organization types
- ?? **Postcode Conversion**: Convert UK postcodes to latitude/longitude
- ?? **Health Information**: Retrieve detailed NHS health topic information

## ?? API Endpoint

### MCP JSON-RPC Endpoint (Native Protocol)

**POST** `/api/mcp`

Native Model Context Protocol implementation with JSON-RPC 2.0.

```bash
# Initialize
curl -X POST http://localhost:7071/api/mcp \
  -H "Content-Type: application/json" \
  -d '{
 "jsonrpc": "2.0",
    "id": 1,
    "method": "initialize",
    "params": {"protocolVersion": "2024-11-05"}
  }'

# List tools
curl -X POST http://localhost:7071/api/mcp \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","id":2,"method":"tools/list","params":{}}'

# Call a tool
curl -X POST http://localhost:7071/api/mcp \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 3,
    "method": "tools/call",
    "params": {
      "name": "search_organisations_by_postcode",
      "arguments": {"organisationType":"PHA","postcode":"SW1A 1AA","maxResults":5}
    }
  }'
```

?? **Full MCP JSON-RPC Documentation**: [MCP_JSON_RPC_GUIDE.md](MCP_JSON_RPC_GUIDE.md)

## ?? Organisation Types

| Code | Description |
|------|-------------|
| CCG | Clinical Commissioning Group |
| CLI | Clinics |
| DEN | Dentists |
| GPB | GP |
| GPP | GP Practice |
| HOS | Hospital |
| MIU | Minor Injury Unit |
| OPT | Optician |
| PHA | Pharmacy |
| UC | Urgent Care |

## ?? Response Format

### JSON-RPC Format (MCP Native)

```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
    "content": [
      {"type": "text", "text": "{\"organisations\":[...]}"}
    ]
  }
}
```

## ?? Deploy to Azure

```bash
# Run the deployment script
chmod +x deploy-functions.sh
./deploy-functions.sh <resource-group> <function-app-name> <location>
```

See [README_AZURE_FUNCTIONS.md](README_AZURE_FUNCTIONS.md) for detailed deployment instructions.

## ?? Testing

### Test Scripts

```bash
# Test MCP JSON-RPC protocol
./test-mcp-jsonrpc.sh http://localhost:7071
# or
.\test-mcp-jsonrpc.ps1 "http://localhost:7071"
```

### MCP Inspector (Interactive Testing)

The official MCP Inspector provides an interactive UI for testing:

```bash
# Quick diagnostic check
.\diagnose-mcp-inspector.ps1

# Launch Inspector (USE /api/mcp endpoint!)
npx @modelcontextprotocol/inspector http://localhost:7071/api/mcp
```

?? **Important**: Use `/api/mcp` (with `/api` prefix) for the Inspector!

**Troubleshooting?** See [MCP_INSPECTOR_TROUBLESHOOTING.md](MCP_INSPECTOR_TROUBLESHOOTING.md)

**Full testing guide**: [TESTING_WITH_MCP_INSPECTOR.md](TESTING_WITH_MCP_INSPECTOR.md)

## ?? Project Structure

```
NHSUKMCP/
??? Functions/
?   ??? McpJsonRpcFunctions.cs  # MCP JSON-RPC endpoint
??? Tools/
?   ??? NHSOrganisationSearchTools.cs  # MCP tool implementations
?   ??? NHSHealthContentTools.cs       # Health content tools
??? Models/
?   ??? Models.cs     # Data models
??? Services/
?   ??? AzureSearchService.cs    # API Management integration
??? Program.cs         # Azure Functions host
??? host.json    # Functions configuration
??? test-mcp-jsonrpc.ps1   # JSON-RPC tests
```

## ?? Documentation

- [MCP JSON-RPC Guide](MCP_JSON_RPC_GUIDE.md) - Native MCP protocol usage
- [MCP Inspector Testing](TESTING_WITH_MCP_INSPECTOR.md) - Interactive testing guide
- [MCP Inspector Troubleshooting](MCP_INSPECTOR_TROUBLESHOOTING.md) - Fix connection issues
- [Azure Functions Guide](README_AZURE_FUNCTIONS.md) - Deployment guide
- [Migration Guide](MIGRATION_GUIDE.md) - Migration from console app
- [Quick Start](QUICKSTART.md) - 5-minute setup guide
- [Conversion Summary](CONVERSION_SUMMARY.md) - Complete conversion details

## ?? License

MIT License - see LICENSE file for details.

---

**Built with native MCP protocol support** ??
