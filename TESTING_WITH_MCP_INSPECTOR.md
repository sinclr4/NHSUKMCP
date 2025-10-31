# Testing with MCP Inspector

**? Inspector not working? Start here: [MCP_INSPECTOR_QUICK_HELP.md](MCP_INSPECTOR_QUICK_HELP.md)**

This guide shows how to test the NHS Organisations MCP Server using the official MCP Inspector tool.

## ? Quick Start (TL;DR)

**Having trouble?** Run the diagnostic script first:
```powershell
.\diagnose-mcp-inspector.ps1
```

**If everything is working:**
```powershell
# Terminal 1: Start Functions
func start

# Terminal 2: Launch Inspector (USE THE /api PREFIX!)
npx @modelcontextprotocol/inspector http://localhost:7071/api/mcp
```

**?? Common mistake**: Using `http://localhost:7071/mcp` (missing `/api`)  
**? Correct URL**: `http://localhost:7071/api/mcp`

**Still having issues?** See:
- [MCP_INSPECTOR_QUICK_HELP.md](MCP_INSPECTOR_QUICK_HELP.md) - Quick fixes
- [MCP_INSPECTOR_TROUBLESHOOTING.md](MCP_INSPECTOR_TROUBLESHOOTING.md) - Detailed troubleshooting

---

## What is MCP Inspector?

The MCP Inspector is an official debugging and testing tool for MCP servers. It provides:
- Interactive tool calling interface
- Request/response inspection
- JSON-RPC protocol validation
- Real-time testing

## Prerequisites

1. **Node.js 18+** installed
2. **MCP Inspector** installed globally:
   ```bash
   npm install -g @modelcontextprotocol/inspector
   ```

3. **NHS MCP Server running locally**:
   ```bash
   func start
   ```

## Option 1: Testing via HTTP Transport

The MCP Inspector can connect to HTTP-based MCP servers.

### Start the Server

```bash
# Terminal 1: Start Azure Functions
cd C:\Users\robsinclair\Source\Repos\NHSUKMCP
func start
```

The server will be available at `http://localhost:7071/mcp`

### Launch MCP Inspector

```bash
# Terminal 2: Launch Inspector
npx @modelcontextprotocol/inspector http://localhost:7071/mcp
```

This will:
1. Open a web interface (usually at `http://localhost:5173`)
2. Connect to your MCP server
3. Show available tools

### Using the Inspector UI

1. **View Server Info**
   - See server name, version, and capabilities
   - Confirm protocol version (2024-11-05)

2. **List Tools**
   - Click "Tools" tab
   - See all 5 NHS tools with their schemas

3. **Call Tools**
   - Select a tool from the list
   - Fill in the arguments
   - Click "Call Tool"
   - See the response

### Example: Testing get_organisation_types

1. Select `get_organisation_types` tool
2. No arguments needed (leave empty)
3. Click "Call Tool"
4. Response should show all organisation types:
   ```json
   {
     "CCG": "Clinical Commissioning Group",
     "PHA": "Pharmacy",
   ...
   }
   ```

### Example: Testing search_organisations_by_postcode

1. Select `search_organisations_by_postcode` tool
2. Fill in arguments:
   ```json
   {
 "organisationType": "PHA",
     "postcode": "SW1A 1AA",
   "maxResults": 5
   }
   ```
3. Click "Call Tool"
4. Response shows nearby pharmacies with distances

## Option 2: Testing via stdio (Alternative)

If you want to test the server in stdio mode (like Claude Desktop), you can create a wrapper script.

### Create Wrapper Script

**Windows PowerShell** (`run-mcp-stdio.ps1`):
```powershell
# Start the Azure Functions host and proxy stdio to HTTP
$functionsProcess = Start-Process -FilePath "func" -ArgumentList "start" -PassThru -NoNewWindow

Start-Sleep -Seconds 5

# Now connect Inspector via HTTP
npx @modelcontextprotocol/inspector http://localhost:7071/mcp

# Cleanup
$functionsProcess.Kill()
```

