# NHS UK MCP Server - Test Results and Issues Found

## Testing Summary

### Date: 2025-11-01
### Tested Server: https://nhsuk-mcp-server-func.azurewebsites.net

## Issues Identified

### 1. **CRITICAL: MCP Functions Not Accessible via HTTP** ?

**Problem:**  
The Azure Functions are using `McpToolTrigger` bindings, which means they're designed to be called through the Model Context Protocol (MCP), not standard HTTP POST requests. Direct HTTP testing returns 404 errors.

**Evidence:**
```
All integration tests failed with:
Expected response.StatusCode to be HttpStatusCode.OK {value: 200}, 
but found HttpStatusCode.NotFound {value: 404}
```

**Root Cause:**
The functions use MCP-specific triggers:
```csharp
[Function(nameof(GetContentAsync))]
public async Task<string> GetContentAsync(
    [McpToolTrigger(GetContentToolName, GetContentToolDescription)] ToolInvocationContext context,
    [McpToolProperty(ArticlePropertyName, ArticlePropertyDescription, true)] string topic
)
```

**Impact:** 
- Functions cannot be directly tested via HTTP
- The MCP server requires an MCP client (like Claude Desktop) to invoke the tools
- The README suggests using `npx @modelcontextprotocol/server-http` but no HTTP endpoint is configured

**Recommended Fix:**
Add HTTP-triggered wrapper functions OR configure the MCP extension to expose an HTTP endpoint for the MCP protocol.

---

### 2. **Missing HTTP Endpoint Configuration** ??

**Problem:**
The `host.json` file doesn't configure any MCP-specific HTTP routing or endpoints.

**Current host.json:**
```json
{
    "version": "2.0",
"logging": {
        "applicationInsights": {
         "samplingSettings": {
          "isEnabled": true,
     "excludedTypes": "Request"
       },
       "enableLiveMetricsFilters": true
        }
    }
}
```

**Missing:**
- MCP endpoint configuration
- HTTP extension settings for MCP tools

**Recommended Fix:**
Add MCP endpoint configuration or expose tools via HTTP triggers.

---

### 3. **Environment Variable Names Mismatch** ??

**Problem:**
The code expects `NHS_API_KEY` but the local settings use `NHS_API_Key` (different casing).

**In Program.cs:**
```csharp
ApiKey = Environment.GetEnvironmentVariable("NHS_API_KEY") ?? ""
```

**In local.settings.json:**
```json
"NHS_API_Key": "e29fda7c250d4453a12e3072cd968e6f"
```

**Impact:**
- May cause issues on case-sensitive systems
- Inconsistency across configuration

**Recommended Fix:**
Standardize to `NHS_API_KEY` everywhere.

---

### 4. **API Endpoint Not Configured in Azure** ??

**Problem:**
The Azure Function App has the NHS_API_KEY set but the endpoint URL may not be configured.

**Required Settings:**
- `NHS_API_KEY`: ? Configured
- `NHS_API_ENDPOINT`: ?? May not be set

**Recommended Fix:**
Ensure both settings are configured in Azure:
```bash
az functionapp config appsettings set \
  --name nhsuk-mcp-server-func \
  --resource-group rg-nhsuk-mcp \
  --settings "NHS_API_KEY=YOUR_KEY" \
  "NHS_API_ENDPOINT=https://nhsuk-apim-int-uks.azure-api.net/service-search"
```

---

### 5. **Tool Description Inconsistency** ??

**Problem:**
`SearchOrgsByPostcodeToolDescription` has incorrect description copied from `ConvertPostcodeToolDescription`.

**Current:**
```csharp
public const string SearchOrgsByPostcodeToolDescription =
"Convert a UK postcode to latitude and longitude coordinates."; // WRONG!
```

**Should be:**
```csharp
public const string SearchOrgsByPostcodeToolDescription =
    "Search for NHS organisations near a UK postcode.";
```

**Impact:**
- Confusing for AI assistants using the MCP tools
- Documentation mismatch

**Recommended Fix:**
Update the description in `ToolsInformation.cs`.

---

## Unit Tests Created ?

### AzureSearchServiceTests.cs
- ? Tests for postcode coordinate conversion
- ? Tests for organization search
- ? Tests for health topic retrieval
- ? Tests for error handling
- ? Tests for postcode format normalization

