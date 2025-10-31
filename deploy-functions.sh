#!/bin/bash

# Deploy NHS MCP Server to Azure Functions
# Usage: ./deploy-functions.sh <resource-group> <function-app-name> <location>

set -e

RESOURCE_GROUP=${1:-"rg-nhsmcp-functions"}
FUNCTION_APP_NAME=${2:-"nhsmcp-functions"}
LOCATION=${3:-"uksouth"}
STORAGE_ACCOUNT="${FUNCTION_APP_NAME}storage"
API_MANAGEMENT_ENDPOINT="https://nhsuk-apim-int-uks.azure-api.net/service-search"

echo "=================================================="
echo "NHS MCP Server - Azure Functions Deployment"
echo "=================================================="
echo "Resource Group: $RESOURCE_GROUP"
echo "Function App: $FUNCTION_APP_NAME"
echo "Location: $LOCATION"
echo "Storage: $STORAGE_ACCOUNT"
echo "=================================================="
echo ""

# Check if Azure CLI is installed
if ! command -v az &> /dev/null; then
 echo "Error: Azure CLI is not installed"
    echo "Install from: https://docs.microsoft.com/cli/azure/install-azure-cli"
    exit 1
fi

# Check if logged in
echo "Checking Azure login status..."
if ! az account show &> /dev/null; then
    echo "Not logged in. Logging in to Azure..."
    az login
fi

SUBSCRIPTION_NAME=$(az account show --query name -o tsv)
echo "Using subscription: $SUBSCRIPTION_NAME"
echo ""

# Create resource group
echo "Creating resource group: $RESOURCE_GROUP"
az group create --name "$RESOURCE_GROUP" --location "$LOCATION" -o none
echo "? Resource group created"
echo ""

# Create storage account
echo "Creating storage account: $STORAGE_ACCOUNT"
# Remove hyphens for storage account name (must be alphanumeric)
STORAGE_ACCOUNT_CLEAN=$(echo "$STORAGE_ACCOUNT" | tr -d '-')
az storage account create \
  --name "$STORAGE_ACCOUNT_CLEAN" \
  --resource-group "$RESOURCE_GROUP" \
  --location "$LOCATION" \
  --sku Standard_LRS \
  -o none
echo "? Storage account created"
echo ""

# Create function app
echo "Creating function app: $FUNCTION_APP_NAME"
az functionapp create \
  --name "$FUNCTION_APP_NAME" \
--resource-group "$RESOURCE_GROUP" \
  --consumption-plan-location "$LOCATION" \
  --runtime dotnet-isolated \
  --runtime-version 9.0 \
  --functions-version 4 \
  --storage-account "$STORAGE_ACCOUNT_CLEAN" \
  --os-type Linux \
  -o none
echo "? Function app created"
echo ""

# Prompt for API Management subscription key
read -p "Enter API Management Subscription Key: " API_KEY
echo ""

# Configure app settings
echo "Configuring application settings..."
az functionapp config appsettings set \
  --name "$FUNCTION_APP_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --settings \
    "API_MANAGEMENT_ENDPOINT=$API_MANAGEMENT_ENDPOINT" \
"API_MANAGEMENT_SUBSCRIPTION_KEY=$API_KEY" \
  -o none
echo "? Application settings configured"
echo ""

# Enable Application Insights
echo "Enabling Application Insights..."
az monitor app-insights component create \
  --app "$FUNCTION_APP_NAME-insights" \
  --location "$LOCATION" \
  --resource-group "$RESOURCE_GROUP" \
  -o none || true

INSTRUMENTATION_KEY=$(az monitor app-insights component show \
  --app "$FUNCTION_APP_NAME-insights" \
  --resource-group "$RESOURCE_GROUP" \
  --query instrumentationKey -o tsv)

az functionapp config appsettings set \
  --name "$FUNCTION_APP_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --settings "APPINSIGHTS_INSTRUMENTATIONKEY=$INSTRUMENTATION_KEY" \
  -o none
echo "? Application Insights enabled"
echo ""

# Deploy function app
echo "Building and deploying function app..."
if ! command -v func &> /dev/null; then
    echo "Error: Azure Functions Core Tools not installed"
    echo "Install from: https://docs.microsoft.com/azure/azure-functions/functions-run-local"
    exit 1
fi

func azure functionapp publish "$FUNCTION_APP_NAME"
echo "? Function app deployed"
echo ""

# Get function app URL
FUNCTION_URL=$(az functionapp show \
  --name "$FUNCTION_APP_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --query defaultHostName -o tsv)

echo "=================================================="
echo "Deployment Complete!"
echo "=================================================="
echo ""
echo "Function App URL: https://$FUNCTION_URL"
echo ""
echo "Available Endpoints:"
echo "  • Tools List: https://$FUNCTION_URL/mcp/tools"
echo "  • Get Org Types: https://$FUNCTION_URL/mcp/tools/get_organisation_types"
echo "  • Convert Postcode: https://$FUNCTION_URL/mcp/tools/convert_postcode_to_coordinates"
echo "  • Search by Postcode: https://$FUNCTION_URL/mcp/tools/search_organisations_by_postcode"
echo "  • Search by Coords: https://$FUNCTION_URL/mcp/tools/search_organisations_by_coordinates"
echo "  • Health Topic: https://$FUNCTION_URL/mcp/tools/get_health_topic"
echo ""
echo "Test with:"
echo "  curl https://$FUNCTION_URL/mcp/tools"
echo ""
echo "=================================================="