**Linux/Mac Bash** (`run-mcp-stdio.sh`):
```bash
#!/bin/bash
# Start Functions in background
func start &
FUNC_PID=$!

sleep 5

# Connect Inspector
npx @modelcontextprotocol/inspector http://localhost:7071/mcp

# Cleanup
kill $FUNC_PID
```

## Testing Checklist

Use this checklist to verify all functionality:

### ? Server Initialization
- [ ] Inspector connects successfully
- [ ] Server info displays correctly
- [ ] Protocol version is 2024-11-05
- [ ] Capabilities show `tools` support

### ? Tool Discovery
- [ ] All 5 tools are listed
- [ ] Each tool has a description
- [ ] Input schemas are present and valid
- [ ] Required fields are marked

### ? Tool: get_organisation_types
- [ ] Tool executes without errors
- [ ] Returns dictionary of org types
- [ ] All expected types present (CCG, PHA, GPB, etc.)

### ? Tool: convert_postcode_to_coordinates
**Test Case 1: Valid Postcode**
- [ ] Input: `{"postcode": "SW1A 1AA"}`
- [ ] Returns coordinates
- [ ] Latitude and longitude are valid numbers

**Test Case 2: Invalid Postcode**
- [ ] Input: `{"postcode": "INVALID"}`
- [ ] Returns error message
- [ ] Error indicates postcode not found

### ? Tool: search_organisations_by_postcode
**Test Case 1: Pharmacies in Central London**
- [ ] Input: `{"organisationType": "PHA", "postcode": "SW1A 1AA", "maxResults": 5}`
- [ ] Returns up to 5 pharmacies
- [ ] Each has name, distance, coordinates
- [ ] Results sorted by distance

**Test Case 2: Invalid Organisation Type**
- [ ] Input: `{"organisationType": "INVALID", "postcode": "SW1A 1AA"}`
- [ ] Returns error about invalid type
- [ ] Suggests using get_organisation_types

### ? Tool: search_organisations_by_coordinates
**Test Case 1: GPs near London**
- [ ] Input: `{"organisationType": "GPB", "latitude": 51.5074, "longitude": -0.1278, "maxResults": 10}`
- [ ] Returns up to 10 GP practices
- [ ] Results include distance calculations

### ? Tool: get_health_topic
**Test Case 1: Common Condition (Asthma)**
- [ ] Input: `{"topic": "asthma"}`
- [ ] Returns health information
- [ ] Includes sections with headlines
- [ ] Has URL and last reviewed date

**Test Case 2: Invalid Topic**
- [ ] Input: `{"topic": "nonexistent-condition"}`
- [ ] Returns error message
- [ ] Error indicates topic not found

## Inspector Features to Explore

### 1. Request/Response Inspection

The Inspector shows full JSON-RPC messages:

**Request**:
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "tools/call",
  "params": {
    "name": "get_organisation_types",
 "arguments": {}
  }
}
```

**Response**:
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
 "content": [
{
        "type": "text",
 "text": "{\"CCG\":\"Clinical Commissioning Group\",...}"
      }
    ]
}
}
```

### 2. Error Inspection

When tools fail, you can see detailed error information:

```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
    "content": [
      {
        "type": "text",
 "text": "Error: Postcode 'INVALID' not found"
      }
    ],
    "isError": true
  }
}
```

### 3. Protocol Validation

The Inspector validates:
- JSON-RPC 2.0 compliance
- MCP protocol adherence
- Schema validation
- Response format correctness

## Debugging Tips

### Server Not Connecting

If Inspector can't connect:

1. **Check Azure Functions is running**:
   ```bash
   curl http://localhost:7071/mcp -X POST -H "Content-Type: application/json" -d '{"jsonrpc":"2.0","id":1,"method":"ping","params":{}}'
   ```

