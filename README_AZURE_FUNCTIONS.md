# NHS Organisations MCP Server - Azure Functions

This project has been converted to an **Azure Functions** application that provides Model Context Protocol (MCP) tools as streamable HTTP endpoints.

## Overview

The NHS Organisations MCP Server enables searching for NHS organizations by type and location, as well as retrieving health information from the NHS API. All tools are available as Server-Sent Events (SSE) HTTP endpoints.

## Features

- **Organisation Search**: Find NHS organizations by postcode or coordinates
- **Organisation Types**: Get all available NHS organization types
- **Postcode Conversion**: Convert UK postcodes to latitude/longitude
- **Health Information**: Retrieve detailed NHS health topic information
- **Streamable Responses**: All endpoints use Server-Sent Events (SSE) for progressive data delivery
- **MCP Tool Discovery**: `/mcp/tools` endpoint lists all available tools with schemas

## Architecture

- **.NET 9**: Core runtime
- **Azure Functions v4**: Isolated worker model
- **Azure API Management**: Backend for NHS data
- **Server-Sent Events**: Streaming responses for all tools

## Configuration

### Environment Variables

Set these in Azure Functions configuration or `local.settings.json`:

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

## API Endpoints

### Tool Discovery

**GET** `/mcp/tools`

Returns all available MCP tools with their input schemas.

```bash
curl https://<your-function-app>.azurewebsites.net/mcp/tools
```

Response:
```json
{
  "serverInfo": {
    "name": "nhs-uk-mcp-server",
    "version": "1.0.0",
    "description": "NHS UK Model Context Protocol Server"
  },
  "tools": [
    {
      "name": "get_organisation_types",
    "description": "Get a list of all available NHS organisation types",
      "inputSchema": { ... }
    },
    ...
  ]
}
```

### Get Organisation Types

**GET/POST** `/mcp/tools/get_organisation_types`

Returns all available NHS organisation types.

```bash
curl https://<your-function-app>.azurewebsites.net/mcp/tools/get_organisation_types
```

SSE Response:
```
event: data
data: {"success":true,"organisationTypes":{"CCG":"Clinical Commissioning Group",...}}

event: complete
data: {"success":true}
```

### Convert Postcode to Coordinates

**GET/POST** `/mcp/tools/convert_postcode_to_coordinates`

Converts a UK postcode to latitude/longitude coordinates.

```bash
# GET
curl "https://<your-function-app>.azurewebsites.net/mcp/tools/convert_postcode_to_coordinates?postcode=SW1A%201AA"

# POST
curl -X POST https://<your-function-app>.azurewebsites.net/mcp/tools/convert_postcode_to_coordinates \
  -H "Content-Type: application/json" \
  -d '{"postcode":"SW1A 1AA"}'
```

SSE Response:
```
event: data
data: {"success":true,"postcode":"SW1A 1AA","coordinates":{"latitude":51.5014,"longitude":-0.1419}}

event: complete
data: {"success":true}
```

### Search Organisations by Postcode

**GET/POST** `/mcp/tools/search_organisations_by_postcode`

Searches for NHS organisations near a postcode. Results are streamed.

```bash
# GET
curl "https://<your-function-app>.azurewebsites.net/mcp/tools/search_organisations_by_postcode?organisationType=PHA&postcode=SW1A%201AA&maxResults=5"

# POST
curl -X POST https://<your-function-app>.azurewebsites.net/mcp/tools/search_organisations_by_postcode \
  -H "Content-Type: application/json" \
-d '{"organisationType":"PHA","postcode":"SW1A 1AA","maxResults":5}'
```

SSE Response:
```
event: metadata
data: {"postcode":"SW1A 1AA","organisationType":"PHA","coordinates":{"latitude":51.5014,"longitude":-0.1419}}

event: organisation
data: {"organisationName":"Boots Pharmacy","odsCode":"FA123",...}

event: organisation
data: {"organisationName":"Lloyds Pharmacy","odsCode":"FA456",...}

event: complete
data: {"success":true,"resultCount":5}
```

### Search Organisations by Coordinates

**GET/POST** `/mcp/tools/search_organisations_by_coordinates`

Searches for NHS organisations near specific coordinates. Results are streamed.

```bash
# GET
curl "https://<your-function-app>.azurewebsites.net/mcp/tools/search_organisations_by_coordinates?organisationType=GPB&latitude=51.5074&longitude=-0.1278&maxResults=10"

# POST
curl -X POST https://<your-function-app>.azurewebsites.net/mcp/tools/search_organisations_by_coordinates \
  -H "Content-Type: application/json" \
  -d '{"organisationType":"GPB","latitude":51.5074,"longitude":-0.1278,"maxResults":10}'
```

SSE Response:
```
event: metadata
data: {"organisationType":"GPB","coordinates":{"latitude":51.5074,"longitude":-0.1278}}

event: organisation
data: {"organisationName":"NHS GP Practice","odsCode":"G12345",...}

event: complete
data: {"success":true,"resultCount":10}
```

### Get Health Topic

**GET/POST** `/mcp/tools/get_health_topic`

Retrieves detailed information about a health condition from NHS API. Sections are streamed progressively.

```bash
# GET
curl "https://<your-function-app>.azurewebsites.net/mcp/tools/get_health_topic?topic=asthma"

# POST
curl -X POST https://<your-function-app>.azurewebsites.net/mcp/tools/get_health_topic \
  -H "Content-Type: application/json" \
  -d '{"topic":"asthma"}'
```

