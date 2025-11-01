using System.Text.Json;
using Microsoft.Extensions.Logging;
using NHSUKMCP.Models;

namespace NHSUKMCP.Services;

/// <summary>
/// Service for interacting with Azure Search to find postcodes and NHS organizations
/// </summary>
public class AzureSearchService
{
    private readonly HttpClient _httpClient;
    private readonly AzureSearchConfig _config;
    private readonly ILogger<AzureSearchService> _logger;

    public AzureSearchService(HttpClient httpClient, AzureSearchConfig config, ILogger<AzureSearchService> logger)
    {
        _httpClient = httpClient;
        _config = config;
        _logger = logger;

        // Set up common headers for API Management
        _httpClient.DefaultRequestHeaders.Add("subscription-key", _config.ApiKey);
    }

    /// <summary>
    /// Convert a postcode to latitude and longitude using API Management
    /// </summary>
    /// <param name="postcode">The postcode to convert</param>
    /// <returns>PostcodeResult with lat/long coordinates</returns>
    public async Task<PostcodeResult?> GetPostcodeCoordinatesAsync(string postcode)
    {
        try
        {
            // Use API Management endpoint for postcode search
            var normalizedPostcode = postcode.Replace(" ", "").ToUpper();
            var searchUrl = $"{_config.Endpoint}/postcodesandplaces/?search={Uri.EscapeDataString(normalizedPostcode)}&api-version=2";

            _logger.LogInformation("Searching for postcode: {Postcode}", postcode);

            var response = await _httpClient.GetAsync(searchUrl);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("API Management postcode error: {StatusCode} - {ErrorContent}", response.StatusCode, errorContent);
                throw new HttpRequestException($"API Management postcode request failed: {response.StatusCode} - {errorContent}");
            }

            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(responseContent);

            // API Management returns results in a "value" array like Azure Search
            if (!document.RootElement.TryGetProperty("value", out var valueArray) || valueArray.GetArrayLength() == 0)
            {
                _logger.LogWarning("No results found for postcode: {Postcode}", postcode);
                return null;
            }

            var firstResult = valueArray[0];

            // Extract coordinates from the first result
            double? latitude = null;
            double? longitude = null;

            // Try various possible field names for coordinates
            if (firstResult.TryGetProperty("Latitude", out var latProp))
                latitude = latProp.GetDouble();
            else if (firstResult.TryGetProperty("latitude", out var latProp2))
                latitude = latProp2.GetDouble();
            else if (firstResult.TryGetProperty("lat", out var latProp3))
                latitude = latProp3.GetDouble();

            if (firstResult.TryGetProperty("Longitude", out var lngProp))
                longitude = lngProp.GetDouble();
            else if (firstResult.TryGetProperty("longitude", out var lngProp2))
                longitude = lngProp2.GetDouble();
            else if (firstResult.TryGetProperty("lng", out var lngProp3))
                longitude = lngProp3.GetDouble();
            else if (firstResult.TryGetProperty("lon", out var lngProp4))
                longitude = lngProp4.GetDouble();

            if (latitude.HasValue && longitude.HasValue)
            {
                return new PostcodeResult
                {
                    Latitude = latitude.Value,
                    Longitude = longitude.Value,
                    Postcode = postcode
                };
            }

            _logger.LogWarning("Could not extract coordinates from postcode search result for: {Postcode}", postcode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching for postcode: {Postcode}", postcode);
            throw;
        }
    }

