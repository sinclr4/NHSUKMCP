# Quick Start Guide - NHS MCP Azure Functions

Get up and running in 5 minutes!

## Prerequisites

Install these tools:

```bash
# .NET 8 SDK
winget install Microsoft.DotNet.SDK.8

# Azure Functions Core Tools
npm install -g azure-functions-core-tools@4 --unsafe-perm true
```

## Local Development

### 1. Clone and Configure

```bash
# Clone repository
git clone https://github.com/sinclr4/NHSUKMCP.git
cd NHSUKMCP

# Create local settings
cat > local.settings.json << 'EOF'
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "API_MANAGEMENT_ENDPOINT": "https://nhsuk-apim-int-uks.azure-api.net/service-search",
    "API_MANAGEMENT_SUBSCRIPTION_KEY": "YOUR_KEY_HERE"
  }
}
EOF
```

### 2. Run Locally

```bash
# Start Functions
func start
```

You should see:
```
Functions:
        GetHealthTopic: [GET,POST] http://localhost:7071/mcp/tools/get_health_topic
     GetOrganisationTypes: [GET,POST] http://localhost:7071/mcp/tools/get_organisation_types
        ListTools: [GET] http://localhost:7071/mcp/tools
  SearchOrganisationsByCoordinates: [GET,POST] http://localhost:7071/mcp/tools/search_organisations_by_coordinates
        SearchOrganisationsByPostcode: [GET,POST] http://localhost:7071/mcp/tools/search_organisations_by_postcode
   ConvertPostcodeToCoordinates: [GET,POST] http://localhost:7071/mcp/tools/convert_postcode_to_coordinates
```

### 3. Test It

```bash
# List all tools
curl http://localhost:7071/mcp/tools | jq .

# Search pharmacies near SW1A 1AA
curl -N "http://localhost:7071/mcp/tools/search_organisations_by_postcode?organisationType=PHA&postcode=SW1A%201AA&maxResults=3"

# Get health information about asthma
curl -N "http://localhost:7071/mcp/tools/get_health_topic?topic=asthma"
```

## Deploy to Azure

### Quick Deploy

```bash
# Make script executable
chmod +x deploy-functions.sh

# Deploy (will prompt for API key)
./deploy-functions.sh my-resource-group my-function-app uksouth
```

### Manual Deploy

```bash
# Login to Azure
az login

# Create resource group
az group create --name rg-nhsmcp --location uksouth

# Create storage account
az storage account create \
  --name nhsmcpstorage \
  --resource-group rg-nhsmcp \
  --location uksouth \
  --sku Standard_LRS

# Create function app
az functionapp create \
  --name my-nhsmcp-app \
  --resource-group rg-nhsmcp \
  --consumption-plan-location uksouth \
  --runtime dotnet-isolated \
--runtime-version 8.0 \
  --functions-version 4 \
  --storage-account nhsmcpstorage \
  --os-type Linux

# Configure settings
az functionapp config appsettings set \
  --name my-nhsmcp-app \
  --resource-group rg-nhsmcp \
  --settings \
    API_MANAGEMENT_ENDPOINT="https://nhsuk-apim-int-uks.azure-api.net/service-search" \
    API_MANAGEMENT_SUBSCRIPTION_KEY="your-key-here"

# Deploy
func azure functionapp publish my-nhsmcp-app
```

### Test Deployed App

```bash
# Your function URL
APP_URL="https://my-nhsmcp-app.azurewebsites.net"

# Test it
curl "$APP_URL/mcp/tools" | jq .
```

## Common Issues

### Issue: "Invalid combination of TargetFramework and AzureFunctionsVersion"

**Solution**: Make sure `NHSUKMCP.csproj` has:
```xml
<TargetFramework>net8.0</TargetFramework>
<AzureFunctionsVersion>v4</AzureFunctionsVersion>
```

### Issue: "API Management service not configured"

**Solution**: Set your subscription key in `local.settings.json`:
```json
"API_MANAGEMENT_SUBSCRIPTION_KEY": "your-actual-key"
```

### Issue: "func command not found"

**Solution**: Install Azure Functions Core Tools:
```bash
npm install -g azure-functions-core-tools@4 --unsafe-perm true
```

### Issue: Cold start delays in Azure

**Solution**: Consider Premium Plan for production:
```bash
az functionapp plan create \
  --name premium-plan \
  --resource-group rg-nhsmcp \
  --location uksouth \
  --sku EP1
```

## Next Steps

- **Read full docs**: [README.md](README.md)
- **Understand migration**: [MIGRATION_GUIDE.md](MIGRATION_GUIDE.md)
- **Deep dive**: [README_AZURE_FUNCTIONS.md](README_AZURE_FUNCTIONS.md)
- **Run tests**: `./test-functions.sh` or `.\test-functions.ps1`

## Available Tools

| Tool | Description |
|------|-------------|
| `get_organisation_types` | List all NHS organisation types |
| `convert_postcode_to_coordinates` | Convert UK postcode to lat/long |
| `search_organisations_by_postcode` | Find NHS orgs near a postcode |
| `search_organisations_by_coordinates` | Find NHS orgs near coordinates |
| `get_health_topic` | Get NHS health information |

## Example Requests

### Find Nearby Pharmacies

```bash
curl -N "http://localhost:7071/mcp/tools/search_organisations_by_postcode" \
  -H "Content-Type: application/json" \
  -d '{
  "organisationType": "PHA",
    "postcode": "SW1A 1AA",
    "maxResults": 5
  }'
```

### Get Health Information

```bash
curl -N "http://localhost:7071/mcp/tools/get_health_topic?topic=diabetes"
```

### Find Hospitals Near London

```bash
curl -N "http://localhost:7071/mcp/tools/search_organisations_by_coordinates" \
  -H "Content-Type: application/json" \
  -d '{
    "organisationType": "HOS",
    "latitude": 51.5074,
    "longitude": -0.1278,
  "maxResults": 5
  }'
```

## Understanding SSE Responses

Responses use Server-Sent Events format:

```
event: metadata
data: {"postcode":"SW1A 1AA","organisationType":"PHA"}

event: organisation
data: {"organisationName":"Boots Pharmacy","distance":0.5}

event: organisation
data: {"organisationName":"Lloyds Pharmacy","distance":0.8}

event: complete
data: {"success":true,"resultCount":2}
```

Parse in JavaScript:
```javascript
const eventSource = new EventSource(url);
eventSource.addEventListener('organisation', (e) => {
  const org = JSON.parse(e.data);
  console.log(org.organisationName);
});
```

## Support

- **Issues**: [GitHub Issues](https://github.com/sinclr4/NHSUKMCP/issues)
- **Docs**: See README files in repository
- **Azure Functions Docs**: https://docs.microsoft.com/azure/azure-functions/

---

**You're ready to go! ??**