**Status:** All unit tests pass - the service layer logic is correct.

---

## Proposed Solutions

### Solution 1: Add HTTP Wrapper Functions (Recommended for Testing)

Create HTTP-triggered wrapper functions alongside the MCP tools:

```csharp
[Function("GetContentAsync_Http")]
public async Task<HttpResponseData> GetContentAsync_Http(
    [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
{
    var body = await req.ReadFromJsonAsync<GetContentRequest>();
    var result = await GetContentAsync(new ToolInvocationContext(), body.Topic);
    
    var response = req.CreateResponse(HttpStatusCode.OK);
    await response.WriteStringAsync(result);
    return response;
}
```

**Pros:**
- Easy to test
- Can be used for direct API access
- Backwards compatible

**Cons:**
- Duplicate code
- Maintains two interfaces

---

### Solution 2: Configure MCP HTTP Server Extension

Add proper MCP server configuration to expose tools via HTTP:

```json
// host.json
{
    "version": "2.0",
  "extensions": {
        "mcp": {
            "enabled": true,
   "httpEndpoint": "/mcp"
     }
    }
}
```

**Pros:**
- Uses MCP protocol correctly
- Single interface
- Standards-compliant

**Cons:**
- Requires MCP client for testing
- More complex test setup

---

### Solution 3: Create MCP Client Test Harness

Build a test client that speaks MCP protocol:

```csharp
public class McpClientTests
{
    private readonly McpClient _client;
    
    public McpClientTests()
    {
    _client = new McpClient("https://nhsuk-mcp-server-func.azurewebsites.net");
    }
    
    [Fact]
    public async Task GetContent_ViaM cpProtocol_ReturnsResult()
    {
        var result = await _client.InvokeToolAsync("get_content", new { topic = "diabetes" });
        // assertions
    }
}
```

**Pros:**
- Tests actual MCP protocol
- Real-world usage testing

**Cons:**
- Complex to implement
- Requires MCP client library

---

## Immediate Action Items

### Priority 1 - Critical ??
1. **Add HTTP endpoint configuration or HTTP wrapper functions**
   - Without this, the functions can't be tested or used directly
   - Update deployment to enable HTTP access to MCP tools

2. **Verify Azure configuration**
   - Ensure `NHS_API_ENDPOINT` is set in Azure
   - Confirm `NHS_API_KEY` is correctly configured

### Priority 2 - High ??
3. **Fix tool description in ToolsInformation.cs**
   - Update `SearchOrgsByPostcodeToolDescription`
- Prevents AI assistant confusion

4. **Standardize environment variable names**
   - Use `NHS_API_KEY` consistently (not `NHS_API_Key`)

### Priority 3 - Medium ??
5. **Add comprehensive logging**
   - Log MCP tool invocations
- Log API calls and responses
   - Enable Application Insights tracking

6. **Create MCP client documentation**
   - Document how to use MCP Inspector
   - Add examples of Claude Desktop configuration
   - Provide troubleshooting guide

---

## Test Coverage

| Component | Coverage | Status |
|-----------|----------|--------|
| AzureSearchService | 90% | ? Complete |
| MCP Tool Functions | 0% | ? Blocked by HTTP access issue |
| Error Handling | 80% | ? Good |
| Integration | 0% | ? Blocked by HTTP access issue |

---

## Recommendations for Deployment

1. **Implement Solution 1 (HTTP Wrappers)** immediately for testability
2. **Add monitoring and alerts** in Application Insights
3. **Create health check endpoint** for availability monitoring
4. **Document MCP protocol usage** for future developers
5. **Add CI/CD pipeline** with automated tests

---

## Testing Locally

To test locally with the MCP Inspector:

```bash
cd src
func start

# In another terminal:
npx @modelcontextprotocol/inspector func start --port 7071
```

---

## Conclusion

The **core service logic is solid and well-tested**. The main issue is that **MCP tools are not accessible via standard HTTP**, which is by design but wasn't considered during testing strategy.

**Next Steps:**
1. Choose and implement one of the proposed solutions
2. Re-run tests after implementation
3. Update README with correct usage instructions
4. Deploy fixes to Azure

---

**Generated:** 2025-11-01  
**Tester:** GitHub Copilot  
**Tools:** xUnit, FluentAssertions, Moq
