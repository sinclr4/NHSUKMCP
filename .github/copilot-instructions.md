# NHS Organisations MCP Server

This is a .NET MCP (Model Context Protocol) server that enables searching for NHS organizations by type and location.

## Project Features
- Search NHS organizations by type (CCG, CLI, DEN, GPB, etc.)
- Convert postcodes to latitude/longitude using Azure Search
- Find organizations based on geographic proximity
- Full MCP protocol implementation

## Architecture
- .NET 8 console application
- MCP protocol implementation
- Azure Cognitive Search integration
- JSON-based configuration and responses

## Development Guidelines
- Follow MCP protocol specifications
- Use async/await for all HTTP operations
- Implement proper error handling for Azure Search calls
- Validate input parameters before processing
- Use structured logging for debugging