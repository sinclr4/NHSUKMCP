# Azure Functions Migration Guide

This document explains the migration of NHS Organisations MCP Server from a console application to Azure Functions.

## Overview

The project has been converted from:
- **.NET Console Application** with MCP stdio protocol
- **To Azure Functions** with HTTP/SSE endpoints

## What Changed

### 1. Project Type

**Before**:
- Console application (`OutputType: Exe`)
- MCP Server with stdio transport
- Target Framework: .NET 9

**After**:
- Azure Functions application
- HTTP endpoints with Server-Sent Events
- Target Framework: .NET 8 (Azure Functions requirement)

### 2. Dependencies

**Removed**:
```xml
<PackageReference Include="ModelContextProtocol" Version="0.4.0-preview.3" />
<FrameworkReference Include="Microsoft.AspNetCore.App" />
```

**Added**:
```xml
<PackageReference Include="Microsoft.Azure.Functions.Worker" Version="1.23.0" />
<PackageReference Include="Microsoft.Azure.Functions.Worker.Extensions.Http" Version="3.2.0" />
<PackageReference Include="Microsoft.Azure.Functions.Worker.Sdk" Version="1.18.0" />
<PackageReference Include="Microsoft.ApplicationInsights.WorkerService" Version="2.22.0" />
<PackageReference Include="Microsoft.Azure.Functions.Worker.ApplicationInsights" Version="1.4.0" />
```

### 3. File Structure

**Removed Files**:
- `Tools/NHSOrganisationSearchTools.cs` (MCP tool definitions)
- `Tools/NHSHealthContentTools.cs` (MCP tool definitions)
- `HealthEndpointHostedService.cs` (hosted service)

**Added Files**:
- `Functions/McpFunctions.cs` (Azure Functions endpoints)
- `host.json` (Functions host configuration)
- `local.settings.json` (local development settings)
- `deploy-functions.sh` (deployment script)
- `test-functions.sh` (Bash test script)
- `test-functions.ps1` (PowerShell test script)
- `README_AZURE_FUNCTIONS.md` (detailed documentation)

**Modified Files**:
- `Program.cs` (from console host to Functions host)
- `NHSUKMCP.csproj` (project type and dependencies)
- `README.md` (updated documentation)

### 4. Endpoint Changes

**Before** (MCP stdio):
```
Tools available via stdin/stdout JSON-RPC protocol
```

**After** (HTTP/SSE):
```
GET/POST /mcp/tools     - List all tools
GET/POST /mcp/tools/get_organisation_types            - Get org types
GET/POST /mcp/tools/convert_postcode_to_coordinates     - Convert postcode
GET/POST /mcp/tools/search_organisations_by_postcode    - Search by postcode
GET/POST /mcp/tools/search_organisations_by_coordinates - Search by coords
GET/POST /mcp/tools/get_health_topic  - Get health info
```

### 5. Response Format

**Before** (JSON-RPC):
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": { ... }
}
```

**After** (Server-Sent Events):
```
event: metadata
data: {...}

event: organisation
data: {...}

event: complete
data: {"success":true}
```

### 6. Configuration

**Before** (Environment Variables):
```bash
AZURE_SEARCH_ENDPOINT
AZURE_SEARCH_API_KEY
MCP_CLOUD_MODE
CONTAINER_APP_NAME
```

**After** (Environment Variables):
```bash
API_MANAGEMENT_ENDPOINT
API_MANAGEMENT_SUBSCRIPTION_KEY
FUNCTIONS_WORKER_RUNTIME=dotnet-isolated
AzureWebJobsStorage
```

## Migration Benefits

### 1. HTTP Accessibility
- Tools accessible via standard HTTP requests
- No need for MCP client libraries
- Can be called from any HTTP client (curl, Postman, browsers)

### 2. Streaming Responses
- Server-Sent Events for progressive data delivery
- Better user experience for large result sets
- Real-time progress indication

### 3. Scalability
- Azure Functions automatic scaling
- Pay-per-execution pricing model
- Built-in load balancing

### 4. Tool Discovery
- `/mcp/tools` endpoint lists all available tools
- Self-documenting API with input schemas
- No need for external documentation to know available tools

### 5. Flexibility
- Both GET and POST methods supported
- Easy to integrate with web applications
- CORS support for browser-based clients

### 6. Monitoring
- Application Insights integration
- Built-in logging and telemetry
- Performance monitoring and alerts

## Code Architecture

### Program.cs

**Before**:
```csharp
// Complex dual-mode setup (cloud vs local)
if (runInCloudMode) {
    var webBuilder = WebApplication.CreateBuilder(args);
    // Setup ASP.NET Core
} else {
    var builder = Host.CreateApplicationBuilder(args);
    // Setup MCP stdio
}
```

**After**:
```csharp
// Simple Azure Functions host
var host = new HostBuilder()
  .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) => {
   // Configure services
    })
    .Build();
await host.RunAsync();
```

### Functions Implementation

All MCP tools are now implemented as Azure Functions in `Functions/McpFunctions.cs`:

```csharp
[Function("ListTools")]
public HttpResponseData ListTools(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "mcp/tools")] 
HttpRequestData req)
{
    // Return tool schemas
}

