# MCP JSON-RPC Implementation Guide

This guide explains how to use the NHS Organisations MCP Server with the native Model Context Protocol (MCP) JSON-RPC implementation.

## Overview

The NHS MCP Server now implements the official MCP protocol specification with JSON-RPC 2.0 over HTTP. This allows native integration with MCP clients and provides standardized tool calling.

## Endpoints

### MCP JSON-RPC Endpoint
**POST** `/mcp`

All MCP protocol requests use this single endpoint with JSON-RPC 2.0 format.

## MCP Protocol Methods

### 1. Initialize

Initialize the MCP session and discover server capabilities.

**Request**:
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "initialize",
  "params": {
    "protocolVersion": "2024-11-05",
    "clientInfo": {
  "name": "my-client",
   "version": "1.0.0"
    }
  }
}
```

**Response**:
```json
{
  "jsonrpc": "2.0",
 "id": 1,
  "result": {
    "protocolVersion": "2024-11-05",
    "serverInfo": {
      "name": "nhs-uk-mcp-server",
      "version": "1.0.0"
    },
    "capabilities": {
"tools": {
       "listChanged": true
      }
    }
  }
}
```

### 2. List Tools

Get all available MCP tools with their input schemas.

**Request**:
```json
{
  "jsonrpc": "2.0",
  "id": 2,
"method": "tools/list",
  "params": {}
}
```

**Response**:
```json
{
  "jsonrpc": "2.0",
  "id": 2,
  "result": {
    "tools": [
    {
        "name": "get_organisation_types",
 "description": "Get a list of all available NHS organisation types",
 "inputSchema": {
          "type": "object",
        "properties": {},
     "required": []
  }
  },
      {
   "name": "convert_postcode_to_coordinates",
      "description": "Convert a UK postcode to latitude and longitude coordinates",
"inputSchema": {
          "type": "object",
          "properties": {
  "postcode": {
              "type": "string",
 "description": "UK postcode"
            }
          },
          "required": ["postcode"]
  }
      }
      // ... more tools
    ]
  }
}
```

### 3. Call Tool

Execute a specific tool with arguments.

**Request - Get Organisation Types**:
```json
{
  "jsonrpc": "2.0",
  "id": 3,
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
  "id": 3,
  "result": {
"content": [
      {
        "type": "text",
       "text": "{\"CCG\":\"Clinical Commissioning Group\",\"PHA\":\"Pharmacy\",...}"
      }
    ]
  }
}
```

**Request - Search by Postcode**:
```json
{
  "jsonrpc": "2.0",
  "id": 4,
  "method": "tools/call",
  "params": {
    "name": "search_organisations_by_postcode",
  "arguments": {
"organisationType": "PHA",
      "postcode": "SW1A 1AA",
      "maxResults": 5
    }
  }
}
```

**Response**:
```json
{
 "jsonrpc": "2.0",
  "id": 4,
  "result": {
 "content": [
      {
    "type": "text",
      "text": "{\"postcode\":\"SW1A 1AA\",\"organisations\":[...]}"
      }
    ]
  }
}
```

**Request - Get Health Topic**:
```json
{
  "jsonrpc": "2.0",
  "id": 5,
  "method": "tools/call",
  "params": {
    "name": "get_health_topic",
    "arguments": {
      "topic": "asthma"
    }
  }
}
```

### 4. Ping

Simple keepalive/connectivity test.

**Request**:
```json
{
  "jsonrpc": "2.0",
  "id": 6,
  "method": "ping",
  "params": {}
}
```

**Response**:
```json
{
  "jsonrpc": "2.0",
  "id": 6,
  "result": {}
}
```

## Available Tools

### 1. get_organisation_types

Get all NHS organisation types.

**Arguments**: None

**Example**:
```bash
curl -X POST http://localhost:7071/mcp \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 1,
    "method": "tools/call",
    "params": {
   "name": "get_organisation_types",
      "arguments": {}
    }
  }'
```

### 2. convert_postcode_to_coordinates

Convert UK postcode to coordinates.

**Arguments**:
- `postcode` (string, required): UK postcode

**Example**:
```bash
curl -X POST http://localhost:7071/mcp \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 2,
    "method": "tools/call",
    "params": {
      "name": "convert_postcode_to_coordinates",
      "arguments": {
        "postcode": "SW1A 1AA"
      }
    }
  }'
```

### 3. search_organisations_by_postcode

Search NHS organisations near a postcode.

**Arguments**:
- `organisationType` (string, required): Organisation type code (e.g., "PHA", "GPB")
- `postcode` (string, required): UK postcode
- `maxResults` (integer, optional): Maximum results (default: 10)

**Example**:
```bash
curl -X POST http://localhost:7071/mcp \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 3,
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

### 4. search_organisations_by_coordinates

Search NHS organisations near coordinates.

**Arguments**:
- `organisationType` (string, required): Organisation type code
- `latitude` (number, required): Latitude coordinate
- `longitude` (number, required): Longitude coordinate
- `maxResults` (integer, optional): Maximum results (default: 10)

**Example**:
```bash
curl -X POST http://localhost:7071/mcp \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
  "id": 4,
    "method": "tools/call",
    "params": {
      "name": "search_organisations_by_coordinates",
    "arguments": {
    "organisationType": "GPB",
        "latitude": 51.5074,
   "longitude": -0.1278,
   "maxResults": 10
    }
    }
  }'
```

### 5. get_health_topic

Get detailed NHS health information.

**Arguments**:
- `topic` (string, required): Health topic slug (e.g., "asthma", "diabetes")

**Example**:
```bash
curl -X POST http://localhost:7071/mcp \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 5,
    "method": "tools/call",
    "params": {
      "name": "get_health_topic",
      "arguments": {
        "topic": "asthma"
      }
    }
  }'
```

