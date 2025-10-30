# Python Project Update Instructions

## Changes Required for /Users/robsinclair/NHSUKMCP-Python

### 1. Update `nhs_orgs_mcp/server.py`

Find the section that reads environment variables (around line 20-30):

**OLD:**
```python
# Configuration from environment variables
API_ENDPOINT = os.getenv("AZURE_SEARCH_ENDPOINT", "https://nhsuk-apim-int-uks.azure-api.net/service-search")
SUBSCRIPTION_KEY = os.getenv("AZURE_SEARCH_API_KEY", "")
```

**NEW:**
```python
# Configuration from environment variables
API_ENDPOINT = os.getenv("API_MANAGEMENT_ENDPOINT", "https://nhsuk-apim-int-uks.azure-api.net/service-search")
SUBSCRIPTION_KEY = os.getenv("API_MANAGEMENT_SUBSCRIPTION_KEY", "")
```

### 2. Update README.md

Find the Claude Desktop configuration section:

**OLD:**
```json
{
  "mcpServers": {
    "nhs-organizations": {
      "command": "python",
      "args": ["-m", "nhs_orgs_mcp.server"],
      "env": {
        "AZURE_SEARCH_ENDPOINT": "https://nhsuk-apim-int-uks.azure-api.net/service-search",
        "AZURE_SEARCH_API_KEY": "your-subscription-key-here"
      }
    }
  }
}
```

**NEW:**
```json
{
  "mcpServers": {
    "nhs-organizations": {
      "command": "python",
      "args": ["-m", "nhs_orgs_mcp.server"],
      "env": {
        "API_MANAGEMENT_ENDPOINT": "https://nhsuk-apim-int-uks.azure-api.net/service-search",
        "API_MANAGEMENT_SUBSCRIPTION_KEY": "your-subscription-key-here"
      }
    }
  }
}
```

### 3. Update any other documentation files

Search for these environment variable names and replace them:
- `AZURE_SEARCH_ENDPOINT` → `API_MANAGEMENT_ENDPOINT`
- `AZURE_SEARCH_API_KEY` → `API_MANAGEMENT_SUBSCRIPTION_KEY`

Remove any references to:
- `AZURE_SEARCH_POSTCODE_INDEX`
- `AZURE_SEARCH_SERVICE_INDEX`

These index names are no longer needed as they're hardcoded in the API Management endpoints.

## Rationale

This change:
1. Makes it clear that we're using API Management, not direct Azure Search
2. Removes unnecessary index name configuration (hardcoded in API Management)
3. Standardizes naming across both .NET and Python projects
4. Reduces configuration complexity for end users