    /// <summary>
    /// Search for NHS organizations by type and location using API Management
    /// </summary>
    /// <param name="organizationType">The organization type ID (e.g., 'PHA' for Pharmacy)</param>
    /// <param name="latitude">Latitude coordinate</param>
    /// <param name="longitude">Longitude coordinate</param>
    /// <param name="maxResults">Maximum number of results to return</param>
    /// <returns>List of organizations near the specified location</returns>
    public async Task<List<OrganisationResult>> SearchOrganisationsAsync(
        string organizationType,
        double latitude,
        double longitude,
        int maxResults = 10)
    {
        try
        {
            // Use API Management endpoint for service search
            var searchUrl = $"{_config.Endpoint}/search?api-version=2";

            var searchRequest = new
            {
                search = "*",
                filter = $"OrganisationTypeId eq '{organizationType}'",
                searchMode = "all",
                orderby = $"geo.distance(Geocode, geography'POINT({longitude} {latitude})')",
                top = maxResults,
                count = true
            };

            var json = JsonSerializer.Serialize(searchRequest);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            _logger.LogInformation("Searching for organizations of type {OrganisationType} near {Latitude}, {Longitude}",
                organizationType, latitude, longitude);
            _logger.LogDebug("Search request: {Json}", json);

            var response = await _httpClient.PostAsync(searchUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("API Management search error: {StatusCode} - {ErrorContent}", response.StatusCode, errorContent);
                throw new HttpRequestException($"API Management search request failed: {response.StatusCode} - {errorContent}");
            }

            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(responseContent);

            var results = new List<OrganisationResult>();
            var values = document.RootElement.GetProperty("value");

            foreach (var item in values.EnumerateArray())
            {
                var organization = new OrganisationResult();

                // Extract organization details using actual field names from the index
                if (item.TryGetProperty("OrganisationName", out var nameProp))
                    organization.OrganisationName = nameProp.GetString();

                if (item.TryGetProperty("OrganisationTypeId", out var typeProp))
                    organization.OrganisationTypeID = typeProp.GetString();

                if (item.TryGetProperty("ODSCode", out var odsProp))
                    organization.ODSCode = odsProp.GetString();

                // Combine address fields
                var addressParts = new List<string>();
                if (item.TryGetProperty("Address1", out var addr1) && !string.IsNullOrWhiteSpace(addr1.GetString()))
                    addressParts.Add(addr1.GetString()!);
                if (item.TryGetProperty("Address2", out var addr2) && !string.IsNullOrWhiteSpace(addr2.GetString()))
                    addressParts.Add(addr2.GetString()!);
                if (item.TryGetProperty("Address3", out var addr3) && !string.IsNullOrWhiteSpace(addr3.GetString()))
                    addressParts.Add(addr3.GetString()!);
                if (item.TryGetProperty("City", out var city) && !string.IsNullOrWhiteSpace(city.GetString()))
                    addressParts.Add(city.GetString()!);
                if (item.TryGetProperty("County", out var county) && !string.IsNullOrWhiteSpace(county.GetString()))
                    addressParts.Add(county.GetString()!);

                organization.Address = string.Join(", ", addressParts);

                if (item.TryGetProperty("Postcode", out var postcodeProp))
                    organization.Postcode = postcodeProp.GetString();

                // Extract latitude and longitude directly from the document
                if (item.TryGetProperty("Latitude", out var latProp) && item.TryGetProperty("Longitude", out var lngProp))
                {
                    organization.Geocode = new PostcodeResult
                    {
                        Latitude = latProp.GetDouble(),
                        Longitude = lngProp.GetDouble()
                    };
                }

                // The distance is returned by geo.distance in the orderby, but we need to calculate it manually
                // or Azure Search might provide it in a different way
                if (organization.Geocode != null)
                {
                    // Calculate approximate distance in km using Haversine formula
                    var R = 6371; // Earth's radius in km
                    var dLat = (organization.Geocode.Latitude - latitude) * Math.PI / 180;
                    var dLon = (organization.Geocode.Longitude - longitude) * Math.PI / 180;
                    var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                           Math.Cos(latitude * Math.PI / 180) * Math.Cos(organization.Geocode.Latitude * Math.PI / 180) *
                           Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
                    var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
                    organization.Distance = R * c;
                }

                results.Add(organization);
            }

            _logger.LogInformation("Found {Count} organizations of type {OrganisationType} near {Latitude}, {Longitude}",
                results.Count, organizationType, latitude, longitude);

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching for organizations of type {OrganisationType} near {Latitude}, {Longitude}",
                organizationType, latitude, longitude);
            throw;
        }
    }

    /// <summary>
    /// Get detailed information about a specific health condition or topic from the NHS API
    /// </summary>
    /// <param name="topicSlug">The health topic slug (e.g., 'asthma', 'diabetes', 'flu')</param>
    /// <returns>Health topic information including name, description, content sections, and last reviewed date</returns>
    public async Task<HealthTopicResult?> GetHealthTopicAsync(string topicSlug)
    {
        try
        {
            var normalizedSlug = topicSlug.Trim().ToLower();

            // Extract base URL (remove /service-search if present) and use /conditions endpoint
            var baseUrl = _config.Endpoint.Replace("/service-search", "");
            var url = $"{baseUrl}/conditions/{Uri.EscapeDataString(normalizedSlug)}";

            _logger.LogInformation("Fetching health topic: {TopicSlug} from {Url}", normalizedSlug, url);

            var response = await _httpClient.GetAsync(url);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Health topic not found: {TopicSlug}", normalizedSlug);
                return null;
            }

            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(responseContent);

            var result = new HealthTopicResult();

            // Extract basic information
            if (document.RootElement.TryGetProperty("name", out var nameProp))
                result.Name = nameProp.GetString();

            if (document.RootElement.TryGetProperty("description", out var descProp))
                result.Description = descProp.GetString();

            if (document.RootElement.TryGetProperty("url", out var urlProp))
                result.Url = urlProp.GetString();

            if (document.RootElement.TryGetProperty("dateModified", out var modifiedProp))
                result.DateModified = modifiedProp.GetString();

            if (document.RootElement.TryGetProperty("lastReviewed", out var reviewedProp))
            {
                if (reviewedProp.ValueKind == JsonValueKind.Array && reviewedProp.GetArrayLength() > 0)
                {
                    result.LastReviewed = reviewedProp[0].GetString();
                }
                else if (reviewedProp.ValueKind == JsonValueKind.String)
                {
                    result.LastReviewed = reviewedProp.GetString();
                }
            }

            if (document.RootElement.TryGetProperty("genre", out var genreProp))
            {
                if (genreProp.ValueKind == JsonValueKind.Array)
                {
                    result.Genre = genreProp.EnumerateArray()
                        .Select(x => x.GetString())
                        .Where(x => x != null)
                        .Cast<string>()
                        .ToList();
                }
            }

            // Extract content sections from mainEntityOfPage
            if (document.RootElement.TryGetProperty("mainEntityOfPage", out var mainEntityProp))
            {
                result.Sections = new List<HealthTopicSection>();

                foreach (var section in mainEntityProp.EnumerateArray())
                {
                    // Extract top-level section
                    var topicSection = new HealthTopicSection();

                    if (section.TryGetProperty("headline", out var headlineProp))
                        topicSection.Headline = headlineProp.GetString();

                    if (section.TryGetProperty("text", out var textProp))
                        topicSection.Text = textProp.GetString();

                    if (section.TryGetProperty("description", out var sectionDescProp))
                        topicSection.Description = sectionDescProp.GetString();

                    // Only add sections that have at least one non-empty field
                    if (!string.IsNullOrWhiteSpace(topicSection.Headline) ||
                        !string.IsNullOrWhiteSpace(topicSection.Text) ||
                        !string.IsNullOrWhiteSpace(topicSection.Description))
                    {
                        result.Sections.Add(topicSection);
                    }

                    // Recursively extract from hasPart
                    if (section.TryGetProperty("hasPart", out var hasPartProp) && hasPartProp.ValueKind == JsonValueKind.Array)
                    {
                        ExtractSectionsFromHasPart(hasPartProp, result.Sections);
                    }
                }
            }

            _logger.LogInformation("Successfully retrieved health topic: {Name}", result.Name);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching health topic: {TopicSlug}", topicSlug);
            throw;
        }
    }

    private void ExtractSectionsFromHasPart(JsonElement hasPartArray, List<HealthTopicSection> sections)
    {
        foreach (var part in hasPartArray.EnumerateArray())
        {
            var section = new HealthTopicSection();

            if (part.TryGetProperty("headline", out var headlineProp))
                section.Headline = headlineProp.GetString();

            if (part.TryGetProperty("text", out var textProp))
                section.Text = textProp.GetString();

            if (part.TryGetProperty("description", out var descProp))
                section.Description = descProp.GetString();

            // Only add sections that have at least one non-empty field
            if (!string.IsNullOrWhiteSpace(section.Headline) ||
                !string.IsNullOrWhiteSpace(section.Text) ||
                !string.IsNullOrWhiteSpace(section.Description))
            {
                sections.Add(section);
            }

            // Recursively process nested hasPart
            if (part.TryGetProperty("hasPart", out var nestedHasPart) && nestedHasPart.ValueKind == JsonValueKind.Array)
            {
                ExtractSectionsFromHasPart(nestedHasPart, sections);
            }
        }
    }
}