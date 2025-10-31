# Azure Search Configuration

To use the postcode and organization search features, you need to configure Azure Search access.

## Environment Variables

Set these environment variables before running the MCP server:

```bash
export AZURE_SEARCH_ENDPOINT="https://nhs-uk-search.search.windows.net"
export AZURE_SEARCH_API_KEY="your-api-key-here"
export AZURE_SEARCH_POSTCODE_INDEX="postcode-index"
export AZURE_SEARCH_SERVICE_INDEX="service-search-index"
```

## Claude Desktop Configuration

Update your `claude_desktop_config.json` to include the environment variables:

```json
{
  "mcpServers": {
    "nhs-organizations-mcp-server": {
      "command": "/Users/robsinclair/NHSOrgsMCP/publish/NHSOrgsMCP",
      "env": {
        "AZURE_SEARCH_ENDPOINT": "https://nhs-uk-search.search.windows.net",
        "AZURE_SEARCH_API_KEY": "your-api-key-here",
        "AZURE_SEARCH_POSTCODE_INDEX": "postcode-index",
        "AZURE_SEARCH_SERVICE_INDEX": "service-search-index"
      }
    }
  }
}
```

## Features Available Without Azure Search

- ✅ `get_organisation_types` - List all NHS organization types (works without Azure Search)

## Features Requiring Azure Search

- ❌ `convert_postcode_to_coordinates` - Convert UK postcode to lat/long
- ❌ `search_organisations_by_postcode` - Search organizations near a postcode
- ❌ `search_organisations_by_coordinates` - Search organizations near coordinates

Without Azure Search configuration, these tools will return a helpful error message.

## Postman Configuration

To connect to this MCP server from Postman, use the local executable with environment variables:

```json
{
  "name": "NHS Organisations MCP",
  "command": "/Users/robsinclair/NHSOrgsMCP/bin/Release/net9.0/NHSOrgsMCP",
  "env": {
    "AZURE_SEARCH_ENDPOINT": "https://nhsuksearchintuks.search.windows.net",
    "AZURE_SEARCH_API_KEY": "your-api-key-here",
    "AZURE_SEARCH_POSTCODE_INDEX": "postcodesandplaces-1-0-b-int",
    "AZURE_SEARCH_SERVICE_INDEX": "service-search-internal-3-11"
  }
}
```

**Note**: Postman's MCP client connects to local processes only. 

### Testing Your Azure Deployment

Your MCP server is deployed at: **https://nhs-orgs-mcp.blackflower-9a1bf005.uksouth.azurecontainerapps.io**

#### Health Endpoints (Available Now)

Test that the container is running:

```bash
# Check health
curl https://nhs-orgs-mcp.blackflower-9a1bf005.uksouth.azurecontainerapps.io/healthz
# Returns: {"status":"ok"}

# Check readiness
curl https://nhs-orgs-mcp.blackflower-9a1bf005.uksouth.azurecontainerapps.io/ready
# Returns: {"status":"ready"}
```

#### Important: Cloud Mode

The deployed container runs in "Cloud Mode" which provides health endpoints only. The MCP stdio server is disabled in Azure because Container Apps don't provide stdin/stdout pipes.

To test MCP functionality:

1. **Use Postman's MCP Client** (local process) - Connect to your local build as shown above
2. **Test via bash scripts** - Use `test_postcode_search.sh`, `test_search_coordinates.sh`, etc.
3. **Claude Desktop** - Connect to local build with Azure Search credentials

#### Future: Add HTTP API

To make MCP tools available via HTTP in Azure, you would need to add REST API endpoints. Contact me if you need this functionality.

---

# Azure Deployment (Container Apps)

This section covers deploying the MCP server as a container to **Azure Container Apps**.

## 1. Build Container Image Locally

The provided `Dockerfile` supports multi-stage build. From repository root:

```bash
docker build -t nhsorgsmcp:latest .
```

## 2. Create Azure Resources

Set shell variables (adjust names):

```bash
RESOURCE_GROUP="rg-nhsorgsmcp"
LOCATION="uksouth"
ACR_NAME="nhsorgsmcpacr" 
CONTAINERAPPS_ENV="nhsorgsmcp-env"
APP_NAME="nhs-orgs-mcp"
IMAGE_TAG="nhsorgsmcp:latest"
```

Login and create resource group:

```bash
az login
az group create --name "$RESOURCE_GROUP" --location "$LOCATION"
```

### 2.1 Create Azure Container Registry (ACR)
```bash
az acr create --name "$ACR_NAME" --resource-group "$RESOURCE_GROUP" --location "$LOCATION" --sku Basic --admin-enabled true
az acr login --name "$ACR_NAME"
```

Tag and push image:
```bash
docker tag "$IMAGE_TAG" "$ACR_NAME.azurecr.io/$IMAGE_TAG"
docker push "$ACR_NAME.azurecr.io/$IMAGE_TAG"
```

