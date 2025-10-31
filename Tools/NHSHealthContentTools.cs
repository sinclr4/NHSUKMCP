using ModelContextProtocol.Server;
using NHSUKMCP.Services;

namespace NHSUKMCP.Tools;

/// <summary>
/// MCP tools for retrieving NHS health content
/// </summary>
[McpServerToolType]
public class NHSHealthContentTools
{
    private readonly AzureSearchService _searchService;

    public NHSHealthContentTools(AzureSearchService searchService)
    {
     _searchService = searchService;
    }

    /// <summary>
    /// Get detailed information about a specific health condition or topic from the NHS API.
    /// Returns comprehensive information including description, content sections, and last reviewed date.
    /// </summary>
    /// <param name="topic">Health topic slug (e.g., 'asthma', 'diabetes', 'flu', 'covid-19', 'heart-disease', 'stroke', 'cancer', 'depression', 'anxiety')</param>
    [McpServerTool(Name = "get_health_topic")]
public async Task<object> GetHealthTopicAsync(string topic)
    {
   if (string.IsNullOrWhiteSpace(topic))
        {
    throw new ArgumentException("Topic parameter is required", nameof(topic));
        }

        var result = await _searchService.GetHealthTopicAsync(topic.Trim().ToLower());

        if (result == null)
     {
            throw new InvalidOperationException($"Health topic '{topic}' not found. Please check the topic name and try again.");
        }

        // Format sections for better readability
        var formattedSections = result.Sections?.Select(s => new
        {
   headline = s.Headline,
            text = s.Text,
            description = s.Description
        }).ToList();

        return new
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
    }
}
