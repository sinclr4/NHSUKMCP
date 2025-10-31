# MCP Inspector - Quick Help Guide

## The Problem

The MCP Inspector isn't connecting or showing tools? You're probably hitting one of these common issues:

### Most Common Issue: Wrong URL or Functions Not Running ?

**Correct URL**: `http://localhost:7071/api/mcp` ?

Azure Functions adds `/api` by default - always include it!

---

## Quick Fix Steps

### Step 1: Run the Diagnostic

```powershell
.\diagnose-mcp-inspector.ps1
```

This script checks:
- ? Is Functions running?
- ? Is port 7071 listening?
- ? Is Node.js installed?
- ? Does the MCP endpoint respond?
- ? Are tools registered?
- ? Is MCP Inspector installed?

### Step 2: If Functions isn't running

```powershell
# Start Functions
func start
```

Wait for: "Host lock lease acquired by instance ID" and check that the `Mcp` function is listed.

### Step 3: Test the endpoint directly

```powershell
.\test-mcp-endpoint.ps1
```

This confirms the MCP endpoint is responding correctly.

### Step 4: Launch Inspector

```bash
npx @modelcontextprotocol/inspector http://localhost:7071/api/mcp
```

### Step 5: Use the Inspector UI

1. Browser opens at http://localhost:5173
2. Click "Tools" tab
3. Should see 5 NHS tools listed
4. Select a tool, fill parameters, click "Call Tool"

---

## Alternative: One-Click Launch (Windows)

```cmd
launch-inspector.bat
```

This script:
- Starts Functions if not running
- Tests the endpoint
- Launches Inspector automatically

---

## Common Error Messages

### "Cannot connect to server"

**Cause**: Functions not running  
**Fix**: 
```powershell
func start
# Then in another terminal:
npx @modelcontextprotocol/inspector http://localhost:7071/api/mcp
```

### "Port 7071 is already in use"

**Cause**: Previous Functions instance still running or another app using the port  
**Fix**:
```powershell
# Quick fix - run the port clearing script
.\clear-port-7071.ps1

# Or use the complete cleanup
.\cleanup-all.ps1

# Then start Functions
func start
```

**See**: [CLEARING_PORTS.md](CLEARING_PORTS.md) for detailed help

### "Port 6277 is already in use"

**Cause**: MCP Inspector or another Node.js process still running  
**Fix**:
```powershell
# Clear the specific port
.\clear-port.ps1 6277

# Or use complete cleanup
.\cleanup-all.ps1
```

**See**: [CLEARING_PORTS.md](CLEARING_PORTS.md) for detailed help

### "404 Not Found"

**Cause**: Wrong URL or Functions not started correctly  
**Fix**: Verify Functions started successfully with `func start` and use correct URL: `http://localhost:7071/api/mcp`

### "Tools list is empty"

**Cause**: Tool registration issue  
**Fix**:
```powershell
dotnet clean
dotnet build
func start
```

### "API Management errors"

**Cause**: Missing or invalid subscription key  
**Fix**: Update `local.settings.json`:
```json
{
  "Values": {
    "API_MANAGEMENT_SUBSCRIPTION_KEY": "your-real-key-here"
  }
}
```

---

## Testing Without Inspector

If Inspector won't work, test with curl:

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

Or use our test script:
```powershell
.\test-mcp-jsonrpc.ps1 "http://localhost:7071"
```

---

## Checklist Before Using Inspector

Before launching Inspector, verify:

- [ ] Functions is running (`func start`)
- [ ] Can see "Mcp" function in startup logs
- [ ] `.\test-mcp-endpoint.ps1` passes
- [ ] Node.js 18+ installed (`node --version`)
- [ ] Using correct URL: `http://localhost:7071/api/mcp`

---

## Get More Help

| Issue | Resource |
|-------|----------|
| Inspector won't connect | [MCP_INSPECTOR_TROUBLESHOOTING.md](MCP_INSPECTOR_TROUBLESHOOTING.md) |
| Want full testing guide | [TESTING_WITH_MCP_INSPECTOR.md](TESTING_WITH_MCP_INSPECTOR.md) |
| JSON-RPC protocol help | [MCP_JSON_RPC_GUIDE.md](MCP_JSON_RPC_GUIDE.md) |
| Functions deployment | [README_AZURE_FUNCTIONS.md](README_AZURE_FUNCTIONS.md) |

---

## Scripts Summary

| Script | Purpose |
|--------|---------|
| `diagnose-mcp-inspector.ps1` | ? Full diagnostic check |
| `test-mcp-endpoint.ps1` | ? Quick endpoint test |
| `launch-inspector.bat` | ?? One-click launch (Windows) |
| `test-mcp-jsonrpc.ps1` | ?? Test JSON-RPC protocol |

---

## The Golden Rule

**Always use**: `http://localhost:7071/api/mcp`

This is the only endpoint available. Azure Functions includes `/api` by default.

---

## Still Stuck?

1. Run: `.\diagnose-mcp-inspector.ps1`
2. Read the error messages
3. Follow the suggested fixes
4. If still stuck, check [MCP_INSPECTOR_TROUBLESHOOTING.md](MCP_INSPECTOR_TROUBLESHOOTING.md)

**Need to verify tools work?** Use `.\test-mcp-jsonrpc.ps1` to test without the Inspector.