SSE Response:
```
event: metadata
data: {"name":"Asthma","description":"Asthma is a common lung condition...","sectionCount":5}

event: section
data: {"index":0,"headline":"Overview","text":"Asthma is a condition..."}

event: section
data: {"index":1,"headline":"Symptoms","text":"The main symptoms..."}

event: complete
data: {"success":true}
```

## Organisation Types

Available organisation types include:

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

Use `GET /mcp/tools/get_organisation_types` to get the complete list.

## Development

### Local Development

1. **Install Azure Functions Core Tools**:
   ```bash
 npm install -g azure-functions-core-tools@4
   ```

2. **Configure local settings**:
   Create `local.settings.json`:
   ```json
   {
  "IsEncrypted": false,
     "Values": {
       "AzureWebJobsStorage": "UseDevelopmentStorage=true",
       "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
  "API_MANAGEMENT_ENDPOINT": "https://nhsuk-apim-int-uks.azure-api.net/service-search",
       "API_MANAGEMENT_SUBSCRIPTION_KEY": "your-key-here"
     }
   }
   ```

3. **Run locally**:
   ```bash
   func start
   ```

4. **Test endpoints**:
   ```bash
   curl http://localhost:7071/mcp/tools
   ```

### Building

```bash
dotnet build
```

### Deployment to Azure

#### Using Azure CLI

```bash
# Create resource group
az group create --name rg-nhsmcp --location uksouth

# Create storage account
az storage account create \
  --name nhsmcpfuncstorage \
  --resource-group rg-nhsmcp \
  --location uksouth \
  --sku Standard_LRS

# Create function app
az functionapp create \
  --name nhsmcp-functions \
  --resource-group rg-nhsmcp \
  --consumption-plan-location uksouth \
  --runtime dotnet-isolated \
  --runtime-version 9.0 \
  --functions-version 4 \
  --storage-account nhsmcpfuncstorage

# Configure app settings
az functionapp config appsettings set \
  --name nhsmcp-functions \
  --resource-group rg-nhsmcp \
  --settings \
    API_MANAGEMENT_ENDPOINT="https://nhsuk-apim-int-uks.azure-api.net/service-search" \
    API_MANAGEMENT_SUBSCRIPTION_KEY="your-subscription-key"

# Deploy
func azure functionapp publish nhsmcp-functions
```

#### Using Visual Studio

1. Right-click project ? Publish
2. Choose Azure Functions
3. Configure settings
4. Publish

## Testing with Server-Sent Events

### Using curl

```bash
curl -N "http://localhost:7071/mcp/tools/get_health_topic?topic=diabetes"
```

### Using JavaScript

```javascript
const eventSource = new EventSource('https://your-app.azurewebsites.net/mcp/tools/get_health_topic?topic=asthma');

eventSource.addEventListener('metadata', (e) => {
  const data = JSON.parse(e.data);
  console.log('Metadata:', data);
});

eventSource.addEventListener('section', (e) => {
  const data = JSON.parse(e.data);
  console.log('Section:', data);
});

eventSource.addEventListener('complete', (e) => {
  console.log('Complete');
  eventSource.close();
});

eventSource.addEventListener('error', (e) => {
  const data = JSON.parse(e.data);
  console.error('Error:', data);
  eventSource.close();
});
```

### Using Python

```python
import requests

response = requests.get(
    'http://localhost:7071/mcp/tools/search_organisations_by_postcode',
    params={'organisationType': 'PHA', 'postcode': 'SW1A 1AA', 'maxResults': 5},
    stream=True
)

for line in response.iter_lines():
    if line:
        decoded_line = line.decode('utf-8')
    if decoded_line.startswith('event:'):
 event = decoded_line.split(': ')[1]
            print(f"Event: {event}")
        elif decoded_line.startswith('data:'):
            data = decoded_line.split(': ', 1)[1]
    print(f"Data: {data}")
```

## Error Handling

All endpoints return errors in SSE format:

```
event: error
data: {"success":false,"error":"Error message"}
```

Common errors:
- Missing required parameters
- Invalid organisation type
- Postcode not found
- Health topic not found
- API Management service not configured

## Project Structure

```
NHSUKMCP/
??? Functions/
?   ??? McpFunctions.cs# All Azure Functions endpoints
??? Models/
?   ??? Models.cs # Data models
??? Services/
?   ??? AzureSearchService.cs   # API Management integration
??? Program.cs       # Azure Functions host configuration
??? host.json       # Functions host configuration
??? local.settings.json         # Local development settings
??? NHSUKMCP.csproj     # Project file
??? README_AZURE_FUNCTIONS.md  # This file
```

## Monitoring

Azure Functions provides built-in monitoring via Application Insights:

- Function execution logs
- Performance metrics
- Failure tracking
- Custom telemetry

Access via Azure Portal ? Your Function App ? Monitoring

## Performance Considerations

- SSE streaming allows progressive data delivery
- Each organization/section is sent immediately when available
- Small delay (50ms) between items for demonstration (can be removed)
- Azure Functions scales automatically based on load

## Security

- Use Azure AD authentication for production deployments
- Store API Management subscription key in Azure Key Vault
- Use managed identities where possible
- Enable HTTPS only

## License

See LICENSE file for details.

## Support

For issues or questions, please open an issue on the GitHub repository.
