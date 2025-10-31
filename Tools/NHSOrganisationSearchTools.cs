using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using NHSOrgsMCP.Models;
using NHSOrgsMCP.Services;

namespace NHSOrgsMCP.Tools;

/// <summary>
/// MCP tools for searching NHS organisations
/// </summary>
[McpServerToolType]
public class NHSOrganisationSearchTools
{
    private readonly AzureSearchService? _searchService;
    private readonly ILogger<NHSOrganisationSearchTools> _logger;

    // Constructor with Azure Search service (preferred when service is registered)
    public NHSOrganisationSearchTools(ILogger<NHSOrganisationSearchTools> logger, AzureSearchService searchService)
    {
        _searchService = searchService;
        _logger = logger;
    }

    // Fallback constructor without Azure Search service
    public NHSOrganisationSearchTools(ILogger<NHSOrganisationSearchTools> logger)
    {
        _searchService = null;
        _logger = logger;
    }

    /// <summary>
    /// Get list of available NHS organisation types
    /// </summary>
    /// <returns>Dictionary of organisation type codes and descriptions</returns>
    [McpServerTool(Name = "get_organisation_types")]
    [Description("Get a list of all available NHS organisation types with their descriptions")]
    public Dictionary<string, string> GetOrganisationTypes()
    {
        try
        {
            _logger.LogInformation("Retrieving NHS organisation types - START");
            var result = OrganisationTypes.Types;
            _logger.LogInformation("Retrieved {Count} organisation types successfully", result.Count);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving organisation types");
            throw;
        }
    }

    /// <summary>
    /// Convert a UK postcode to latitude and longitude coordinates
    /// </summary>
    /// <param name="postcode">UK postcode (e.g., 'SW1A 1AA')</param>
    /// <returns>Coordinates for the postcode</returns>
    [McpServerTool(Name = "convert_postcode_to_coordinates")]
    [Description("Convert a UK postcode to latitude and longitude coordinates using Azure Search")]
    public async Task<PostcodeResult?> ConvertPostcodeToCoordinates(
        [Description("UK postcode to convert (e.g., 'SW1A 1AA')")] string postcode)
    {
        if (string.IsNullOrWhiteSpace(postcode))
        {
            throw new ArgumentException("Postcode cannot be empty", nameof(postcode));
        }

        if (_searchService == null)
        {
            throw new InvalidOperationException("Azure Search service is not configured. Please check your configuration.");
        }

        _logger.LogInformation("Converting postcode {Postcode} to coordinates", postcode);
        return await _searchService.GetPostcodeCoordinatesAsync(postcode.Trim());
    }

