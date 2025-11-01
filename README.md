# NHS UK MCP Server

A Model Context Protocol (MCP) server that provides access to NHS UK health information and organisation data. Built with .NET 8 and Azure Functions, this server enables AI assistants like Claude to access trusted NHS health content and search for NHS organisations.

## ?? What is this?

This MCP server provides AI assistants with tools to:

- **Get NHS Health Content**: Retrieve trusted health information from NHS.UK on various medical topics
- **Find NHS Organisations**: Search for NHS services like pharmacies, GP surgeries, and hospitals by postcode
- **Convert Postcodes**: Convert UK postcodes to geographic coordinates
- **List Organisation Types**: Get all available NHS organisation types

## ??? Available Tools

### 1. `get_content`
Retrieve NHS health articles by topic.

**Parameters:**
- `topic` (required): The health topic slug (e.g., "diabetes", "covid-19", "mental-health")

**Example Response:**
```json
{
  "name": "Type 2 diabetes",
  "description": "Find out about type 2 diabetes...",
  "url": "https://www.nhs.uk/conditions/type-2-diabetes/",
  "lastReviewed": "2024-01-15",
  "sections": [...]
}
```

### 2. `get_organisation_types`
Get a list of all available NHS organisation types.

**Parameters:** None

**Example Response:**
```json
{
  "PHA": "Pharmacy",
  "GPB": "GP Surgery",
  "HOS": "Hospital",
  "DEN": "Dentist",
  ...
}
```

### 3. `convert_postcode_to_coordinates`
Convert a UK postcode to latitude and longitude coordinates.

**Parameters:**
- `location` (required): UK postcode (e.g., "SW1A 1AA", "M1 1AE")

**Example Response:**
```json
{
  "postcode": "SW1A 1AA",
  "latitude": 51.5014,
  "longitude": -0.1419
}
```

### 4. `search_organisations_by_postcode`
Find NHS organisations near a postcode.

**Parameters:**
- `postcode` (required): UK postcode to search near
- `organisation_type` (required): Type of organisation (use `get_organisation_types` to see valid types)
- `maxResults` (optional): Maximum number of results (default: 10)

**Example Response:**
```json
{
  "postcode": "SW1A 1AA",
  "coordinates": {
    "latitude": 51.5014,
    "longitude": -0.1419
  },
  "organisationType": "PHA",
  "organisations": [...]
}
```

## ?? Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Azure Functions Core Tools](https://docs.microsoft.com/azure/azure-functions/functions-run-local) (v4)
- NHS UK API key (obtain from [NHS UK Developer Portal](https://developer.api.nhs.uk/))

## ?? Installation & Setup

### 1. Clone the Repository

```bash
git clone https://github.com/sinclr4/NHSUKMCP.git
cd NHSUKMCP/src
```

### 2. Configure Environment Variables

Edit `src/local.settings.json` and add your NHS API key:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "NHS_API_Key": "YOUR_NHS_API_KEY_HERE"
  }
}
```

### 3. Build the Project

```bash
cd src
dotnet restore
dotnet build
```

### 4. Run the Server

```bash
func start
```

The server will start on `http://localhost:7071` by default.

## ?? Azure Deployment

The NHS UK MCP Server is deployed to Azure Functions for production use.

### Deployed Function App

- **URL**: `https://nhsuk-mcp-server-func.azurewebsites.net`
- **Resource Group**: `rg-nhsuk-mcp`
- **Region**: UK South
- **Runtime**: .NET 8 (Isolated)

### Deployed Functions

| Function Name | Trigger Type | Description |
|--------------|-------------|-------------|
| `GetContentAsync` | MCP Tool | Retrieve NHS health content |
| `GetOrganisationTypes` | MCP Tool | List organisation types |
| `ConvertPostcodeToCoordinates` | MCP Tool | Convert postcodes |
| `SearchOrgsByPostcode` | MCP Tool | Find NHS organisations |

### Deploying Updates

To deploy updates to Azure:

```bash
cd src

# Build the project
dotnet build --configuration Release

# Publish the project
dotnet publish --configuration Release --output ./publish

# Create deployment package
Compress-Archive -Path .\publish\* -DestinationPath .\deploy.zip -Force

# Deploy to Azure
az functionapp deployment source config-zip `
  --resource-group rg-nhsuk-mcp `
  --name nhsuk-mcp-server-func `
  --src .\deploy.zip