2. **Check port availability**:
   ```bash
   netstat -an | findstr 7071
   ```

3. **Check Functions logs**:
   Look for errors in the Functions console output

### Tools Not Listed

If tools don't appear:

1. **Test tools/list directly**:
   ```bash
   curl http://localhost:7071/mcp -X POST -H "Content-Type: application/json" -d '{"jsonrpc":"2.0","id":1,"method":"tools/list","params":{}}'
   ```

2. **Check tool registration**:
   - Verify `Program.cs` registers tools
   - Check tool classes have `[McpServerToolType]` attribute
   - Verify methods have `[McpServerTool]` attribute

### Tool Calls Fail

If tool execution fails:

1. **Check API Management credentials**:
   - Verify `API_MANAGEMENT_SUBSCRIPTION_KEY` is set
   - Test key works with direct API call

2. **Check tool arguments**:
   - Verify all required arguments provided
   - Check argument types match schema

3. **Review error messages**:
   - Read error text in response
   - Check Functions logs for stack traces

## Advanced: Custom Inspector Configuration

Create an `mcp-inspector-config.json`:

```json
{
  "server": {
    "url": "http://localhost:7071/mcp",
    "headers": {
      "Authorization": "Bearer optional-token"
    }
  },
  "ui": {
    "theme": "dark",
    "autoConnect": true
  }
}
```

Launch with config:
```bash
npx @modelcontextprotocol/inspector --config mcp-inspector-config.json
```

## Alternative: Manual Testing with curl

If you prefer command-line testing:

```bash
# Initialize
curl -X POST http://localhost:7071/mcp \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05"}}'

# List tools
curl -X POST http://localhost:7071/mcp \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","id":2,"method":"tools/list","params":{}}'

# Call a tool
curl -X POST http://localhost:7071/mcp \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"get_organisation_types","arguments":{}}}'
```

## Next Steps

After verifying with Inspector:

1. **Integrate with Claude Desktop**
   - Configure in Claude Desktop settings
   - Test conversational interactions

2. **Build Custom Clients**
   - Use the validated JSON-RPC format
   - Implement in Python, JavaScript, or C#

3. **Deploy to Azure**
   - Test with deployed URL
   - Update Inspector URL to production

4. **Monitor in Production**
   - Use Application Insights
   - Track tool usage
   - Monitor errors

## Resources

- [MCP Inspector Documentation](https://github.com/modelcontextprotocol/inspector)
- [MCP Specification](https://spec.modelcontextprotocol.io/)
- [Our JSON-RPC Guide](MCP_JSON_RPC_GUIDE.md)
- [Testing Scripts](test-mcp-jsonrpc.sh)

## Common Inspector Commands

```bash
# Install Inspector globally
npm install -g @modelcontextprotocol/inspector

# Launch with HTTP transport
npx @modelcontextprotocol/inspector http://localhost:7071/mcp

# Launch with custom port
npx @modelcontextprotocol/inspector --port 3000 http://localhost:7071/mcp

# Launch with verbose logging
npx @modelcontextprotocol/inspector --verbose http://localhost:7071/mcp

# Update Inspector
npm update -g @modelcontextprotocol/inspector
```

## Troubleshooting

### "Cannot connect to server"

**Solution**: Ensure Azure Functions is running on port 7071:
```bash
func start --port 7071
```

### "Protocol version mismatch"

**Solution**: Verify server returns protocol version `2024-11-05` in initialize response.

### "Tool not found"

**Solution**: 
1. Check tool name matches exactly (case-sensitive)
2. Verify tool is registered in `Program.cs`
3. Rebuild and restart Functions

### "Invalid arguments"

**Solution**:
1. Check input schema in Inspector
2. Verify all required fields provided
3. Match data types (string, number, integer)

---

**Ready to test! Start your Functions and launch the Inspector** ??
