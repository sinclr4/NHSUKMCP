using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using NHSOrgsMCP.Models;
using NHSOrgsMCP.Services;
using NHSOrgsMCP.Tools;
using NHSOrgsMCP;
using System.Text.Json;

// Check if running in Azure Container Apps (no stdin) or locally (with stdin)
var runInCloudMode = Environment.GetEnvironmentVariable("CONTAINER_APP_NAME") != null ||
                      Environment.GetEnvironmentVariable("MCP_CLOUD_MODE") == "true";

WebApplicationBuilder? webBuilder = null;
IHostApplicationBuilder builder;

if (runInCloudMode)
{
    // Cloud mode: Use ASP.NET Core with HTTP API
    Console.WriteLine("Running in Cloud Mode - HTTP API enabled (MCP stdio disabled)");
    webBuilder = WebApplication.CreateBuilder(args);
    builder = webBuilder;
}
else
{
    // Local mode: Use console host for MCP stdio
    builder = Host.CreateApplicationBuilder(args);
}

// Configure logging
builder.Logging.ClearProviders();
var logLevelEnv = Environment.GetEnvironmentVariable("LOG_LEVEL");
LogLevel parsedLevel = LogLevel.Information;
if (!string.IsNullOrWhiteSpace(logLevelEnv) && Enum.TryParse<LogLevel>(logLevelEnv, true, out var lvl))
{
    parsedLevel = lvl;
}
builder.Logging.AddConsole(options =>
{
    options.LogToStandardErrorThreshold = parsedLevel;
});

// Configure API Management
var azureSearchConfig = new AzureSearchConfig
{
    Endpoint = Environment.GetEnvironmentVariable("API_MANAGEMENT_ENDPOINT") ?? "https://nhsuk-apim-int-uks.azure-api.net/service-search",
    ApiKey = Environment.GetEnvironmentVariable("API_MANAGEMENT_SUBSCRIPTION_KEY") ?? ""
};

// Only register API Management service if configuration is provided
if (!string.IsNullOrEmpty(azureSearchConfig.ApiKey))
{
    builder.Services.AddSingleton(azureSearchConfig);
    builder.Services.AddHttpClient<AzureSearchService>();
    builder.Services.AddSingleton<AzureSearchService>();
}

// Configure MCP server (only if not in cloud mode)
if (!runInCloudMode)
{
    builder.Services.AddMcpServer(options =>
    {
        options.ServerInfo = new ModelContextProtocol.Protocol.Implementation
        {
            Name = "nhs-organizations-mcp-server",
            Version = "1.0.0"
        };
        options.Capabilities = new ModelContextProtocol.Protocol.ServerCapabilities
        {
            Tools = new ModelContextProtocol.Protocol.ToolsCapability
            {
                ListChanged = true
            }
        };
    })
    .WithStdioServerTransport()
    .WithTools<NHSOrganisationSearchTools>()
    .WithTools<NHSHealthContentTools>();
}

