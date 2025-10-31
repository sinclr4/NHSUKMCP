# NHS Organisations MCP Server - Azure Functions

This is an **Azure Functions** implementation that provides Model Context Protocol (MCP) tools via both native JSON-RPC and streamable HTTP endpoints for searching NHS organizations and health information.

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

4. **Test endpoints**:
   ```bash
   # MCP JSON-RPC (Native Protocol)
   curl -X POST http://localhost:7071/mcp \
     -H "Content-Type: application/json" \
     -d '{"jsonrpc":"2.0","id":1,"method":"tools/list","params":{}}'

   # Or use SSE streaming endpoints
   curl http://localhost:7071/mcp/tools/get_organisation_types

   # Or use the test scripts
   ./test-mcp-jsonrpc.sh    # Test JSON-RPC
   ./test-functions.sh      # Test SSE endpoints
   ```

## ?? Features

- ? **Native MCP Protocol**: JSON-RPC 2.0 implementation (`/mcp` endpoint)
- ? **SSE Streaming**: Server-Sent Events for progressive data delivery
- ? **Dual Interface**: Both JSON-RPC and HTTP/SSE endpoints
- ? **Tool Discovery**: MCP `tools/list` and REST `/mcp/tools`
- ? **Organisation Search**: Find NHS organizations by postcode or coordinates
- ? **Organisation Types**: Get all available NHS organization types
- ? **Postcode Conversion**: Convert UK postcodes to latitude/longitude
- ? **Health Information**: Retrieve detailed NHS health topic information
- ? **GET & POST Support**: SSE endpoints support both HTTP methods

## ?? API Endpoints

### MCP JSON-RPC Endpoint (Native Protocol)

**POST** `/mcp`

Native Model Context Protocol implementation with JSON-RPC 2.0.

```bash
# Initialize
curl -X POST http://localhost:7071/mcp \
-H "Content-Type: application/json" \
-d '{
    "jsonrpc": "2.0",
    "id": 1,
"method": "initialize",
    "params": {"protocolVersion": "2024-11-05"}
  }'

# List tools
curl -X POST http://localhost:7071/mcp \
  -H "Content-Type: application/json" \
-d '{"jsonrpc":"2.0","id":2,"method":"tools/list","params":{} }'

# Call a tool
curl -X POST http://localhost:7071/mcp \
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

### SSE Streaming Endpoints (HTTP/REST)

Alternative HTTP endpoints with Server-Sent Events for streaming.

**GET** `/mcp/tools/get_organisation_types` - Returns all NHS organisation types
**GET/POST** `/mcp/tools/convert_postcode_to_coordinates` - Convert postcode
**GET/POST** `/mcp/tools/search_organisations_by_postcode` - Search by postcode
**GET/POST** `/mcp/tools/search_organisations_by_coordinates` - Search by coordinates
**GET/POST** `/mcp/tools/get_health_topic` - Get health information

```bash
# Example SSE request
curl -N "http://localhost:7071/mcp/tools/search_organisations_by_postcode?organisationType=PHA&postcode=SW1A%201AA&maxResults=5"
```

?? **Full SSE Documentation**: [README_AZURE_FUNCTIONS.md](README_AZURE_FUNCTIONS.md)

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

## ?? Response Formats

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

### SSE Format (Streaming)

```
event: metadata
data: {"postcode":"SW1A 1AA"}

event: organisation
data: {"organisationName":"Boots Pharmacy",...}

event: complete
data: {"success":true}
```

## ?? Deploy to Azure

```bash
# Run the deployment script
chmod +x deploy-functions.sh
./deploy-functions.sh <resource-group> <function-app-name> <location>
```

See [README_AZURE_FUNCTIONS.md](README_AZURE_FUNCTIONS.md) for detailed deployment instructions.

## ?? Testing

```bash
# Test MCP JSON-RPC protocol
./test-mcp-jsonrpc.sh http://localhost:7071
# or
.\test-mcp-jsonrpc.ps1 "http://localhost:7071"

# Test SSE streaming endpoints
./test-functions.sh http://localhost:7071
# or
.\test-functions.ps1 "http://localhost:7071"
```

## ?? Project Structure

```
NHSUKMCP/
??? Functions/
?   ??? McpJsonRpcFunctions.cs  # MCP JSON-RPC endpoint
?   ??? McpFunctions.cs   # SSE streaming endpoints
??? Tools/
?   ??? NHSOrganisationSearchTools.cs  # MCP tool implementations
?   ??? NHSHealthContentTools.cs       # Health content tools
??? Models/
?   ??? Models.cs                # Data models
??? Services/
?   ??? AzureSearchService.cs    # API Management integration
??? Program.cs         # Azure Functions host
??? host.json      # Functions configuration
??? test-mcp-jsonrpc.sh          # JSON-RPC tests (Bash)
??? test-mcp-jsonrpc.ps1    # JSON-RPC tests (PowerShell)
??? test-functions.sh   # SSE tests (Bash)
??? test-functions.ps1  # SSE tests (PowerShell)
```

## ?? Which Endpoint Should I Use?

### Use MCP JSON-RPC (`/mcp`) when:
- ? Building MCP-compliant clients
- ? Need native protocol integration
- ? Want standard tool calling
- ? Building AI agents or assistants

### Use SSE Endpoints (`/mcp/tools/*`) when:
- ? Building web UIs
- ? Need progressive/streaming responses
- ? Want simple HTTP GET/POST
- ? Browser-based applications

## ?? Documentation

- [MCP JSON-RPC Guide](MCP_JSON_RPC_GUIDE.md) - Native MCP protocol usage
- [Azure Functions Guide](README_AZURE_FUNCTIONS.md) - SSE streaming endpoints
- [Migration Guide](MIGRATION_GUIDE.md) - Migration from console app
- [Quick Start](QUICKSTART.md) - 5-minute setup guide
- [Conversion Summary](CONVERSION_SUMMARY.md) - Complete conversion details

## ?? License

MIT License - see LICENSE file for details.

---

**Built with native MCP protocol support and streaming HTTP endpoints** ??
