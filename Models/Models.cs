namespace NHSOrgsMCP.Models;

/// <summary>
/// Configuration for Azure Search integration
/// </summary>
public class AzureSearchConfig
{
    public string ServiceName { get; set; } = "nhsuksearchintuks";
    public string Endpoint { get; set; } = "https://nhsuksearchintuks.search.windows.net";
    public string ApiKey { get; set; } = "REDACTED_API_KEY";
    public string ServiceSearchIndex { get; set; } = "service-search-internal-3-11";
    public string PostcodeIndex { get; set; } = "postcodesandplaces-1-0-b-int";
}

/// <summary>
/// NHS Organization type mappings
/// </summary>
public static class OrganizationTypes
{
    public static readonly Dictionary<string, string> Types = new()
    {
        { "CCG", "Clinical Commissioning Group" },
        { "CLI", "Clinics" },
        { "DEN", "Dentists" },
        { "GDOS", "Generic Directory of Services" },
        { "GPB", "GP" },
        { "GPP", "GP Practice" },
        { "GSD", "Generic Service Directory" },
        { "HA", "Health Authority" },
        { "HOS", "Hospital" },
        { "HWB", "Health and Wellbeing Board" },
        { "LA", "Local Authority" },
        { "LAT", "Area Team" },
        { "MIU", "Minor Injury Unit" },
        { "OPT", "Optician" },
        { "PHA", "Pharmacy" },
        { "RAT", "Regional Area Team" },
        { "SCL", "Social Care Provider Location" },
        { "SCP", "Social Care Provider" },
        { "SHA", "Strategic Health Authority" },
        { "STP", "Sustainability and Transformation Partnership" },
        { "TRU", "Trust" },
        { "UC", "Urgent Care" },
        { "UNK", "UNKNOWN" }
    };
}

/// <summary>
/// Search request model for Azure Search
/// </summary>
public class SearchRequest
{
    public string Search { get; set; } = "*";
    public string? Filter { get; set; }
    public string SearchMode { get; set; } = "all";
    public string? OrderBy { get; set; }
    public int Top { get; set; } = 10;
    public bool Count { get; set; } = true;
}

/// <summary>
/// Postcode search result
/// </summary>
public class PostcodeResult
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? Postcode { get; set; }
}

/// <summary>
/// Organization search result
/// </summary>
public class OrganizationResult
{
    public string? OrganizationName { get; set; }
    public string? OrganizationTypeID { get; set; }
    public string? ODSCode { get; set; }
    public string? Address { get; set; }
    public string? Postcode { get; set; }
    public double Distance { get; set; }
    public PostcodeResult? Geocode { get; set; }
}