// Build the application
if (runInCloudMode && webBuilder != null)
{
    // Build web application with HTTP endpoints
    var app = webBuilder.Build();
    
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    var searchService = app.Services.GetService<AzureSearchService>();
    
    // Health endpoints
    app.MapGet("/healthz", () => Results.Json(new { status = "ok" }));
    app.MapGet("/ready", () => Results.Json(new { status = "ready" }));
    
    // API endpoint: Get organisation types
    app.MapGet("/api/organisation-types", () =>
    {
        try
        {
            logger.LogInformation("GET /api/organisation-types");
            return Results.Json(new
            {
                success = true,
                organisationTypes = OrganisationTypes.Types
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting organisation types");
            return Results.Json(new { success = false, error = ex.Message });
        }
    });
    
    // API endpoint: Convert postcode to coordinates
    app.MapGet("/api/postcode/{postcode}", async (string postcode) =>
    {
        try
        {
            if (searchService == null)
            {
                return Results.Json(new
                {
                    success = false,
                    error = "API Management service not configured"
                });
            }
            
            logger.LogInformation("GET /api/postcode/{Postcode}", postcode);
            var result = await searchService.GetPostcodeCoordinatesAsync(postcode);
            
            if (result == null)
            {
                return Results.Json(new
                {
                    success = false,
                    error = $"Postcode '{postcode}' not found"
                });
            }
            
            return Results.Json(new
            {
                success = true,
                postcode = postcode,
                coordinates = result
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error converting postcode");
            return Results.Json(new { success = false, error = ex.Message });
        }
    });
    
    // API endpoint: Search organizations by postcode
    app.MapGet("/api/search/postcode", async (string organisationType, string postcode, int maxResults = 10) =>
    {
        try
        {
            if (searchService == null)
            {
                return Results.Json(new
                {
                    success = false,
                    error = "API Management service not configured"
                });
            }
            
            var orgType = organisationType.ToUpper();
            if (!OrganisationTypes.Types.ContainsKey(orgType))
            {
                return Results.Json(new
                {
                    success = false,
                    error = $"Invalid organisation type '{organisationType}'"
                });
            }
            
            logger.LogInformation("GET /api/search/postcode?organisationType={OrgType}&postcode={Postcode}&maxResults={MaxResults}",
                orgType, postcode, maxResults);
            
            // Convert postcode to coordinates
            var coordinates = await searchService.GetPostcodeCoordinatesAsync(postcode);
            if (coordinates == null)
            {
                return Results.Json(new
                {
                    success = false,
                    error = $"Postcode '{postcode}' not found"
                });
            }
            
            // Search organizations
            var organizations = await searchService.SearchOrganisationsAsync(
                orgType,
                coordinates.Latitude,
                coordinates.Longitude,
                maxResults);
            
            return Results.Json(new
            {
                success = true,
                postcode = postcode,
                coordinates = new
                {
                    latitude = coordinates.Latitude,
                    longitude = coordinates.Longitude
                },
                organisationType = orgType,
                organisationTypeDescription = OrganisationTypes.Types[orgType],
                resultCount = organizations.Count,
                organizations = organizations
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error searching organizations by postcode");
            return Results.Json(new { success = false, error = ex.Message });
        }
    });
    
    // API endpoint: Search organizations by coordinates
    app.MapGet("/api/search/coordinates", async (string organisationType, double latitude, double longitude, int maxResults = 10) =>
    {
        try
        {
            if (searchService == null)
            {
                return Results.Json(new
                {
                    success = false,
                    error = "API Management service not configured"
                });
            }
            
            var orgType = organisationType.ToUpper();
            if (!OrganisationTypes.Types.ContainsKey(orgType))
            {
                return Results.Json(new
                {
                    success = false,
                    error = $"Invalid organisation type '{organisationType}'"
                });
            }
            
            logger.LogInformation("GET /api/search/coordinates?organisationType={OrgType}&latitude={Lat}&longitude={Lon}&maxResults={MaxResults}",
                orgType, latitude, longitude, maxResults);
            
            var organizations = await searchService.SearchOrganisationsAsync(
                orgType,
                latitude,
                longitude,
                maxResults);
            
            return Results.Json(new
            {
                success = true,
                coordinates = new
                {
                    latitude = latitude,
                    longitude = longitude
                },
                organisationType = orgType,
                organisationTypeDescription = OrganisationTypes.Types[orgType],
                resultCount = organizations.Count,
                organizations = organizations
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error searching organizations by coordinates");
            return Results.Json(new { success = false, error = ex.Message });
        }
    });
    
    logger.LogInformation("Starting HTTP API on port 8080");
    logger.LogInformation("Available endpoints:");
    logger.LogInformation("  GET /healthz - Health check");
    logger.LogInformation("  GET /ready - Readiness check");
    logger.LogInformation("  GET /api/organisation-types - List organisation types");
    logger.LogInformation("  GET /api/postcode/{{postcode}} - Convert postcode to coordinates");
    logger.LogInformation("  GET /api/search/postcode?organisationType={{type}}&postcode={{postcode}}&maxResults={{n}}");
    logger.LogInformation("  GET /api/search/coordinates?organisationType={{type}}&latitude={{lat}}&longitude={{lon}}&maxResults={{n}}");
    
    await app.RunAsync();
}
else
{
    // Build and run console host for MCP stdio
    var host = ((HostApplicationBuilder)builder).Build();
    
    try
    {
        await host.RunAsync();
    }
    catch (Exception ex)
    {
        var logger = host.Services.GetRequiredService<ILogger<Program>>();
        logger.LogCritical(ex, "Application terminated unexpectedly");
        throw;
    }
}
