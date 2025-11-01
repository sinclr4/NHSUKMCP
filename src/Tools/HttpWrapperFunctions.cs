using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using NHSUKMCP.Services;
using NHSUKMCP.Models;
using System.Net;
using System.Text.Json;

namespace NHSUKMCPServer.Tools;

/// <summary>
/// HTTP wrapper functions for MCP tools to enable direct HTTP testing and access
/// </summary>
public class HttpWrapperFunctions
{
    private readonly AzureSearchService _searchService;
    private readonly ILogger<HttpWrapperFunctions> _logger;

    public HttpWrapperFunctions(ILogger<HttpWrapperFunctions> logger, AzureSearchService searchService)
    {
        _logger = logger;
        _searchService = searchService;
    }

    /// <summary>
 /// HTTP endpoint for getting NHS health content
    /// POST /api/GetContent
    /// Body: { "topic": "diabetes" }
    /// </summary>
    [Function("GetContent_Http")]
    public async Task<HttpResponseData> GetContentHttp(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "GetContent")] HttpRequestData req)
    {
   try
  {
   var body = await req.ReadFromJsonAsync<GetContentRequest>();
     
            if (body == null || string.IsNullOrWhiteSpace(body.Topic))
          {
    var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
   await badRequest.WriteAsJsonAsync(new { error = "Topic parameter is required" });
  return badRequest;
         }

      var slug = body.Topic.Trim().ToLowerInvariant();
            _logger.LogInformation("HTTP: Fetching article for topic slug: {Slug}", slug);
            
        var result = await _searchService.GetHealthTopicAsync(slug);

            if (result == null)
  {
             var notFound = req.CreateResponse(HttpStatusCode.NotFound);
      await notFound.WriteAsJsonAsync(new { error = $"Health topic '{body.Topic}' not found. Please check the topic name and try again." });
    return notFound;
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

        var response = req.CreateResponse(HttpStatusCode.OK);
     await response.WriteAsJsonAsync(payload);
return response;
        }
        catch (Exception ex)
     {
            _logger.LogError(ex, "HTTP: Error fetching health topic");
            var error = req.CreateResponse(HttpStatusCode.InternalServerError);
 await error.WriteAsJsonAsync(new { error = ex.Message });
            return error;
        }
  }

    /// <summary>
    /// HTTP endpoint for getting organisation types
    /// POST /api/GetOrganisationTypes
    /// </summary>
    [Function("GetOrganisationTypes_Http")]
    public async Task<HttpResponseData> GetOrganisationTypesHttp(
   [HttpTrigger(AuthorizationLevel.Anonymous, "post", "get", Route = "GetOrganisationTypes")] HttpRequestData req)
    {
        try
        {
            _logger.LogInformation("HTTP: Getting organisation types");
    
    var response = req.CreateResponse(HttpStatusCode.OK);
     await response.WriteAsJsonAsync(OrganisationTypes.Types);
            return response;
      }
        catch (Exception ex)
  {
       _logger.LogError(ex, "HTTP: Error getting organisation types");
         var error = req.CreateResponse(HttpStatusCode.InternalServerError);
            await error.WriteAsJsonAsync(new { error = ex.Message });
   return error;
        }
    }

    /// <summary>
    /// HTTP endpoint for converting postcode to coordinates
    /// POST /api/ConvertPostcode
    /// Body: { "postcode": "SW1A 1AA" }
    /// </summary>
  [Function("ConvertPostcode_Http")]
    public async Task<HttpResponseData> ConvertPostcodeHttp(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "ConvertPostcode")] HttpRequestData req)
{
        try
        {
            var body = await req.ReadFromJsonAsync<ConvertPostcodeRequest>();
            
       if (body == null || string.IsNullOrWhiteSpace(body.Postcode))
{
                var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
       await badRequest.WriteAsJsonAsync(new { error = "Postcode parameter is required" });
   return badRequest;
       }

  _logger.LogInformation("HTTP: Converting postcode: {Postcode}", body.Postcode);
   
         var result = await _searchService.GetPostcodeCoordinatesAsync(body.Postcode);

            if (result == null)
         {
  var notFound = req.CreateResponse(HttpStatusCode.NotFound);
   await notFound.WriteAsJsonAsync(new { error = $"Postcode '{body.Postcode}' not found" });
         return notFound;
            }

  var response = req.CreateResponse(HttpStatusCode.OK);
          await response.WriteAsJsonAsync(new
        {
        postcode = body.Postcode,
       latitude = result.Latitude,
                longitude = result.Longitude
            });
       return response;
  }
      catch (Exception ex)
        {
          _logger.LogError(ex, "HTTP: Error converting postcode");
   var error = req.CreateResponse(HttpStatusCode.InternalServerError);
       await error.WriteAsJsonAsync(new { error = ex.Message });
         return error;
        }
    }

    /// <summary>
    /// HTTP endpoint for searching organisations by postcode
    /// POST /api/SearchOrganisations
    /// Body: { "postcode": "SW1A 1AA", "organisationType": "PHA", "maxResults": 10 }
    /// </summary>
    [Function("SearchOrganisations_Http")]
    public async Task<HttpResponseData> SearchOrganisationsHttp(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "SearchOrganisations")] HttpRequestData req)
    {
        try
        {
    var body = await req.ReadFromJsonAsync<SearchOrganisationsRequest>();
        
    if (body == null || string.IsNullOrWhiteSpace(body.Postcode))
            {
         var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
     await badRequest.WriteAsJsonAsync(new { error = "Postcode parameter is required" });
                return badRequest;
        }

            if (string.IsNullOrWhiteSpace(body.OrganisationType))
     {
        var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
     await badRequest.WriteAsJsonAsync(new { error = "Organisation type parameter is required" });
          return badRequest;
            }

            var orgType = body.OrganisationType.ToUpper();
            if (!OrganisationTypes.Types.ContainsKey(orgType))
      {
     var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
             await badRequest.WriteAsJsonAsync(new 
         { 
           error = $"Invalid organisation type '{body.OrganisationType}'. Use GetOrganisationTypes to see valid types." 
      });
    return badRequest;
       }

 _logger.LogInformation("HTTP: Searching organisations of type {OrgType} near {Postcode}", orgType, body.Postcode);
   
   // Convert postcode to coordinates
            var coordinates = await _searchService.GetPostcodeCoordinatesAsync(body.Postcode);
          if (coordinates == null)
            {
         var notFound = req.CreateResponse(HttpStatusCode.NotFound);
              await notFound.WriteAsJsonAsync(new { error = $"Postcode '{body.Postcode}' not found" });
         return notFound;
      }

            // Search organisations
            var organisations = await _searchService.SearchOrganisationsAsync(
 orgType,
         coordinates.Latitude,
  coordinates.Longitude,
                body.MaxResults ?? 10);

   var response = req.CreateResponse(HttpStatusCode.OK);
      await response.WriteAsJsonAsync(new
    {
       postcode = body.Postcode,
          coordinates = new
        {
   latitude = coordinates.Latitude,
         longitude = coordinates.Longitude
           },
     organisationType = orgType,
organisationTypeDescription = OrganisationTypes.Types[orgType],
           resultCount = organisations.Count,
   organisations = organisations
 });
      return response;
    }
        catch (Exception ex)
    {
            _logger.LogError(ex, "HTTP: Error searching organisations");
    var error = req.CreateResponse(HttpStatusCode.InternalServerError);
 await error.WriteAsJsonAsync(new { error = ex.Message });
            return error;
        }
    }

    /// <summary>
/// Health check endpoint
    /// GET /api/health
  /// </summary>
    [Function("HealthCheck")]
    public async Task<HttpResponseData> HealthCheck(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health")] HttpRequestData req)
    {
 var response = req.CreateResponse(HttpStatusCode.OK);
      await response.WriteAsJsonAsync(new
        {
            status = "healthy",
            service = "NHS UK MCP Server",
   timestamp = DateTime.UtcNow,
       version = "1.0.0"
        });
        return response;
    }
}

// Request DTOs
public class GetContentRequest
{
    public string Topic { get; set; } = string.Empty;
}

public class ConvertPostcodeRequest
{
    public string Postcode { get; set; } = string.Empty;
}

public class SearchOrganisationsRequest
{
    public string Postcode { get; set; } = string.Empty;
    public string OrganisationType { get; set; } = string.Empty;
    public int? MaxResults { get; set; }
}
