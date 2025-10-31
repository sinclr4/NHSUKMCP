# MCP Inspector Troubleshooting Guide

## Quick Diagnosis

If the MCP Inspector isn't working, follow these steps in order:

### Step 1: Verify Azure Functions is Running

```powershell
# Start the Functions host
cd C:\Users\robsinclair\Source\Repos\NHSUKMCP
func start
```

**Expected output:**
```
Functions:
        Mcp: [POST] http://localhost:7071/api/mcp
```

? **Success**: You should see the `Mcp` function listed
? **Problem**: If you don't see it, rebuild the project first:

```powershell
dotnet build
func start
```

### Step 2: Test the Endpoint Directly

Before using the Inspector, verify the endpoint works:

```powershell
# Test with curl (Windows PowerShell)
curl.exe -X POST http://localhost:7071/api/mcp `
  -H "Content-Type: application/json" `
  -d '{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"initialize\",\"params\":{\"protocolVersion\":\"2024-11-05\"}}'
```

**Expected response:**
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
      "tools": {}
    }
  }
}
```

? **Success**: You got a valid JSON-RPC response
? **Problem**: See error messages below

### Step 3: Launch MCP Inspector Correctly

**IMPORTANT**: The Inspector needs the **full API path** including `/api`:

```bash
# ? CORRECT
npx @modelcontextprotocol/inspector http://localhost:7071/api/mcp

# ? WRONG (missing /api)
npx @modelcontextprotocol/inspector http://localhost:7071/mcp
```

### Step 4: Use the Inspector UI

1. **Web UI opens** at http://localhost:5173 (or similar)
2. **Check connection status** in the UI
3. **Click "Tools" tab** to see available tools
4. **Select a tool** and fill in parameters
5. **Click "Call Tool"** to test

---

## Common Problems & Solutions

### Problem 1: "Cannot connect to server"

**Symptoms:**
- Inspector shows connection error
- Web UI says "Failed to connect"

**Solutions:**

1. **Verify the Functions host is running**:
   ```powershell
   # Check if process is running
   Get-Process -Name "func" -ErrorAction SilentlyContinue
   ```

2. **Check the port is listening**:
   ```powershell
   netstat -ano | findstr :7071
   ```

3. **Ensure you're using the correct URL**:
   - ? `http://localhost:7071/api/mcp`
   - ? `http://localhost:7071/mcp` (missing /api)

4. **Test with curl first** (see Step 2 above)

### Problem 2: "404 Not Found"

**Symptoms:**
- HTTP 404 error
- "Endpoint not found"

**Solutions:**

1. **Missing `/api` prefix**:
   - Azure Functions adds `/api` by default
   - Always use: `http://localhost:7071/api/mcp`

2. **Check `host.json` doesn't override routing**:
   ```json
   {
     "version": "2.0",
     "extensions": {
       "http": {
         "routePrefix": "api"  // Should be "api" or ""
       }
     }
   }
   ```

3. **Verify function route** in `McpJsonRpcFunctions.cs`:
   ```csharp
   [Function("Mcp")]
   public async Task<HttpResponseData> Mcp(
       [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "mcp")] HttpRequestData req)
   ```
   - Full URL becomes: `{host}/api/{Route}` = `http://localhost:7071/api/mcp`

### Problem 3: "Tools not showing in Inspector"

**Symptoms:**
- Inspector connects but shows 0 tools
- Empty tools list

**Solutions:**

1. **Test tools/list directly**:
   ```powershell
   curl.exe -X POST http://localhost:7071/api/mcp `
     -H "Content-Type: application/json" `
     -d '{\"jsonrpc\":\"2.0\",\"id\":2,\"method\":\"tools/list\",\"params\":{}}'
   ```

2. **Check tool registration** in `Program.cs`:
   ```csharp
   services.AddMcpServer(options => { ... })
  .WithTools<NHSOrganisationSearchTools>()
       .WithTools<NHSHealthContentTools>();
   ```

3. **Rebuild and restart**:
   ```powershell
   dotnet clean
   dotnet build
   func start
   ```

### Problem 4: "API Management errors"

**Symptoms:**
- Tools execute but return errors about API keys
- "Subscription key required" errors

**Solutions:**

1. **Check `local.settings.json`**:
   ```json
   {
     "Values": {
       "API_MANAGEMENT_SUBSCRIPTION_KEY": "your-actual-key-here"
     }
   }
   ```

2. **Set the key temporarily**:
   ```powershell
   $env:API_MANAGEMENT_SUBSCRIPTION_KEY = "your-key-here"
   func start
   ```

3. **Get your actual subscription key** from Azure Portal:
   - Go to your API Management instance
   - Navigate to "Subscriptions"
   - Copy the primary or secondary key

### Problem 5: "Protocol version mismatch"

**Symptoms:**
- Inspector shows version error
- "Unsupported protocol version"

**Solutions:**

The server must return `"2024-11-05"`. This is already configured in your code:

```csharp
// In McpJsonRpcFunctions.cs
"initialize" => new
{
    protocolVersion = "2024-11-05",  // This is correct
    serverInfo = new { ... }
}
```

If you still see errors, restart both the Functions host and Inspector.

### Problem 6: Inspector installs but won't start

**Symptoms:**
- `npx` command fails
- Package errors

**Solutions:**

1. **Install globally first**:
   ```bash
   npm install -g @modelcontextprotocol/inspector
   ```

2. **Use the installed version**:
   ```bash
   mcp-inspector http://localhost:7071/api/mcp
   ```

3. **Or use npx with version**:
   ```bash
   npx @modelcontextprotocol/inspector@latest http://localhost:7071/api/mcp
   ```