[Function("GetOrganisationTypes")]
public async Task<HttpResponseData> GetOrganisationTypes(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", 
     Route = "mcp/tools/get_organisation_types")] 
    HttpRequestData req)
{
    // Stream organisation types via SSE
}

// ... other functions
```

### SSE Streaming Pattern

Each function follows this pattern for streaming:

```csharp
var response = req.CreateResponse(HttpStatusCode.OK);
response.Headers.Add("Content-Type", "text/event-stream");
response.Headers.Add("Cache-Control", "no-cache");
response.Headers.Add("Connection", "keep-alive");

await using var writer = new StreamWriter(response.Body, Encoding.UTF8, leaveOpen: true);

// Send metadata
await writer.WriteLineAsync("event: metadata");
await writer.WriteLineAsync($"data: {json}");
await writer.WriteLineAsync();
await writer.FlushAsync();

// Stream items
foreach (var item in items) {
    await writer.WriteLineAsync("event: item");
    await writer.WriteLineAsync($"data: {JsonSerializer.Serialize(item)}");
    await writer.WriteLineAsync();
    await writer.FlushAsync();
}

// Send completion
await writer.WriteLineAsync("event: complete");
await writer.WriteLineAsync("data: {\"success\":true}");
await writer.WriteLineAsync();
await writer.FlushAsync();
```

## Backward Compatibility

### For Existing Integrations

If you have existing code using the MCP stdio protocol:

1. **Update endpoints**: Change from stdio to HTTP endpoints
2. **Parse SSE**: Implement SSE parsing instead of JSON-RPC
3. **Update configuration**: Use new environment variable names

### Example Client Migration

**Before** (MCP stdio):
```csharp
var client = new McpClient();
var result = await client.CallToolAsync("search_organisations_by_postcode", 
    new { organisationType = "PHA", postcode = "SW1A 1AA" });
```

**After** (HTTP/SSE):
```csharp
var client = new HttpClient();
var response = await client.GetAsync(
  "https://your-app.azurewebsites.net/mcp/tools/search_organisations_by_postcode" +
    "?organisationType=PHA&postcode=SW1A%201AA");

using var stream = await response.Content.ReadAsStreamAsync();
using var reader = new StreamReader(stream);

while (!reader.EndOfStream) {
    var line = await reader.ReadLineAsync();
    if (line?.StartsWith("event:") == true) {
    var eventType = line.Substring(7);
    } else if (line?.StartsWith("data:") == true) {
   var data = JsonSerializer.Deserialize<dynamic>(line.Substring(6));
    }
}
```

## Testing

### Local Testing

```bash
# Start Functions locally
func start

# Test endpoints
curl http://localhost:7071/mcp/tools

# Run test suite
./test-functions.sh
```

### Azure Testing

```bash
# Deploy to Azure
./deploy-functions.sh rg-nhsmcp nhsmcp-functions uksouth

# Test deployed endpoints
./test-functions.sh https://nhsmcp-functions.azurewebsites.net
```

## Deployment Comparison

### Before (Container Apps)

```bash
# Build Docker image
docker build -t nhsorgsmcp:latest .

# Push to ACR
docker push acr.azurecr.io/nhsorgsmcp:latest

# Deploy to Container Apps
az containerapp update --image acr.azurecr.io/nhsorgsmcp:latest
```

### After (Azure Functions)

```bash
# Deploy directly from code
func azure functionapp publish nhsmcp-functions

# Or use deployment script
./deploy-functions.sh rg-nhsmcp nhsmcp-functions uksouth
```

## Performance Considerations

### Streaming Benefits

- **Progressive Loading**: Users see results as they arrive
- **Reduced Memory**: Server doesn't buffer all results
- **Better UX**: Real-time feedback vs waiting for complete response

### Cold Start

Azure Functions may have cold start delays:
- First request after idle may take 2-5 seconds
- Use Premium Plan for always-warm instances if needed
- Consumption Plan is cost-effective for sporadic use

## Rollback Plan

If issues occur, you can:

1. **Keep old code**: The original console app code is in git history
2. **Create separate branch**: Maintain both versions
3. **Use feature flags**: Toggle between endpoints

## Next Steps

### Recommended Enhancements

1. **Authentication**: Add Azure AD authentication
2. **Rate Limiting**: Implement throttling
3. **Caching**: Add Redis cache for frequently accessed data
4. **API Documentation**: Generate OpenAPI/Swagger docs
5. **CI/CD**: Set up GitHub Actions pipeline
6. **Monitoring**: Configure alerts and dashboards

### Future Considerations

- Consider Azure API Management for centralized API governance
- Implement GraphQL endpoint as alternative to REST
- Add WebSocket support for two-way communication
- Create SDKs for common languages (C#, Python, JavaScript)

## Support

For questions or issues with the migration:
- Review the [README](README.md)
- Check [detailed documentation](README_AZURE_FUNCTIONS.md)
- Open an issue on GitHub

## Conclusion

The migration to Azure Functions provides:
- ? Better HTTP accessibility
- ? Progressive streaming responses
- ? Tool discovery endpoint
- ? Automatic scaling
- ? Better monitoring
- ? Simpler deployment

While maintaining all original functionality of searching NHS organisations and retrieving health information.
