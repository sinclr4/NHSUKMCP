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

## ?? Adding to Claude Desktop

To use this MCP server with Claude Desktop, you need to configure it in Claude's configuration file.

### Windows Configuration

1. Locate your Claude Desktop config file at:
   ```
   %APPDATA%\Claude\claude_desktop_config.json
   ```

2. Add the NHS UK MCP Server configuration:

```json
{
  "mcpServers": {
    "nhs-uk": {
   "command": "func",
      "args": [
        "start",
        "--port",
   "7071"
      ],
      "cwd": "C:\\Users\\YourUsername\\Source\\Repos\\NHSUKMCP\\src",
      "env": {
        "NHS_API_KEY": "YOUR_NHS_API_KEY_HERE"
      }
    }
  }
}
```

### macOS/Linux Configuration

1. Locate your Claude Desktop config file at:
   ```
   ~/Library/Application Support/Claude/claude_desktop_config.json
   ```

2. Add the NHS UK MCP Server configuration:

```json
{
  "mcpServers": {
    "nhs-uk": {
      "command": "func",
      "args": [
        "start",
     "--port",
        "7071"
      ],
      "cwd": "/path/to/NHSUKMCP/src",
 "env": {
        "NHS_API_KEY": "YOUR_NHS_API_KEY_HERE"
  }
    }
  }
}
```

### 3. Restart Claude Desktop

After saving the configuration file, completely restart Claude Desktop for the changes to take effect.

### 4. Verify Connection

In Claude Desktop, you should see the MCP server connection indicator. You can test it by asking:

> "Can you get me information about diabetes from the NHS?"

## ?? Testing the Server

You can test the MCP server tools directly using Claude or by making HTTP requests:

### Example: Get Health Content

```bash
curl -X POST http://localhost:7071/api/GetContentAsync \
  -H "Content-Type: application/json" \
  -d '{"topic": "diabetes"}'
```

### Example: Search for Pharmacies

```bash
curl -X POST http://localhost:7071/api/SearchOrgsByPostcode \
  -H "Content-Type: application/json" \
  -d '{
 "postcode": "SW1A 1AA",
  "organisationType": "PHA",
    "maxResults": 5
  }'
```

## ??? Project Structure

```
NHSUKMCP/
??? src/
?   ??? NHSUKMCPServer.csproj    # Project file
?   ??? NHSUKMCPServer.sln       # Solution file
?   ??? Program.cs      # Application entry point
?   ??? host.json        # Azure Functions host configuration
?   ??? local.settings.json       # Local environment settings
?   ??? Models/
?   ?   ??? Models.cs  # Data models
?   ??? Services/
?   ?   ??? APIEndpoints.cs    # NHS API service
?   ??? Tools/
?       ??? NHSHealthContentTools.cs      # Health content MCP tools
?       ??? NHSOrganisationTools.cs       # Organisation search MCP tools
?       ??? ToolsInformation.cs   # Tool metadata
??? README.md
```

## ?? Development

### Building

```bash
cd src
dotnet build
```

### Running Locally

```bash
cd src
func start
```

### Debugging in Visual Studio

1. Open `src/NHSUKMCPServer.sln` in Visual Studio
2. Press F5 to start debugging
3. The Azure Functions host will start with breakpoint support

## ?? Dependencies

- **.NET 8**: Target framework
- **Azure Functions Worker v4**: Serverless hosting
- **Microsoft.Azure.Functions.Worker.Extensions.Mcp**: MCP protocol support
- **Azure Search Service**: NHS UK API integration
- **Application Insights**: Telemetry and monitoring

## ?? Security Notes

- **Never commit your NHS API key** to version control
- Use User Secrets or environment variables for sensitive data
- The `local.settings.json` file is excluded from git by default
- For production deployments, use Azure Key Vault or similar secret management

## ?? API Key Setup

1. Register at [NHS UK Developer Portal](https://developer.api.nhs.uk/)
2. Create an application to receive your API key
3. Add the key to your `local.settings.json` or environment variables
4. The API provides access to NHS health content and organisation search

## ?? Contributing

Contributions are welcome! Please:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## ?? License

This project uses NHS UK public APIs. Please review the [NHS UK API terms of use](https://developer.api.nhs.uk/terms).

## ?? Troubleshooting

### Server won't start
- Ensure .NET 8 SDK is installed: `dotnet --version`
- Verify Azure Functions Core Tools: `func --version`
- Check if port 7071 is available: `netstat -ano | findstr :7071`

### Claude can't connect
- Verify the `cwd` path in `claude_desktop_config.json` is correct
- Ensure the server is running (`func start`)
- Check Claude Desktop logs for connection errors
- Restart Claude Desktop after configuration changes

### API returns errors
- Verify your NHS API key is valid
- Check the NHS UK API status page
- Review Application Insights logs if configured

## ?? Resources

- [Model Context Protocol Documentation](https://modelcontextprotocol.io/)
- [NHS UK API Documentation](https://developer.api.nhs.uk/)
- [Azure Functions Documentation](https://docs.microsoft.com/azure/azure-functions/)
- [.NET 8 Documentation](https://docs.microsoft.com/dotnet/)

## ?? Support

For issues and questions:
- Open an issue on [GitHub](https://github.com/sinclr4/NHSUKMCP/issues)
- Check existing issues for solutions
- Review the troubleshooting section above

---

Built with ?? for the NHS and AI-powered healthcare information access
