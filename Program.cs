using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Server;
using NHSUKMCP.Models;
using NHSUKMCP.Services;
using NHSUKMCP.Tools;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        // Configure API Management
        var azureSearchConfig = new AzureSearchConfig
        {
            Endpoint = Environment.GetEnvironmentVariable("API_MANAGEMENT_ENDPOINT") 
      ?? "https://nhsuk-apim-int-uks.azure-api.net/service-search",
            ApiKey = Environment.GetEnvironmentVariable("API_MANAGEMENT_SUBSCRIPTION_KEY") ?? ""
        };

        services.AddSingleton(azureSearchConfig);
        services.AddHttpClient<AzureSearchService>();
        services.AddSingleton<AzureSearchService>();

        // Register MCP tool classes
        services.AddScoped<NHSOrganisationSearchTools>();
        services.AddScoped<NHSHealthContentTools>();

    // Configure MCP Server
        services.AddMcpServer(options =>
        {
 options.ServerInfo = new ModelContextProtocol.Protocol.Implementation
   {
      Name = "nhs-uk-mcp-server",
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
        .WithTools<NHSOrganisationSearchTools>()
     .WithTools<NHSHealthContentTools>();
    })
    .Build();

await host.RunAsync();
