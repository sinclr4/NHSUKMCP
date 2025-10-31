using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using NHSUKMCP.Models;
using NHSUKMCP.Services;

namespace NHSUKMCP.Tools;

/// <summary>
/// MCP tools for searching NHS organisations
/// </summary>
[McpServerToolType]
public class NHSOrganisationSearchTools
{
    private readonly AzureSearchService _searchService;

    public NHSOrganisationSearchTools(AzureSearchService searchService)
    {
        _searchService = searchService;
    }

    /// <summary>
    /// Get a list of all available NHS organisation types
    /// </summary>
    [McpServerTool(Name = "get_organisation_types")]
    public Task<Dictionary<string, string>> GetOrganisationTypesAsync()
    {
     return Task.FromResult(OrganisationTypes.Types);
    }

    /// <summary>
    /// Convert a UK postcode to latitude and longitude coordinates
    /// </summary>
    /// <param name="postcode">UK postcode (e.g., 'SW1A 1AA', 'M1 1AE', 'B1 1AA')</param>
    [McpServerTool(Name = "convert_postcode_to_coordinates")]
    public async Task<object> ConvertPostcodeToCoordinatesAsync(string postcode)
    {
    if (string.IsNullOrWhiteSpace(postcode))
        {
      throw new ArgumentException("Postcode is required", nameof(postcode));
        }

        var result = await _searchService.GetPostcodeCoordinatesAsync(postcode);
        
        if (result == null)
        {
 throw new InvalidOperationException($"Postcode '{postcode}' not found");
        }

  return new
        {
       postcode = postcode,
        latitude = result.Latitude,
        longitude = result.Longitude
      };
    }

    /// <summary>
    /// Search for NHS organisations near a postcode. Returns organisations sorted by distance.
    /// </summary>
    /// <param name="organisationType">Type of organisation to search for (e.g., 'PHA' for Pharmacy, 'GPB' for GP, 'HOS' for Hospital). Use get_organisation_types to see all available types.</param>
    /// <param name="postcode">UK postcode to search near (e.g., 'SW1A 1AA')</param>
    /// <param name="maxResults">Maximum number of results to return (default: 10)</param>
    [McpServerTool(Name = "search_organisations_by_postcode")]
    public async Task<object> SearchOrganisationsByPostcodeAsync(
        string organisationType,
        string postcode,
      int maxResults = 10)
    {
  if (string.IsNullOrWhiteSpace(organisationType))
        {
  throw new ArgumentException("Organisation type is required", nameof(organisationType));
}

        if (string.IsNullOrWhiteSpace(postcode))
        {
        throw new ArgumentException("Postcode is required", nameof(postcode));
        }

  var orgType = organisationType.ToUpper();
    if (!OrganisationTypes.Types.ContainsKey(orgType))
        {
            throw new ArgumentException($"Invalid organisation type '{organisationType}'. Use get_organisation_types to see valid types.", nameof(organisationType));
        }

        // Convert postcode to coordinates
 var coordinates = await _searchService.GetPostcodeCoordinatesAsync(postcode);
        if (coordinates == null)
        {
            throw new InvalidOperationException($"Postcode '{postcode}' not found");
    }

        // Search organisations
        var organisations = await _searchService.SearchOrganisationsAsync(
   orgType,
         coordinates.Latitude,
 coordinates.Longitude,
         maxResults);

        return new
 {
            postcode = postcode,
   coordinates = new
   {
                latitude = coordinates.Latitude,
  longitude = coordinates.Longitude
          },
       organisationType = orgType,
    organisationTypeDescription = OrganisationTypes.Types[orgType],
 resultCount = organisations.Count,
 organisations = organisations
        };
    }

    /// <summary>
    /// Search for NHS organisations near specific coordinates. Returns organisations sorted by distance.
    /// </summary>
    /// <param name="organisationType">Type of organisation to search for (e.g., 'PHA' for Pharmacy, 'GPB' for GP, 'HOS' for Hospital)</param>
    /// <param name="latitude">Latitude coordinate (e.g., 51.5074)</param>
    /// <param name="longitude">Longitude coordinate (e.g., -0.1278)</param>
    /// <param name="maxResults">Maximum number of results to return (default: 10)</param>
[McpServerTool(Name = "search_organisations_by_coordinates")]
    public async Task<object> SearchOrganisationsByCoordinatesAsync(
        string organisationType,
        double latitude,
    double longitude,
        int maxResults = 10)
    {
if (string.IsNullOrWhiteSpace(organisationType))
        {
        throw new ArgumentException("Organisation type is required", nameof(organisationType));
        }

        var orgType = organisationType.ToUpper();
        if (!OrganisationTypes.Types.ContainsKey(orgType))
        {
    throw new ArgumentException($"Invalid organisation type '{organisationType}'. Use get_organisation_types to see valid types.", nameof(organisationType));
   }

        // Search organisations
        var organisations = await _searchService.SearchOrganisationsAsync(
       orgType,
            latitude,
    longitude,
  maxResults);

   return new
    {
            coordinates = new
        {
  latitude = latitude,
         longitude = longitude
            },
     organisationType = orgType,
  organisationTypeDescription = OrganisationTypes.Types[orgType],
            resultCount = organisations.Count,
            organisations = organisations
        };
    }
}
