using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using NHSUKMCP.Models;
using Xunit;
using Xunit.Abstractions;

namespace NHSUKMCPServer.Tests;

/// <summary>
/// Integration tests for the deployed Azure Function MCP Server
/// </summary>
public class AzureFunctionIntegrationTests : IDisposable
{
    private readonly HttpClient _httpClient;
  private readonly ITestOutputHelper _output;
    private const string BaseUrl = "https://nhsuk-mcp-server-func.azurewebsites.net";

    public AzureFunctionIntegrationTests(ITestOutputHelper output)
  {
        _output = output;
        _httpClient = new HttpClient
        {
       BaseAddress = new Uri(BaseUrl),
     Timeout = TimeSpan.FromSeconds(30)
        };
    }

    [Fact]
    public async Task GetContentAsync_WithValidTopic_ReturnsHealthContent()
    {
        // Arrange
        var topic = "diabetes";

        // Act
        var response = await _httpClient.PostAsJsonAsync("/api/GetContent", new { topic });
        var content = await response.Content.ReadAsStringAsync();
    _output.WriteLine($"Status: {response.StatusCode}");
     _output.WriteLine($"Response: {content}");

        // Assert
     response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().NotBeNullOrEmpty();
      
        var result = JsonSerializer.Deserialize<HealthTopicResult>(content);
        result.Should().NotBeNull();
  result!.Name.Should().NotBeNullOrEmpty();
  result.Description.Should().NotBeNullOrEmpty();
        result.Url.Should().Contain("diabetes");
    }

    [Fact]
    public async Task GetContentAsync_WithInvalidTopic_ReturnsError()
    {
        // Arrange
        var topic = "nonexistenthealthtopic12345";

     // Act
        var response = await _httpClient.PostAsJsonAsync("/api/GetContent", new { topic });
        var content = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"Status: {response.StatusCode}");
        _output.WriteLine($"Response: {content}");

        // Assert
      response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        content.Should().Contain("not found");
    }

    [Fact]
    public async Task GetOrganisationTypes_ReturnsAllTypes()
    {
        // Act
      var response = await _httpClient.PostAsync("/api/GetOrganisationTypes", null);
     var content = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"Status: {response.StatusCode}");
        _output.WriteLine($"Response: {content}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().NotBeNullOrEmpty();
      
        var result = JsonSerializer.Deserialize<Dictionary<string, string>>(content);
        result.Should().NotBeNull();
      result.Should().ContainKey("PHA").WhoseValue.Should().Be("Pharmacy");
        result.Should().ContainKey("GPB");
    result.Should().ContainKey("HOS");
        result.Should().ContainKey("DEN");
     result.Should().HaveCountGreaterThan(10);
    }

    [Fact]
 public async Task ConvertPostcodeToCoordinates_WithValidPostcode_ReturnsCoordinates()
    {
        // Arrange
        var postcode = "SW1A 1AA"; // Buckingham Palace postcode

        // Act
        var response = await _httpClient.PostAsJsonAsync("/api/ConvertPostcode", new { postcode });
        var content = await response.Content.ReadAsStringAsync();
     _output.WriteLine($"Status: {response.StatusCode}");
        _output.WriteLine($"Response: {content}");

        // Assert
   response.StatusCode.Should().Be(HttpStatusCode.OK);
 content.Should().NotBeNullOrEmpty();
    
  var result = JsonSerializer.Deserialize<JsonElement>(content);
     result.GetProperty("postcode").GetString().Should().Be(postcode);
    result.GetProperty("latitude").GetDouble().Should().BeInRange(51.0, 52.0);
        result.GetProperty("longitude").GetDouble().Should().BeInRange(-1.0, 0.5);
  }

    [Fact]
    public async Task ConvertPostcodeToCoordinates_WithInvalidPostcode_ReturnsError()
    {
        // Arrange
        var postcode = "INVALID123";

    // Act
        var response = await _httpClient.PostAsJsonAsync("/api/ConvertPostcode", new { postcode });
        var content = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"Status: {response.StatusCode}");
        _output.WriteLine($"Response: {content}");

        // Assert
  response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        content.Should().Contain("not found");
    }

    [Fact]
    public async Task SearchOrgsByPostcode_WithValidParameters_ReturnsOrganisations()
    {
        // Arrange
   var postcode = "M1 1AE"; // Manchester city center
  var organisationType = "PHA"; // Pharmacy

        // Act
        var response = await _httpClient.PostAsJsonAsync("/api/SearchOrganisations", new 
  { 
            postcode, 
            organisationType,
  maxResults = 5
        });
        var content = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"Status: {response.StatusCode}");
        _output.WriteLine($"Response: {content}");

   // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().NotBeNullOrEmpty();
        
      var result = JsonSerializer.Deserialize<JsonElement>(content);
        result.GetProperty("postcode").GetString().Should().Be(postcode);
        result.GetProperty("organisationType").GetString().Should().Be(organisationType);
    
        var organisations = result.GetProperty("organisations");
        organisations.GetArrayLength().Should().BeGreaterThan(0);
        organisations.GetArrayLength().Should().BeLessThanOrEqualTo(5);
   
     // Check first organisation has required fields
        var firstOrg = organisations[0];
      firstOrg.GetProperty("OrganisationName").GetString().Should().NotBeNullOrEmpty();
        firstOrg.GetProperty("Distance").GetDouble().Should().BeGreaterThanOrEqualTo(0);
  }

    [Fact]
    public async Task SearchOrgsByPostcode_WithInvalidOrganisationType_ReturnsError()
    {
        // Arrange
        var postcode = "M1 1AE";
        var organisationType = "INVALID";

        // Act
        var response = await _httpClient.PostAsJsonAsync("/api/SearchOrganisations", new 
        { 
            postcode, 
   organisationType
        });
        var content = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"Status: {response.StatusCode}");
        _output.WriteLine($"Response: {content}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        content.Should().Contain("Invalid organisation type");
    }

    [Theory]
    [InlineData("asthma")]
[InlineData("flu")]
    [InlineData("covid-19")]
    public async Task GetContentAsync_WithVariousTopics_ReturnsContent(string topic)
  {
        // Act
        var response = await _httpClient.PostAsJsonAsync("/api/GetContent", new { topic });
        var content = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"Topic: {topic}");
     _output.WriteLine($"Status: {response.StatusCode}");
        _output.WriteLine($"Response Length: {content.Length}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData("PHA", "Pharmacy")]
  [InlineData("GPB", "GP")]
    [InlineData("HOS", "Hospital")]
    [InlineData("DEN", "Dentists")]
    public async Task SearchOrgsByPostcode_WithDifferentTypes_ReturnsResults(string orgType, string expectedDescription)
    {
        // Arrange
        var postcode = "LS1 1UR"; // Leeds city center

        // Act
        var response = await _httpClient.PostAsJsonAsync("/api/SearchOrganisations", new 
        { 
 postcode, 
  organisationType = orgType,
       maxResults = 3
    });
  var content = await response.Content.ReadAsStringAsync();
    _output.WriteLine($"Type: {orgType}");
        _output.WriteLine($"Status: {response.StatusCode}");

     // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = JsonSerializer.Deserialize<JsonElement>(content);
result.GetProperty("organisationType").GetString().Should().Be(orgType);
        result.GetProperty("organisationTypeDescription").GetString().Should().Contain(expectedDescription);
    }

    public void Dispose()
    {
 _httpClient?.Dispose();
    }
}
