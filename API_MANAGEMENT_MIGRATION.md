# API Management Migration

## Changes Made

The NHS Organizations MCP Server has been updated to use Azure API Management instead of direct Azure Cognitive Search.

### What Changed

#### 1. **Endpoints**

**Before** (Azure Cognitive Search):
- Postcode: `https://{searchname}.search.windows.net/indexes/{index}/docs/search`
- Organization Search: `https://{searchname}.search.windows.net/indexes/{index}/docs/search`

**After** (API Management):
- Postcode: `https://nhsuk-apim-int-uks.azure-api.net/service-search/postcodesandplaces/{postcode}?api-version=2`
- Organization Search: `https://nhsuk-apim-int-uks.azure-api.net/service-search/search?api-version=2`

#### 2. **Authentication**

**Before**:
- Header: `api-key: {azure-search-api-key}`

**After**:
- Header: `subscription-key: {api-management-subscription-key}`
- Header: `Content-Type: application/json`

#### 3. **Request Methods**

**Before**:
- Postcode: POST with JSON body containing search query
- Organization: POST with JSON body containing search query

**After**:
- Postcode: GET with postcode in URL path
- Organization: POST with JSON body (same as before)

#### 4. **Response Format**

**Before**:
- Results in `value` array
- Used Azure Search field names

**After**:
- Postcode returns direct object (not in `value` array)
- Organization search still returns `value` array
- Same field names preserved

### Files Updated

#### .NET Version (`/Users/robsinclair/NHSOrgsMCP`)

1. **Services/AzureSearchService.cs**
   - Changed header from `api-key` to `subscription-key`
   - Updated postcode endpoint to GET `/postcodesandplaces/{postcode}`
   - Updated organization search endpoint to `/search`
   - Removed index names from URLs
   - Updated error messages to reference "API Management"

2. **appsettings.json**
   - Changed `Endpoint` to `https://nhsuk-apim-int-uks.azure-api.net/service-search`
   - Updated `PostcodeIndex` to `postcodesandplaces`
   - Updated `ServiceSearchIndex` to `service-search`
   - Changed comment for `ApiKey` to indicate it's a subscription key

3. **README.md**
   - Updated description to mention API Management
   - Updated configuration examples with new endpoint
   - Updated architecture section

#### Python Version (`/Users/robsinclair/NHSUKMCP-Python`)

1. **nhs_orgs_mcp/azure_search.py**
   - Changed header from `api-key` to `subscription-key`
   - Updated default endpoint to API Management URL
   - Changed postcode search from POST to GET
   - Updated postcode URL to `/postcodesandplaces/{postcode}`
   - Updated organization search URL to `/search`
   - Added HTTP 404 handling for postcode not found
   - Updated comments and docstrings

### Environment Variables

No change to variable names (for backwards compatibility), but values change:

```bash
# Before
export AZURE_SEARCH_ENDPOINT="https://nhsuksearchintuks.search.windows.net"
export AZURE_SEARCH_API_KEY="azure-search-api-key"
export AZURE_SEARCH_POSTCODE_INDEX="postcodesandplaces-1-0-b-int"
export AZURE_SEARCH_SERVICE_INDEX="service-search-internal-3-11"

# After
export AZURE_SEARCH_ENDPOINT="https://nhsuk-apim-int-uks.azure-api.net/service-search"
export AZURE_SEARCH_API_KEY="api-management-subscription-key"
export AZURE_SEARCH_POSTCODE_INDEX="postcodesandplaces"  # Not used in API URLs
export AZURE_SEARCH_SERVICE_INDEX="service-search"       # Not used in API URLs
```

**Note**: The index environment variables are kept for backwards compatibility but are no longer used in the actual API URLs since API Management abstracts this.

### Testing Required

Before deployment, test with your actual API Management subscription key:

#### .NET Version

```bash
cd /Users/robsinclair/NHSOrgsMCP

# Set environment variables
export AZURE_SEARCH_ENDPOINT="https://nhsuk-apim-int-uks.azure-api.net/service-search"
export AZURE_SEARCH_API_KEY="your-subscription-key"
export AZURE_SEARCH_POSTCODE_INDEX="postcodesandplaces"
export AZURE_SEARCH_SERVICE_INDEX="service-search"

# Build
dotnet build -c Release

# Test locally
dotnet run
```

#### Python Version

```bash
cd /Users/robsinclair/NHSUKMCP-Python

# Set environment variables
export AZURE_SEARCH_ENDPOINT="https://nhsuk-apim-int-uks.azure-api.net/service-search"
export AZURE_SEARCH_API_KEY="your-subscription-key"

# Test locally
python -m nhs_orgs_mcp.server
```

### Azure Container Apps Deployment

Update environment variables in the container app:

```bash
az containerapp update \
  -n nhs-orgs-mcp \
  -g rg-nhsorgsmcp \
  --set-env-vars \
    AZURE_SEARCH_ENDPOINT="https://nhsuk-apim-int-uks.azure-api.net/service-search" \
    AZURE_SEARCH_API_KEY="your-subscription-key" \
    AZURE_SEARCH_POSTCODE_INDEX="postcodesandplaces" \
    AZURE_SEARCH_SERVICE_INDEX="service-search" \
    MCP_CLOUD_MODE=true
```

### Rollback Plan

If issues occur, revert to Azure Cognitive Search:

1. Restore previous version of files from git
2. Update environment variables back to Azure Search values
3. Redeploy

### Benefits of API Management

1. **Centralized Management**: Single point for API governance
2. **Security**: Subscription keys instead of direct search keys
3. **Rate Limiting**: Built-in throttling and quota management
4. **Monitoring**: Better analytics and logging
5. **Versioning**: API version control (currently using v2)
6. **Abstraction**: Backend changes don't affect consumers

## Summary

Both .NET and Python versions have been successfully updated to use Azure API Management. The changes are backwards compatible in terms of environment variable names, making migration easier. Test thoroughly with your subscription key before deploying to production.
