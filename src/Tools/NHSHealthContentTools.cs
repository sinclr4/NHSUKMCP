// This is a new file added to the project.
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Extensions.Logging;
using NHSUKMCP.Services;
using System.Text.Json;
using static NHSUKMCPServer.Tools.ToolsInformation;

namespace NHSUKMCPServer.Tools
{
    public class HealthContentTools
    {
        private readonly AzureSearchService _searchService;
        private readonly ILogger<HealthContentTools> _logger;

        public HealthContentTools(ILogger<HealthContentTools> logger, AzureSearchService searchService)
        {
            _logger = logger;
            _searchService = searchService;
        }

        [Function(nameof(GetContentAsync))]
        public async Task<string> GetContentAsync(
            [McpToolTrigger(GetContentToolName, GetContentToolDescription)] ToolInvocationContext context,
            [McpToolProperty(ArticlePropertyName, ArticlePropertyDescription, true)] string topic
        )
        {
            if (string.IsNullOrWhiteSpace(topic))
            {
                throw new ArgumentException("Topic parameter is required", nameof(topic));
            }

            var slug = topic.Trim().ToLowerInvariant();
            _logger.LogInformation("Fetching article for topic slug: {Slug}", slug);
            var result = await _searchService.GetHealthTopicAsync(slug);

            if (result == null)
            {
                throw new InvalidOperationException($"Health topic '{topic}' not found. Please check the topic name and try again.");
            }

            var formattedSections = result.Sections?.Select(s => new
            {
                headline = s.Headline,
                text = s.Text,
                description = s.Description
            }).ToList();

            var payload = new
            {
                name = result.Name,
                description = result.Description,
                url = result.Url,
                lastReviewed = result.LastReviewed,
                dateModified = result.DateModified,
                genre = result.Genre,
                sectionCount = result.Sections?.Count ?? 0,
                sections = formattedSections
            };

            // Serialize explicitly so MCP tool returns a string value
            return JsonSerializer.Serialize(payload);
        }
    }
}
