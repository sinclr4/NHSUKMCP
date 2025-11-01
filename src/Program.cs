using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NHSUKMCP.Services;
using NHSUKMCP.Models;
using static NHSUKMCPServer.Tools.ToolsInformation;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

// Register AzureSearchService and dependencies
builder.Services.AddSingleton<AzureSearchConfig>(sp =>
{
    var config = new AzureSearchConfig
    {
        Endpoint = Environment.GetEnvironmentVariable("NHS_API_ENDPOINT") ?? "https://nhsuk-apim-int-uks.azure-api.net/service-search",
        ApiKey = Environment.GetEnvironmentVariable("NHS_API_KEY") ?? ""
    };
    return config;
});

builder.Services.AddHttpClient<AzureSearchService>();

// Demonstrate how you can define tool properties without requiring
//// input bindings:
//builder
//    .ConfigureMcpTool(GetSnippetToolName)
//    .WithProperty(SnippetNamePropertyName, PropertyType, SnippetNamePropertyDescription, required: true);

builder.Build().Run();
