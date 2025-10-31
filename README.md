# NHS Organisations MCP Server

This is a .NET Model Context Protocol (MCP) server that enables searching for NHS organizations by type and location using Azure API Management.

## Features

The server provides the following MCP tools:

### 1. GetOrganisationTypes
- **Description**: Get a list of all available NHS organization types with their descriptions
- **Parameters**: None
- **Returns**: Dictionary of organization type codes and descriptions

### 2. ConvertPostcodeToCoordinates
- **Description**: Convert a UK postcode to latitude and longitude coordinates
- **Parameters**: 
  - `postcode` (string): UK postcode (e.g., 'SW1A 1AA')
- **Returns**: Coordinates for the postcode

### 3. SearchOrganisationsByPostcode
- **Description**: Search for NHS organizations by type and postcode
- **Parameters**:
  - `organizationType` (string): NHS organization type code (e.g., 'PHA', 'GPP', 'HOS')
  - `postcode` (string): UK postcode to search near
  - `maxResults` (int, optional): Maximum number of results (default: 10, max: 50)
- **Returns**: List of NHS organizations near the specified postcode

### 4. SearchOrganisationsByCoordinates
- **Description**: Search for NHS organizations by type and coordinates
- **Parameters**:
  - `organizationType` (string): NHS organization type code
  - `latitude` (double): Latitude coordinate
  - `longitude` (double): Longitude coordinate
  - `maxResults` (int, optional): Maximum number of results (default: 10, max: 50)
- **Returns**: List of NHS organizations near the specified coordinates

## Supported Organisation Types

| Code | Description |
|------|-------------|
| CCG  | Clinical Commissioning Group |
| CLI  | Clinics |
| DEN  | Dentists |
| GDOS | Generic Directory of Services |
| GPB  | GP |
| GPP  | GP Practice |
| GSD  | Generic Service Directory |
| HA   | Health Authority |
| HOS  | Hospital |
| HWB  | Health and Wellbeing Board |
| LA   | Local Authority |
| LAT  | Area Team |
| MIU  | Minor Injury Unit |
| OPT  | Optician |
| PHA  | Pharmacy |
| RAT  | Regional Area Team |
| SCL  | Social Care Provider Location |
| SCP  | Social Care Provider |
| SHA  | Strategic Health Authority |
| STP  | Sustainability and Transformation Partnership |
| TRU  | Trust |
| UC   | Urgent Care |
| UNK  | UNKNOWN |

## Prerequisites

- .NET 9.0 or later
- Azure Search API access with NHS data (optional for some features)

## Features Availability

✅ **Always Available** (no Azure Search required):
- `get_organization_types` - List all NHS organization types

❌ **Requires Azure Search Configuration**:
- `convert_postcode_to_coordinates` - Convert postcode to coordinates
- `search_organizations_by_postcode` - Search organizations near a postcode
- `search_organizations_by_coordinates` - Search organizations by coordinates

See [AZURE_SETUP.md](AZURE_SETUP.md) for Azure Search configuration instructions.

## Setup and Running

### 1. Clone and Build

```bash
git clone <repository-url>
cd NHSOrgsMCP
dotnet restore
dotnet build
```

### 2. Run the Server

```bash
dotnet run
```

The server will start and listen for MCP protocol messages via standard input/output.

### 3. Configure in Claude Desktop

Add this server to your Claude Desktop configuration file (`~/Library/Application Support/Claude/claude_desktop_config.json` on macOS):

**Basic Configuration** (only `get_organization_types` will work):
```json
{
  "mcpServers": {
    "nhs-organizations-mcp-server": {
      "command": "/path/to/NHSOrgsMCP/publish/NHSOrgsMCP"
    }
  }
}
```

**Full Configuration** (with API Management for all features):
```json
{
  "mcpServers": {
    "nhs-organizations-mcp-server": {
      "command": "/path/to/NHSOrgsMCP/publish/NHSOrgsMCP",
      "env": {
        "API_MANAGEMENT_ENDPOINT": "https://nhsuk-apim-int-uks.azure-api.net/service-search",
        "API_MANAGEMENT_SUBSCRIPTION_KEY": "your-subscription-key-here"
      }
    }
  }
}
```

**Important**: Replace `/path/to/NHSOrgsMCP` with the actual absolute path to your project directory.

See [AZURE_SETUP.md](AZURE_SETUP.md) for detailed Azure Search configuration instructions.

## Architecture

The server is built using:

- **.NET 9**: Core runtime and libraries
- **ModelContextProtocol**: MCP SDK for .NET
- **Microsoft.Extensions.Hosting**: Dependency injection and hosting
- **Azure API Management**: Backend service for NHS data search

### Key Components

1. **Models/Models.cs**: Data models for API Management configuration, organization types, and results
2. **Services/AzureSearchService.cs**: Service for interacting with Azure API Management
3. **Tools/NHSOrganisationTools.cs**: MCP tools that expose the search functionality
4. **Program.cs**: Application entry point and DI configuration

## Configuration

The API Management configuration uses environment variables:

- **Endpoint**: <https://nhsuk-apim-int-uks.azure-api.net/service-search>
- **Subscription Key**: Provided via API_MANAGEMENT_SUBSCRIPTION_KEY environment variable
- **Postcode Endpoint**: /postcodesandplaces/?search={postcode}&api-version=2
- **Search Endpoint**: /search?api-version=2

## Usage Examples

### Example 1: Find nearby pharmacies
```
User: "Find pharmacies near postcode SW1A 1AA"
1. Call GetOrganisationTypes() to understand available types
2. Call SearchOrganisationsByPostcode("PHA", "SW1A 1AA", 10)
```

### Example 2: Find hospitals by coordinates
```
User: "Find hospitals near latitude 51.5074, longitude -0.1278"
1. Call SearchOrganisationsByCoordinates("HOS", 51.5074, -0.1278, 5)
```

## Error Handling

The server includes comprehensive error handling:
- Invalid postcode validation
- Organisation type validation
- Coordinate range validation
- Azure Search API error handling
- Structured error responses

## Logging

All operations are logged to stderr using the Microsoft.Extensions.Logging framework, making it compatible with MCP protocol requirements.

## Development

To extend the server:

1. Add new tools to `Tools/NHSOrganisationTools.cs`
2. Implement additional Azure Search functionality in `Services/AzureSearchService.cs`
3. Add new data models to `Models/Models.cs` as needed

The server follows MCP best practices and .NET conventions for maintainable, extensible code.