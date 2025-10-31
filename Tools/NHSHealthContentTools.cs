using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using NHSUKMCP.Services;

namespace NHSUKMCP.Tools;

/// <summary>
/// MCP tools for retrieving NHS health content
/// </summary>
[McpServerToolType]
public class NHSHealthContentTools
{
    private readonly AzureSearchService? _searchService;
    private readonly ILogger<NHSHealthContentTools> _logger;

    // Constructor with Azure Search service (preferred when service is registered)
    public NHSHealthContentTools(ILogger<NHSHealthContentTools> logger, AzureSearchService searchService)
    {
        _searchService = searchService;
        _logger = logger;
    }

    // Fallback constructor without Azure Search service
    public NHSHealthContentTools(ILogger<NHSHealthContentTools> logger)
    {
        _searchService = null;
        _logger = logger;
    }

    /// <summary>
    /// Get detailed information about a specific health condition or topic from the NHS API
    /// </summary>
    /// <param name="topic">The health topic to retrieve (e.g., 'asthma', 'diabetes', 'flu', 'covid-19')</param>
    /// <returns>Health topic information including name, description, content sections, and last reviewed date</returns>
    [McpServerTool(Name = "get_health_topic")]
    [Description("Get detailed information about a specific health condition or topic from the NHS API. Returns comprehensive information including description, content sections, and last reviewed date.")]
    public async Task<object> GetHealthTopic(
        [Description("Health topic slug (e.g., 'asthma', 'diabetes', 'flu', 'covid-19', 'heart-disease', 'stroke', 'cancer', 'depression', 'anxiety')")] string topic)
    {
        if (string.IsNullOrWhiteSpace(topic))
        {
            throw new ArgumentException("Topic cannot be empty", nameof(topic));
        }

        if (_searchService == null)
        {
            throw new InvalidOperationException("Azure Search service is not configured. Please check your configuration.");
        }

        _logger.LogInformation("Fetching health topic: {Topic}", topic);

        try
        {
            var result = await _searchService.GetHealthTopicAsync(topic.Trim().ToLower());
            
            if (result == null)
            {
                return new
                {
                    success = false,
                    error = $"Health topic '{topic}' not found. Please check the topic name and try again.",
                    topic = topic
                };
            }

            // Format sections for better readability
            var formattedSections = result.Sections?.Select(s => new
            {
                headline = s.Headline,
                text = s.Text != null && s.Text.Length > 500 ? s.Text.Substring(0, 500) + "..." : s.Text,
                description = s.Description
            }).ToList();

            return new
            {
                success = true,
                name = result.Name,
                description = result.Description,
                url = result.Url,
                lastReviewed = result.LastReviewed,
                dateModified = result.DateModified,
                genre = result.Genre,
                sectionCount = result.Sections?.Count ?? 0,
                sections = formattedSections
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching health topic");
            return new
            {
                success = false,
                error = $"Failed to retrieve health topic: {ex.Message}",
                topic = topic
            };
        }
    }
}