4. **Check Node.js version**:
   ```bash
   node --version  # Should be 18+
   ```

---

## Complete Testing Workflow

Here's a complete workflow to verify everything works:

### Terminal 1: Start Functions

```powershell
cd C:\Users\robsinclair\Source\Repos\NHSUKMCP

# Set API key if not in local.settings.json
$env:API_MANAGEMENT_SUBSCRIPTION_KEY = "your-key-here"

# Start Functions
func start
```

**Wait for**: "Host lock lease acquired by instance ID"

### Terminal 2: Test Endpoint

```powershell
# Test initialize
curl.exe -X POST http://localhost:7071/api/mcp `
  -H "Content-Type: application/json" `
  -d '{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"initialize\",\"params\":{\"protocolVersion\":\"2024-11-05\"}}'

# Test tools/list
curl.exe -X POST http://localhost:7071/api/mcp `
  -H "Content-Type: application/json" `
  -d '{\"jsonrpc\":\"2.0\",\"id\":2,\"method\":\"tools/list\",\"params\":{}}'

# Test a tool call
curl.exe -X POST http://localhost:7071/api/mcp `
  -H "Content-Type: application/json" `
  -d '{\"jsonrpc\":\"2.0\",\"id\":3,\"method\":\"tools/call\",\"params\":{\"name\":\"get_organisation_types\",\"arguments\":{}}}'
```

? **All three should return valid JSON responses**

### Terminal 3: Launch Inspector

```bash
# Launch Inspector
npx @modelcontextprotocol/inspector http://localhost:7071/api/mcp
```

**Expected**:
- Web browser opens to http://localhost:5173
- Inspector UI loads
- Server connection shows green/connected

### In Inspector UI:

1. **Server Info tab**: Should show server name and version
2. **Tools tab**: Should list 5 tools
3. **Test a tool**:
 - Select `get_organisation_types`
   - Click "Call Tool"
   - See response with org types

---

## Debugging with Logs

### Enable verbose logging in Functions

Edit `host.json`:

```json
{
  "version": "2.0",
  "logging": {
    "logLevel": {
      "default": "Information",
      "NHSUKMCP.Functions": "Debug"
  }
  }
}
```

Restart Functions to see detailed logs.

### Check Functions output

Watch for these log messages:

```
? Good signs:
- "MCP JSON-RPC request received"
- "Handling method: initialize"
- "Handling method: tools/list"
- "Handling method: tools/call"

? Bad signs:
- "Parse error"
- "Method not found"
- "Invalid tool call parameters"
- Any stack traces
```

### Use Postman for testing

If Inspector still doesn't work, use Postman:

1. **Create a POST request** to `http://localhost:7071/api/mcp`
2. **Set header**: `Content-Type: application/json`
3. **Body (raw JSON)**:
   ```json
   {
     "jsonrpc": "2.0",
     "id": 1,
 "method": "initialize",
     "params": {
       "protocolVersion": "2024-11-05"
     }
   }
   ```
4. **Send** and verify response

---

## Alternative: Use our test scripts

We have PowerShell test scripts that verify everything:

```powershell
# Run the JSON-RPC test script
.\test-mcp-jsonrpc.ps1
```

This script:
- Checks if Functions is running
- Tests initialize, tools/list, and tools/call
- Shows formatted responses
- Reports success/failure

---

## Still having issues?

If none of the above work:

1. **Share the error message**:
   - Copy exact error from Inspector
   - Copy Functions console output
   - Include curl test results

2. **Check versions**:
   ```powershell
   node --version     # Should be 18+
 func --version        # Should be 4.x
   dotnet --version      # Should be 8.0.x
   ```

3. **Try the simple test endpoint**:
   Create a test function to verify routing works:

   ```csharp
   [Function("TestPing")]
   public HttpResponseData TestPing(
       [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "ping")] HttpRequestData req)
   {
       var response = req.CreateResponse(HttpStatusCode.OK);
       response.WriteString("pong");
       return response;
   }
   ```

   Test: `curl.exe http://localhost:7071/api/ping`

---

## Quick Reference: Inspector Commands

```bash
# Install
npm install -g @modelcontextprotocol/inspector

# Launch with HTTP
npx @modelcontextprotocol/inspector http://localhost:7071/api/mcp

# Launch with verbose logging
npx @modelcontextprotocol/inspector --verbose http://localhost:7071/api/mcp

# Launch with custom port
npx @modelcontextprotocol/inspector --port 3000 http://localhost:7071/api/mcp

# Update
npm update -g @modelcontextprotocol/inspector

# Check version
npm list -g @modelcontextprotocol/inspector
```

---

## Summary Checklist

Before using Inspector:

- [ ] Functions host is running (`func start`)
- [ ] Can see "Mcp" function in startup output
- [ ] `curl` test to `/api/mcp` works
- [ ] `local.settings.json` has API key
- [ ] Node.js 18+ is installed
- [ ] Using correct URL: `http://localhost:7071/api/mcp` (with `/api`)

When launching Inspector:

- [ ] Use full URL with `/api` prefix
- [ ] Wait for browser to open
- [ ] Check connection status in UI
- [ ] Look for errors in Inspector console (F12)

If problems persist:

- [ ] Try curl tests first
- [ ] Check Functions logs for errors
- [ ] Rebuild project (`dotnet clean && dotnet build`)
- [ ] Try our test scripts
- [ ] Use Postman as alternative
- [ ] Ask for help with specific error messages

---

**Most Common Fix**: Use `http://localhost:7071/api/mcp` (not `http://localhost:7071/mcp`)

The `/api` prefix is added by Azure Functions by default! ??
