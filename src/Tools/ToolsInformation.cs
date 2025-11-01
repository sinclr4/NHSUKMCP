namespace NHSUKMCPServer.Tools;

internal sealed class ToolsInformation
{
    //public const string SaveSnippetToolName = "save_snippet";
    //public const string SaveSnippetToolDescription =
    //    "Saves a code snippet into your snippet collection.";
    //public const string GetSnippetToolName = "get_snippets";
    //public const string GetSnippetToolDescription =
    //    "Gets code snippets from your snippet collection.";
    //public const string SnippetNamePropertyName = "snippetname";
    //public const string SnippetPropertyName = "snippet";
    //public const string SnippetNamePropertyDescription = "The name of the snippet.";
    //public const string SnippetPropertyDescription = "The code snippet.";
    //public const string PropertyType = "string";
    //public const string HelloToolName = "hello";
    //public const string HelloToolDescription =
    //    "Simple hello world MCP Tool that responses with a hello message.";
    public const string GetContentToolName = "get_content";
    public const string GetContentToolDescription =
        "Simple way to get an article from nhs.uk";
    public const string ArticlePropertyName = "topic";
    public const string ArticlePropertyDescription = "The name of the topic to retrieve.";
    public const string GetOrganisationsToolName = "get_organisation_types";
    public const string GetOrganisationsToolDescription =
        "Get a list of all available NHS organisation types.";
    public const string ConvertPostcodeToolName = "convert_postcode_to_coordinates";
    public const string ConvertPostcodeToolDescription =
        "Convert a UK postcode to latitude and longitude coordinates.";
    public const string LocationPropertyName = "location";
    public const string LocationPropertyDescription = "The location, place name or postcode to convert.";
    public const string SearchOrgsByPostcodeToolName = "search_organisations_by_postcode";
    public const string SearchOrgsByPostcodeToolDescription =
      "Search for NHS organisations near a UK postcode. Returns organisations sorted by distance.";
    public const string SearchOrgsTypePropertyName = "organisation_type";
    public const string SearchOrgsTypePropertyDescription = "The type of organisation to search for.";
}