    /// <summary>
    /// Search for NHS organisations by type and postcode
    /// </summary>
    /// <param name="organisationType">NHS organisation type code (e.g., 'PHA' for Pharmacy)</param>
    /// <param name="postcode">UK postcode to search near</param>
    /// <param name="maxResults">Maximum number of results to return (default: 10)</param>
    /// <returns>List of NHS organisations near the specified postcode</returns>
    [McpServerTool(Name = "search_organisations_by_postcode")]
    [Description("Search for NHS organisations by type and postcode. First converts postcode to coordinates, then searches for nearby organisations.")]
    public async Task<object> SearchOrganisationsByPostcode(
        [Description("NHS organisation type code (e.g., 'PHA', 'GPP', 'HOS'). Use GetOrganisationTypes to see all available types.")] string organisationType,
        [Description("UK postcode to search near (e.g., 'SW1A 1AA')")] string postcode,
        [Description("Maximum number of results to return (1-50, default: 10)")] int maxResults = 10)
    {
        if (string.IsNullOrWhiteSpace(organisationType))
        {
            throw new ArgumentException("Organisation type cannot be empty", nameof(organisationType));
        }

        if (string.IsNullOrWhiteSpace(postcode))
        {
            throw new ArgumentException("Postcode cannot be empty", nameof(postcode));
        }

        if (maxResults < 1 || maxResults > 50)
        {
            throw new ArgumentException("Max results must be between 1 and 50", nameof(maxResults));
        }

        // Validate organisation type
        if (!OrganisationTypes.Types.ContainsKey(organisationType.ToUpper()))
        {
            var availableTypes = string.Join(", ", OrganisationTypes.Types.Keys);
            throw new ArgumentException($"Invalid organisation type '{organisationType}'. Available types: {availableTypes}", nameof(organisationType));
        }

        if (_searchService == null)
        {
            throw new InvalidOperationException("Azure Search service is not configured. Please check your configuration and set the API_MANAGEMENT_SUBSCRIPTION_KEY environment variable.");
        }

        _logger.LogInformation("Searching for {OrganisationType} organisations near postcode {Postcode}", 
            organisationType, postcode);

        try
        {
            // First, convert postcode to coordinates
            var coordinates = await _searchService.GetPostcodeCoordinatesAsync(postcode.Trim());
            if (coordinates == null)
            {
                return new
                {
                    success = false,
                    error = $"Could not find coordinates for postcode '{postcode}'. Please check the postcode is valid.",
                    postcode = postcode,
                    organisationType = organisationType
                };
            }

            // Then search for organisations
            var organisations = await _searchService.SearchOrganisationsAsync(
                organisationType.ToUpper(), 
                coordinates.Latitude, 
                coordinates.Longitude, 
                maxResults);

            return new
            {
                success = true,
                postcode = postcode,
                coordinates = new
                {
                    latitude = coordinates.Latitude,
                    longitude = coordinates.Longitude
                },
                organisationType = organisationType,
                organisationTypeDescription = OrganisationTypes.Types[organisationType.ToUpper()],
                resultCount = organisations.Count,
                organisations = organisations
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching for organisations");
            return new
            {
                success = false,
                error = $"Search failed: {ex.Message}",
                postcode = postcode,
                organisationType = organisationType
            };
        }
    }

    /// <summary>
    /// Search for NHS organisations by type and coordinates
    /// </summary>
    /// <param name="organisationType">NHS organisation type code (e.g., 'PHA' for Pharmacy)</param>
    /// <param name="latitude">Latitude coordinate</param>
    /// <param name="longitude">Longitude coordinate</param>
    /// <param name="maxResults">Maximum number of results to return (default: 10)</param>
    /// <returns>List of NHS organisations near the specified coordinates</returns>
    [McpServerTool(Name = "search_organisations_by_coordinates")]
    [Description("Search for NHS organisations by type and coordinates (latitude/longitude)")]
    public async Task<object> SearchOrganisationsByCoordinates(
        [Description("NHS organisation type code (e.g., 'PHA', 'GPP', 'HOS'). Use GetOrganisationTypes to see all available types.")] string organisationType,
        [Description("Latitude coordinate")] double latitude,
        [Description("Longitude coordinate")] double longitude,
        [Description("Maximum number of results to return (1-50, default: 10)")] int maxResults = 10)
    {
        if (string.IsNullOrWhiteSpace(organisationType))
        {
            throw new ArgumentException("Organisation type cannot be empty", nameof(organisationType));
        }

        if (maxResults < 1 || maxResults > 50)
        {
            throw new ArgumentException("Max results must be between 1 and 50", nameof(maxResults));
        }

        if (latitude < -90 || latitude > 90)
        {
            throw new ArgumentException("Latitude must be between -90 and 90", nameof(latitude));
        }

        if (longitude < -180 || longitude > 180)
        {
            throw new ArgumentException("Longitude must be between -180 and 180", nameof(longitude));
        }

        // Validate organisation type
        if (!OrganisationTypes.Types.ContainsKey(organisationType.ToUpper()))
        {
            var availableTypes = string.Join(", ", OrganisationTypes.Types.Keys);
            throw new ArgumentException($"Invalid organisation type '{organisationType}'. Available types: {availableTypes}", nameof(organisationType));
        }

        if (_searchService == null)
        {
            throw new InvalidOperationException("Azure Search service is not configured. Please check your configuration and set the API_MANAGEMENT_SUBSCRIPTION_KEY environment variable.");
        }

        _logger.LogInformation("Searching for {OrganisationType} organisations near {Latitude}, {Longitude}", 
            organisationType, latitude, longitude);

        try
        {
            var organisations = await _searchService.SearchOrganisationsAsync(
                organisationType.ToUpper(), 
                latitude, 
                longitude, 
                maxResults);

            return new
            {
                success = true,
                coordinates = new
                {
                    latitude = latitude,
                    longitude = longitude
                },
                organisationType = organisationType,
                organisationTypeDescription = OrganisationTypes.Types[organisationType.ToUpper()],
                resultCount = organisations.Count,
                organisations = organisations
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching for organisations");
            return new
            {
                success = false,
                error = $"Search failed: {ex.Message}",
                coordinates = new
                {
                    latitude = latitude,
                    longitude = longitude
                },
                organisationType = organisationType
            };
        }
    }
}