### 2.2 Enable Container Apps Extension
```bash
az extension add --name containerapp --upgrade
az provider register --namespace Microsoft.App
az provider register --namespace Microsoft.OperationalInsights
```

### 2.3 Create Container Apps Environment
```bash
az containerapp env create \
  --name "$CONTAINERAPPS_ENV" \
  --resource-group "$RESOURCE_GROUP" \
  --location "$LOCATION"
```

## 3. Deploy the Container App

Provision secrets (Azure Search API key) and app:
```bash
AZURE_SEARCH_ENDPOINT="https://nhsuksearchintuks.search.windows.net"     # your endpoint
AZURE_SEARCH_API_KEY="<your-api-key>"                                   # KEEP SECRET
AZURE_SEARCH_POSTCODE_INDEX="postcodesandplaces-1-0-b-int"              # your postcode index
AZURE_SEARCH_SERVICE_INDEX="service-search-internal-3-11"               # your services index

az containerapp create \
  --name "$APP_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --environment "$CONTAINERAPPS_ENV" \
  --image "$ACR_NAME.azurecr.io/$IMAGE_TAG" \
  --registry-server "$ACR_NAME.azurecr.io" \
  --ingress external --target-port 8080 \
  --min-replicas 1 --max-replicas 1 \
  --env-vars \
    AZURE_SEARCH_ENDPOINT=$AZURE_SEARCH_ENDPOINT \
    AZURE_SEARCH_POSTCODE_INDEX=$AZURE_SEARCH_POSTCODE_INDEX \
    AZURE_SEARCH_SERVICE_INDEX=$AZURE_SEARCH_SERVICE_INDEX \
    LOG_LEVEL=Information \
  --secrets AZURE_SEARCH_API_KEY=$AZURE_SEARCH_API_KEY \
  --secret-env-vars AZURE_SEARCH_API_KEY=AZURE_SEARCH_API_KEY
```

> We mark the API key as a **secret**; referenced through `--secret-env-vars`.

## 4. Health Checks

The container exposes `:8080/healthz` and `:8080/ready`. Azure Container Apps automatically manages basic probing via the ingress port. You can manually verify:

```bash
APP_URL=$(az containerapp show -n "$APP_NAME" -g "$RESOURCE_GROUP" --query properties.configuration.ingress.fqdn -o tsv)
curl https://$APP_URL/healthz
```

## 5. Updating the Image

Rebuild, retag, push, then update the container app:

```bash
docker build -t nhsorgsmcp:latest .
docker tag nhsorgsmcp:latest $ACR_NAME.azurecr.io/nhsorgsmcp:latest
docker push $ACR_NAME.azurecr.io/nhsorgsmcp:latest
az containerapp update -n "$APP_NAME" -g "$RESOURCE_GROUP" --image $ACR_NAME.azurecr.io/nhsorgsmcp:latest
```

## 6. Environment Variables Reference

| Name | Required | Purpose |
|------|----------|---------|
| AZURE_SEARCH_ENDPOINT | Yes | Azure Cognitive Search endpoint URL |
| AZURE_SEARCH_API_KEY | Yes (for search features) | Azure Search API key (set as secret) |
| AZURE_SEARCH_POSTCODE_INDEX | Yes (for postcode conversion) | Name of postcode index |
| AZURE_SEARCH_SERVICE_INDEX | Yes (for organization search) | Name of services index |
| LOG_LEVEL | No (defaults Information) | Adjust runtime logging threshold |
| DOTNET_ENVIRONMENT | No | Set to Production for optimized behavior |

If `AZURE_SEARCH_API_KEY` is missing, only `get_organisation_types` will function.

## 7. Scaling Considerations

- Start with 1 replica; MCP stdio interaction is per process.
- Horizontal scaling may require a **connection distribution layer** in future if multiple MCP clients connect.
- Add resource limits example:
```bash
az containerapp update -n "$APP_NAME" -g "$RESOURCE_GROUP" \
  --cpu 0.5 --memory 1Gi
```

## 8. Observability

- Logs: `az containerapp logs show -n "$APP_NAME" -g "$RESOURCE_GROUP" --follow`
- Consider adding Azure Monitor / Log Analytics workspace for retention.

## 9. Cleanup
```bash
az group delete --name "$RESOURCE_GROUP" --yes --no-wait
```

---

## FAQ

**Why not Azure Web App?** MCP expects stdio transport; Container Apps gives simpler process-level control.

**Can I expose MCP over HTTP?** You’d need a protocol bridge; current implementation remains stdio-only.

**How do I rotate the API key?** Update secret: `az containerapp secret set -n "$APP_NAME" -g "$RESOURCE_GROUP" --secrets AZURE_SEARCH_API_KEY=<new>` then redeploy or restart.

**What about readiness vs liveness?** Both endpoints return static `ok/ready`; extend with internal checks if needed (e.g. warm search). 

