using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using NHSOrgsMCP.Models;
using NHSOrgsMCP.Services;

namespace NHSOrgsMCP.Tools;

/// <summary>
/// MCP tools for searching NHS organizations
/// </summary>
[McpServerToolType]
public class NHSOrganizationSearchTools
{
    private readonly AzureSearchService? _searchService;
    private readonly ILogger<NHSOrganizationSearchTools> _logger;

    // Constructor with Azure Search service (preferred when service is registered)
    public NHSOrganizationSearchTools(ILogger<NHSOrganizationSearchTools> logger, AzureSearchService searchService)
    {
        _searchService = searchService;
        _logger = logger;
    }

    // Fallback constructor without Azure Search service
    public NHSOrganizationSearchTools(ILogger<NHSOrganizationSearchTools> logger)
    {
        _searchService = null;
        _logger = logger;
    }

    /// <summary>
    /// Get list of available NHS organization types
    /// </summary>
    /// <returns>Dictionary of organization type codes and descriptions</returns>
    [McpServerTool(Name = "get_organization_types")]
    [Description("Get a list of all available NHS organization types with their descriptions")]
    public Dictionary<string, string> GetOrganizationTypes()
    {
        try
        {
            _logger.LogInformation("Retrieving NHS organization types - START");
            var result = OrganizationTypes.Types;
            _logger.LogInformation("Retrieved {Count} organization types successfully", result.Count);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving organization types");
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
    /// Search for NHS organizations by type and postcode
    /// </summary>
    /// <param name="organizationType">NHS organization type code (e.g., 'PHA' for Pharmacy)</param>
    /// <param name="postcode">UK postcode to search near</param>
    /// <param name="maxResults">Maximum number of results to return (default: 10)</param>
    /// <returns>List of NHS organizations near the specified postcode</returns>
    [McpServerTool(Name = "search_organizations_by_postcode")]
    [Description("Search for NHS organizations by type and postcode. First converts postcode to coordinates, then searches for nearby organizations.")]
    public async Task<object> SearchOrganizationsByPostcode(
        [Description("NHS organization type code (e.g., 'PHA', 'GPP', 'HOS'). Use GetOrganizationTypes to see all available types.")] string organizationType,
        [Description("UK postcode to search near (e.g., 'SW1A 1AA')")] string postcode,
        [Description("Maximum number of results to return (1-50, default: 10)")] int maxResults = 10)
    {
        if (string.IsNullOrWhiteSpace(organizationType))
        {
            throw new ArgumentException("Organization type cannot be empty", nameof(organizationType));
        }

        if (string.IsNullOrWhiteSpace(postcode))
        {
            throw new ArgumentException("Postcode cannot be empty", nameof(postcode));
        }

        if (maxResults < 1 || maxResults > 50)
        {
            throw new ArgumentException("Max results must be between 1 and 50", nameof(maxResults));
        }

        // Validate organization type
        if (!OrganizationTypes.Types.ContainsKey(organizationType.ToUpper()))
        {
            var availableTypes = string.Join(", ", OrganizationTypes.Types.Keys);
            throw new ArgumentException($"Invalid organization type '{organizationType}'. Available types: {availableTypes}", nameof(organizationType));
        }

        if (_searchService == null)
        {
            throw new InvalidOperationException("Azure Search service is not configured. Please check your configuration and set the API_MANAGEMENT_SUBSCRIPTION_KEY environment variable.");
        }

        _logger.LogInformation("Searching for {OrganizationType} organizations near postcode {Postcode}", 
            organizationType, postcode);

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
                    organizationType = organizationType
                };
            }

            // Then search for organizations
            var organizations = await _searchService.SearchOrganizationsAsync(
                organizationType.ToUpper(), 
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
                organizationType = organizationType,
                organizationTypeDescription = OrganizationTypes.Types[organizationType.ToUpper()],
                resultCount = organizations.Count,
                organizations = organizations
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching for organizations");
            return new
            {
                success = false,
                error = $"Search failed: {ex.Message}",
                postcode = postcode,
                organizationType = organizationType
            };
        }
    }

    /// <summary>
    /// Search for NHS organizations by type and coordinates
    /// </summary>
    /// <param name="organizationType">NHS organization type code (e.g., 'PHA' for Pharmacy)</param>
    /// <param name="latitude">Latitude coordinate</param>
    /// <param name="longitude">Longitude coordinate</param>
    /// <param name="maxResults">Maximum number of results to return (default: 10)</param>
    /// <returns>List of NHS organizations near the specified coordinates</returns>
    [McpServerTool(Name = "search_organizations_by_coordinates")]
    [Description("Search for NHS organizations by type and coordinates (latitude/longitude)")]
    public async Task<object> SearchOrganizationsByCoordinates(
        [Description("NHS organization type code (e.g., 'PHA', 'GPP', 'HOS'). Use GetOrganizationTypes to see all available types.")] string organizationType,
        [Description("Latitude coordinate")] double latitude,
        [Description("Longitude coordinate")] double longitude,
        [Description("Maximum number of results to return (1-50, default: 10)")] int maxResults = 10)
    {
        if (string.IsNullOrWhiteSpace(organizationType))
        {
            throw new ArgumentException("Organization type cannot be empty", nameof(organizationType));
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

        // Validate organization type
        if (!OrganizationTypes.Types.ContainsKey(organizationType.ToUpper()))
        {
            var availableTypes = string.Join(", ", OrganizationTypes.Types.Keys);
            throw new ArgumentException($"Invalid organization type '{organizationType}'. Available types: {availableTypes}", nameof(organizationType));
        }

        if (_searchService == null)
        {
            throw new InvalidOperationException("Azure Search service is not configured. Please check your configuration and set the API_MANAGEMENT_SUBSCRIPTION_KEY environment variable.");
        }

        _logger.LogInformation("Searching for {OrganizationType} organizations near {Latitude}, {Longitude}", 
            organizationType, latitude, longitude);

        try
        {
            var organizations = await _searchService.SearchOrganizationsAsync(
                organizationType.ToUpper(), 
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
                organizationType = organizationType,
                organizationTypeDescription = OrganizationTypes.Types[organizationType.ToUpper()],
                resultCount = organizations.Count,
                organizations = organizations
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching for organizations");
            return new
            {
                success = false,
                error = $"Search failed: {ex.Message}",
                coordinates = new
                {
                    latitude = latitude,
                    longitude = longitude
                },
                organizationType = organizationType
            };
        }
    }
}