## Error Handling

### JSON-RPC Error Response

```json
{
  "jsonrpc": "2.0",
"id": 1,
  "error": {
    "code": -32603,
    "message": "Internal error: Postcode 'INVALID' not found"
  }
}
```

### Common Error Codes

| Code | Meaning |
|------|---------|
| -32700 | Parse error - Invalid JSON |
| -32600 | Invalid Request - JSON-RPC format error |
| -32601 | Method not found |
| -32602 | Invalid params |
| -32603 | Internal error |

### Tool Execution Errors

When a tool fails, the response includes `isError: true`:

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

## Client Integration Examples

### Python Client

```python
import requests
import json

class McpClient:
    def __init__(self, base_url):
 self.base_url = base_url
      self.request_id = 0
    
    def call(self, method, params=None):
        self.request_id += 1
        payload = {
            "jsonrpc": "2.0",
  "id": self.request_id,
          "method": method,
  "params": params or {}
        }
      
        response = requests.post(
    f"{self.base_url}/mcp",
     json=payload,
    headers={"Content-Type": "application/json"}
    )
        
        return response.json()
    
  def initialize(self):
        return self.call("initialize", {
       "protocolVersion": "2024-11-05",
         "clientInfo": {"name": "python-client", "version": "1.0.0"}
        })
    
    def list_tools(self):
        return self.call("tools/list")
    
    def call_tool(self, name, arguments):
        return self.call("tools/call", {
"name": name,
        "arguments": arguments
})

# Usage
client = McpClient("http://localhost:7071")
client.initialize()
tools = client.list_tools()
result = client.call_tool("search_organisations_by_postcode", {
    "organisationType": "PHA",
"postcode": "SW1A 1AA",
   "maxResults": 5
})
print(result)
```

### JavaScript/TypeScript Client

```typescript
class McpClient {
  private baseUrl: string;
  private requestId: number = 0;

  constructor(baseUrl: string) {
this.baseUrl = baseUrl;
  }

  async call(method: string, params: any = {}) {
    this.requestId++;
    
    const response = await fetch(`${this.baseUrl}/mcp`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        jsonrpc: '2.0',
        id: this.requestId,
method,
        params
      })
    });
  
    return response.json();
  }

  async initialize() {
    return this.call('initialize', {
      protocolVersion: '2024-11-05',
      clientInfo: { name: 'js-client', version: '1.0.0' }
    });
  }

  async listTools() {
    return this.call('tools/list');
  }

  async callTool(name: string, arguments: any) {
    return this.call('tools/call', { name, arguments });
  }
}

// Usage
const client = new McpClient('http://localhost:7071');
await client.initialize();
const tools = await client.listTools();
const result = await client.callTool('get_health_topic', { topic: 'asthma' });
console.log(result);
```

### C# Client

```csharp
using System.Net.Http.Json;
using System.Text.Json;

public class McpClient
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;
    private int _requestId = 0;

    public McpClient(string baseUrl)
    {
        _baseUrl = baseUrl;
   _http = new HttpClient();
    }

    private async Task<JsonDocument> CallAsync(string method, object? @params = null)
    {
   _requestId++;
        var request = new
  {
            jsonrpc = "2.0",
   id = _requestId,
       method,
            @params = @params ?? new { }
        };

        var response = await _http.PostAsJsonAsync($"{_baseUrl}/mcp", request);
        var json = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(json);
    }

    public Task<JsonDocument> InitializeAsync() => CallAsync("initialize", new
    {
        protocolVersion = "2024-11-05",
        clientInfo = new { name = "csharp-client", version = "1.0.0" }
    });

    public Task<JsonDocument> ListToolsAsync() => CallAsync("tools/list");

    public Task<JsonDocument> CallToolAsync(string name, object arguments) =>
   CallAsync("tools/call", new { name, arguments });
}

// Usage
var client = new McpClient("http://localhost:7071");
await client.InitializeAsync();
var tools = await client.ListToolsAsync();
var result = await client.CallToolAsync("search_organisations_by_postcode", new
{
    organisationType = "PHA",
    postcode = "SW1A 1AA",
    maxResults = 5
});
```

## Testing

### Using curl

```bash
# Test the full MCP workflow
BASE_URL="http://localhost:7071"

# 1. Initialize
curl -X POST $BASE_URL/mcp \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05"}}'

# 2. List tools
curl -X POST $BASE_URL/mcp \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","id":2,"method":"tools/list","params":{}}'

# 3. Call a tool
curl -X POST $BASE_URL/mcp \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"get_organisation_types","arguments":{}}}'
```

## MCP Specification Compliance

This implementation complies with:
- MCP Protocol Version: `2024-11-05`
- JSON-RPC 2.0 Specification
- MCP Tool Calling Specification

## Comparison: SSE vs JSON-RPC

### SSE Endpoints (Still Available)
- **Route**: `/mcp/tools/*`
- **Format**: Server-Sent Events
- **Use Case**: Browser-based streaming, progressive UIs
- **Response**: Streaming events with progressive data

### JSON-RPC Endpoint (New)
- **Route**: `/mcp`
- **Format**: JSON-RPC 2.0
- **Use Case**: Native MCP clients, programmatic access
- **Response**: Single JSON-RPC response

Both endpoints are available. Choose based on your needs:
- Use **JSON-RPC** for MCP client integration
- Use **SSE** for web UIs with streaming requirements

## Resources

- [MCP Specification](https://spec.modelcontextprotocol.io/)
- [JSON-RPC 2.0 Specification](https://www.jsonrpc.org/specification)
- [MCP GitHub Repository](https://github.com/modelcontextprotocol)

---

**Built with native MCP protocol support** ??
