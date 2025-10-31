using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using NHSUKMCP.Models;
using NHSUKMCP.Services;

namespace NHSUKMCP.Functions;

/// <summary>
/// Azure Functions that provide MCP tools as streamable HTTP endpoints
/// </summary>
public class McpFunctions
{
    private readonly ILogger<McpFunctions> _logger;
    private readonly AzureSearchService _searchService;

    public McpFunctions(ILogger<McpFunctions> logger, AzureSearchService searchService)
    {
        _logger = logger;
        _searchService = searchService;
    }

    /// <summary>
    /// List all available MCP tools with their schemas
    /// </summary>
    [Function("ListTools")]
    public HttpResponseData ListTools(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "mcp/tools")] HttpRequestData req)
    {
        _logger.LogInformation("GET /mcp/tools - Listing available tools");

        var tools = new
        {
       serverInfo = new
         {
 name = "nhs-uk-mcp-server",
         version = "1.0.0",
 description = "NHS UK Model Context Protocol Server - Search NHS organisations and health information"
      },
      tools = new object[]
     {
                new
 {
        name = "get_organisation_types",
      description = "Get a list of all available NHS organisation types (e.g., Pharmacy, GP, Hospital, Dentist, etc.)",
              inputSchema = new
             {
            type = "object",
   properties = new { },
  required = new string[] { }
        }
          },
         new
           {
          name = "convert_postcode_to_coordinates",
            description = "Convert a UK postcode to latitude and longitude coordinates",
         inputSchema = new
           {
type = "object",
              properties = new
        {
        postcode = new
  {
   type = "string",
          description = "UK postcode (e.g., 'SW1A 1AA', 'M1 1AE', 'B1 1AA')"
         }
             },
      required = new[] { "postcode" }
           }
          },
      new
          {
                    name = "search_organisations_by_postcode",
    description = "Search for NHS organisations near a postcode. Returns organisations sorted by distance.",
           inputSchema = new
         {
      type = "object",
 properties = new
      {
          organisationType = new
            {
            type = "string",
   description = "Type of organisation to search for (e.g., 'PHA' for Pharmacy, 'GPB' for GP, 'HOS' for Hospital). Use get_organisation_types to see all available types."
           },
     postcode = new
    {
           type = "string",
      description = "UK postcode to search near (e.g., 'SW1A 1AA')"
        },
      maxResults = new
   {
         type = "integer",
      description = "Maximum number of results to return (default: 10)"
     }
         },
     required = new[] { "organisationType", "postcode" }
    }
    },
    new
    {
     name = "search_organisations_by_coordinates",
          description = "Search for NHS organisations near specific coordinates. Returns organisations sorted by distance.",
             inputSchema = new
            {
        type = "object",
    properties = new
  {
  organisationType = new
                {
 type = "string",
 description = "Type of organisation to search for (e.g., 'PHA' for Pharmacy, 'GPB' for GP, 'HOS' for Hospital)"
        },
       latitude = new
    {
          type = "number",
  description = "Latitude coordinate (e.g., 51.5074)"
    },
    longitude = new
        {
 type = "number",
         description = "Longitude coordinate (e.g., -0.1278)"
            },
          maxResults = new
       {
       type = "integer",
             description = "Maximum number of results to return (default: 10)"
     }
         },
               required = new[] { "organisationType", "latitude", "longitude" }
                }
     },
    new
      {
     name = "get_health_topic",
       description = "Get detailed information about a specific health condition or topic from the NHS API. Returns comprehensive information including description, content sections, and last reviewed date.",
    inputSchema = new
  {
     type = "object",
 properties = new
 {
      topic = new
 {
         type = "string",
       description = "Health topic slug (e.g., 'asthma', 'diabetes', 'flu', 'covid-19', 'heart-disease', 'stroke', 'cancer', 'depression', 'anxiety')"
               }
      },
         required = new[] { "topic" }
           }
 }
         }
        };

  var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/json");
        response.WriteString(JsonSerializer.Serialize(tools));
    return response;
    }

  /// <summary>
    /// Get all organisation types
    /// </summary>
    [Function("GetOrganisationTypes")]
    public async Task<HttpResponseData> GetOrganisationTypes(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "mcp/tools/get_organisation_types")] HttpRequestData req)
    {
        _logger.LogInformation("Getting organisation types");

  var response = req.CreateResponse(HttpStatusCode.OK);
     response.Headers.Add("Content-Type", "text/event-stream");
        response.Headers.Add("Cache-Control", "no-cache");
        response.Headers.Add("Connection", "keep-alive");

     await using var writer = new StreamWriter(response.Body, Encoding.UTF8, leaveOpen: true);

        // Send data event
 var data = new
  {
  success = true,
        organisationTypes = OrganisationTypes.Types
        };

        await writer.WriteLineAsync("event: data");
await writer.WriteLineAsync($"data: {JsonSerializer.Serialize(data)}");
        await writer.WriteLineAsync();
        await writer.FlushAsync();

        // Send completion event
 await writer.WriteLineAsync("event: complete");
        await writer.WriteLineAsync("data: {\"success\":true}");
  await writer.WriteLineAsync();
        await writer.FlushAsync();

        return response;
  }

    /// <summary>
    /// Convert postcode to coordinates
    /// </summary>
    [Function("ConvertPostcodeToCoordinates")]
    public async Task<HttpResponseData> ConvertPostcodeToCoordinates(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "mcp/tools/convert_postcode_to_coordinates")] HttpRequestData req)
    {
        _logger.LogInformation("Converting postcode to coordinates");

        // Parse input
        string? postcode = null;
    if (req.Method == "POST")
        {
            var body = await new StreamReader(req.Body).ReadToEndAsync();
  var input = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(body);
      postcode = input?["postcode"].GetString();
   }
        else
        {
          postcode = req.Query["postcode"];
        }

        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "text/event-stream");
   response.Headers.Add("Cache-Control", "no-cache");
        response.Headers.Add("Connection", "keep-alive");

        await using var writer = new StreamWriter(response.Body, Encoding.UTF8, leaveOpen: true);

        try
      {
          if (string.IsNullOrWhiteSpace(postcode))
            {
         await writer.WriteLineAsync("event: error");
  await writer.WriteLineAsync("data: {\"success\":false,\"error\":\"Postcode is required\"}");
          await writer.WriteLineAsync();
  await writer.FlushAsync();
       return response;
     }

      var coordinates = await _searchService.GetPostcodeCoordinatesAsync(postcode);

        if (coordinates == null)
            {
         await writer.WriteLineAsync("event: error");
        await writer.WriteLineAsync($"data: {{\"success\":false,\"error\":\"Postcode '{postcode}' not found\"}}");
              await writer.WriteLineAsync();
    await writer.FlushAsync();
           return response;
            }

// Send data event
            var data = new
       {
       success = true,
              postcode = postcode,
         coordinates = coordinates
    };

            await writer.WriteLineAsync("event: data");
         await writer.WriteLineAsync($"data: {JsonSerializer.Serialize(data)}");
            await writer.WriteLineAsync();
            await writer.FlushAsync();

   // Send completion event
     await writer.WriteLineAsync("event: complete");
  await writer.WriteLineAsync("data: {\"success\":true}");
            await writer.WriteLineAsync();
 await writer.FlushAsync();
  }
        catch (Exception ex)
        {
   _logger.LogError(ex, "Error converting postcode");
    await writer.WriteLineAsync("event: error");
            await writer.WriteLineAsync($"data: {{\"success\":false,\"error\":\"{ex.Message}\"}}");
await writer.WriteLineAsync();
            await writer.FlushAsync();
        }

        return response;
    }

    /// <summary>
    /// Search organisations by postcode
    /// </summary>
    [Function("SearchOrganisationsByPostcode")]
    public async Task<HttpResponseData> SearchOrganisationsByPostcode(
 [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "mcp/tools/search_organisations_by_postcode")] HttpRequestData req)
    {
        _logger.LogInformation("Searching organisations by postcode");

        // Parse input
        string? organisationType = null;
        string? postcode = null;
        int maxResults = 10;

    if (req.Method == "POST")
        {
    var body = await new StreamReader(req.Body).ReadToEndAsync();
        var input = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(body);
            organisationType = input?["organisationType"].GetString();
     postcode = input?["postcode"].GetString();
       if (input?.ContainsKey("maxResults") == true)
            {
          maxResults = input["maxResults"].GetInt32();
 }
        }
        else
        {
   organisationType = req.Query["organisationType"];
            postcode = req.Query["postcode"];
     if (int.TryParse(req.Query["maxResults"], out var max))
       {
                maxResults = max;
 }
        }

   var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "text/event-stream");
  response.Headers.Add("Cache-Control", "no-cache");
  response.Headers.Add("Connection", "keep-alive");

        await using var writer = new StreamWriter(response.Body, Encoding.UTF8, leaveOpen: true);

        try
        {
  if (string.IsNullOrWhiteSpace(organisationType) || string.IsNullOrWhiteSpace(postcode))
   {
                await writer.WriteLineAsync("event: error");
    await writer.WriteLineAsync("data: {\"success\":false,\"error\":\"organisationType and postcode are required\"}");
    await writer.WriteLineAsync();
     await writer.FlushAsync();
     return response;
          }

            var orgType = organisationType.ToUpper();
         if (!OrganisationTypes.Types.ContainsKey(orgType))
       {
       await writer.WriteLineAsync("event: error");
    await writer.WriteLineAsync($"data: {{\"success\":false,\"error\":\"Invalid organisation type '{organisationType}'\"}}");
                await writer.WriteLineAsync();
  await writer.FlushAsync();
          return response;
   }

  // Convert postcode to coordinates
    var coordinates = await _searchService.GetPostcodeCoordinatesAsync(postcode);
  if (coordinates == null)
     {
    await writer.WriteLineAsync("event: error");
         await writer.WriteLineAsync($"data: {{\"success\":false,\"error\":\"Postcode '{postcode}' not found\"}}");
       await writer.WriteLineAsync();
      await writer.FlushAsync();
                return response;
    }

            // Send metadata event
        await writer.WriteLineAsync("event: metadata");
      await writer.WriteLineAsync($"data: {{\"postcode\":\"{postcode}\",\"organisationType\":\"{orgType}\",\"coordinates\":{{\"latitude\":{coordinates.Latitude},\"longitude\":{coordinates.Longitude}}}}}");
   await writer.WriteLineAsync();
 await writer.FlushAsync();

 // Search organisations
            var organizations = await _searchService.SearchOrganisationsAsync(orgType, coordinates.Latitude, coordinates.Longitude, maxResults);

      // Stream each organization
       foreach (var org in organizations)
{
      await writer.WriteLineAsync("event: organisation");
      await writer.WriteLineAsync($"data: {JsonSerializer.Serialize(org)}");
          await writer.WriteLineAsync();
         await writer.FlushAsync();
await Task.Delay(50); // Small delay for demonstration
  }

         // Send completion event
            await writer.WriteLineAsync("event: complete");
 await writer.WriteLineAsync($"data: {{\"success\":true,\"resultCount\":{organizations.Count}}}");
  await writer.WriteLineAsync();
         await writer.FlushAsync();
        }
        catch (Exception ex)
 {
_logger.LogError(ex, "Error searching organisations by postcode");
      await writer.WriteLineAsync("event: error");
         await writer.WriteLineAsync($"data: {{\"success\":false,\"error\":\"{ex.Message}\"}}");
            await writer.WriteLineAsync();
    await writer.FlushAsync();
    }

    return response;
    }

    /// <summary>
    /// Search organisations by coordinates
    /// </summary>
    [Function("SearchOrganisationsByCoordinates")]
    public async Task<HttpResponseData> SearchOrganisationsByCoordinates(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "mcp/tools/search_organisations_by_coordinates")] HttpRequestData req)
    {
        _logger.LogInformation("Searching organisations by coordinates");

 // Parse input
        string? organisationType = null;
        double latitude = 0;
  double longitude = 0;
        int maxResults = 10;

        if (req.Method == "POST")
        {
  var body = await new StreamReader(req.Body).ReadToEndAsync();
            var input = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(body);
            organisationType = input?["organisationType"].GetString();
     latitude = input?["latitude"].GetDouble() ?? 0;
            longitude = input?["longitude"].GetDouble() ?? 0;
 if (input?.ContainsKey("maxResults") == true)
 {
       maxResults = input["maxResults"].GetInt32();
            }
        }
        else
 {
     organisationType = req.Query["organisationType"];
            double.TryParse(req.Query["latitude"], out latitude);
         double.TryParse(req.Query["longitude"], out longitude);
    if (int.TryParse(req.Query["maxResults"], out var max))
        {
          maxResults = max;
     }
 }

        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "text/event-stream");
        response.Headers.Add("Cache-Control", "no-cache");
        response.Headers.Add("Connection", "keep-alive");

      await using var writer = new StreamWriter(response.Body, Encoding.UTF8, leaveOpen: true);

        try
        {
      if (string.IsNullOrWhiteSpace(organisationType))
     {
                await writer.WriteLineAsync("event: error");
           await writer.WriteLineAsync("data: {\"success\":false,\"error\":\"organisationType is required\"}");
         await writer.WriteLineAsync();
    await writer.FlushAsync();
                return response;
            }

 var orgType = organisationType.ToUpper();
   if (!OrganisationTypes.Types.ContainsKey(orgType))
        {
         await writer.WriteLineAsync("event: error");
          await writer.WriteLineAsync($"data: {{\"success\":false,\"error\":\"Invalid organisation type '{organisationType}'\"}}");
    await writer.WriteLineAsync();
            await writer.FlushAsync();
 return response;
            }

            // Send metadata event
    await writer.WriteLineAsync("event: metadata");
         await writer.WriteLineAsync($"data: {{\"organisationType\":\"{orgType}\",\"coordinates\":{{\"latitude\":{latitude},\"longitude\":{longitude}}}}}");
            await writer.WriteLineAsync();
       await writer.FlushAsync();

    // Search organisations
     var organizations = await _searchService.SearchOrganisationsAsync(orgType, latitude, longitude, maxResults);

    // Stream each organization
            foreach (var org in organizations)
            {
           await writer.WriteLineAsync("event: organisation");
                await writer.WriteLineAsync($"data: {JsonSerializer.Serialize(org)}");
    await writer.WriteLineAsync();
    await writer.FlushAsync();
        await Task.Delay(50); // Small delay for demonstration
       }

      // Send completion event
     await writer.WriteLineAsync("event: complete");
            await writer.WriteLineAsync($"data: {{\"success\":true,\"resultCount\":{organizations.Count}}}");
            await writer.WriteLineAsync();
            await writer.FlushAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching organisations by coordinates");
    await writer.WriteLineAsync("event: error");
       await writer.WriteLineAsync($"data: {{\"success\":false,\"error\":\"{ex.Message}\"}}");
            await writer.WriteLineAsync();
          await writer.FlushAsync();
        }

        return response;
    }

    /// <summary>
    /// Get health topic information (streamed)
    /// </summary>
 [Function("GetHealthTopic")]
    public async Task<HttpResponseData> GetHealthTopic(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "mcp/tools/get_health_topic")] HttpRequestData req)
    {
        _logger.LogInformation("Getting health topic");

        // Parse input
      string? topic = null;
        if (req.Method == "POST")
        {
            var body = await new StreamReader(req.Body).ReadToEndAsync();
      var input = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(body);
    topic = input?["topic"].GetString();
        }
        else
        {
topic = req.Query["topic"];
        }

        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "text/event-stream");
        response.Headers.Add("Cache-Control", "no-cache");
   response.Headers.Add("Connection", "keep-alive");

      await using var writer = new StreamWriter(response.Body, Encoding.UTF8, leaveOpen: true);

      try
        {
            if (string.IsNullOrWhiteSpace(topic))
    {
  await writer.WriteLineAsync("event: error");
 await writer.WriteLineAsync("data: {\"success\":false,\"error\":\"Topic parameter is required\"}");
     await writer.WriteLineAsync();
        await writer.FlushAsync();
        return response;
       }

       var result = await _searchService.GetHealthTopicAsync(topic.Trim().ToLower());

      if (result == null)
      {
                await writer.WriteLineAsync("event: error");
       await writer.WriteLineAsync($"data: {{\"success\":false,\"error\":\"Health topic '{topic}' not found\"}}");
     await writer.WriteLineAsync();
              await writer.FlushAsync();
              return response;
            }

    // Send metadata event
var metadata = new
      {
         name = result.Name,
   description = result.Description,
       url = result.Url,
            lastReviewed = result.LastReviewed,
           dateModified = result.DateModified,
                genre = result.Genre,
         sectionCount = result.Sections?.Count ?? 0
            };

            await writer.WriteLineAsync("event: metadata");
            await writer.WriteLineAsync($"data: {JsonSerializer.Serialize(metadata)}");
     await writer.WriteLineAsync();
            await writer.FlushAsync();

            // Stream each section
            if (result.Sections != null)
    {
    for (int i = 0; i < result.Sections.Count; i++)
         {
        var section = result.Sections[i];
           var sectionData = new
      {
       index = i,
    headline = section.Headline,
           text = section.Text,
       description = section.Description
          };

     await writer.WriteLineAsync("event: section");
            await writer.WriteLineAsync($"data: {JsonSerializer.Serialize(sectionData)}");
             await writer.WriteLineAsync();
 await writer.FlushAsync();
  await Task.Delay(50); // Small delay for demonstration
         }
    }

// Send completion event
    await writer.WriteLineAsync("event: complete");
       await writer.WriteLineAsync("data: {\"success\":true}");
          await writer.WriteLineAsync();
         await writer.FlushAsync();
        }
        catch (Exception ex)
 {
            _logger.LogError(ex, "Error fetching health topic");
            await writer.WriteLineAsync("event: error");
            await writer.WriteLineAsync($"data: {{\"success\":false,\"error\":\"{ex.Message}\"}}");
        await writer.WriteLineAsync();
        await writer.FlushAsync();
      }

        return response;
  }
}
