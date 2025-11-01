using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using NHSUKMCP.Models;
using NHSUKMCP.Services;
using System.Net;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace NHSUKMCPServer.Tests;

/// <summary>
/// Unit tests for AzureSearchService
/// </summary>
public class AzureSearchServiceTests
{
private readonly ITestOutputHelper _output;
    private readonly Mock<ILogger<AzureSearchService>> _mockLogger;
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly HttpClient _httpClient;
    private readonly AzureSearchConfig _config;

    public AzureSearchServiceTests(ITestOutputHelper output)
    {
        _output = output;
        _mockLogger = new Mock<ILogger<AzureSearchService>>();
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object);
        _config = new AzureSearchConfig
        {
        Endpoint = "https://test-api.example.com/service-search",
            ApiKey = "test-api-key"
        };
 }

    [Fact]
    public async Task GetPostcodeCoordinatesAsync_WithValidPostcode_ReturnsCoordinates()
    {
   // Arrange
        var postcode = "SW1A1AA";
        var responseContent = JsonSerializer.Serialize(new
        {
            value = new[]
  {
     new { Latitude = 51.5014, Longitude = -0.1419, Postcode = "SW1A 1AA" }
            }
        });

        _mockHttpMessageHandler
       .Protected()
       .Setup<Task<HttpResponseMessage>>(
    "SendAsync",
  ItExpr.IsAny<HttpRequestMessage>(),
           ItExpr.IsAny<CancellationToken>()
          )
            .ReturnsAsync(new HttpResponseMessage
    {
      StatusCode = HttpStatusCode.OK,
     Content = new StringContent(responseContent)
            });

    var service = new AzureSearchService(_httpClient, _config, _mockLogger.Object);

        // Act
        var result = await service.GetPostcodeCoordinatesAsync(postcode);

        // Assert
        result.Should().NotBeNull();
     result!.Latitude.Should().BeApproximately(51.5014, 0.0001);
        result.Longitude.Should().BeApproximately(-0.1419, 0.0001);
    }

    [Fact]
    public async Task GetPostcodeCoordinatesAsync_WithInvalidPostcode_ReturnsNull()
    {
 // Arrange
        var postcode = "INVALID";
        var responseContent = JsonSerializer.Serialize(new { value = Array.Empty<object>() });

        _mockHttpMessageHandler
     .Protected()
        .Setup<Task<HttpResponseMessage>>(
  "SendAsync",
     ItExpr.IsAny<HttpRequestMessage>(),
              ItExpr.IsAny<CancellationToken>()
            )
   .ReturnsAsync(new HttpResponseMessage
   {
      StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseContent)
            });

        var service = new AzureSearchService(_httpClient, _config, _mockLogger.Object);

 // Act
        var result = await service.GetPostcodeCoordinatesAsync(postcode);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task SearchOrganisationsAsync_WithValidParameters_ReturnsOrganisations()
    {
        // Arrange
  var organisationType = "PHA";
        var latitude = 51.5074;
        var longitude = -0.1278;
        var responseContent = JsonSerializer.Serialize(new
     {
     value = new[]
   {
        new
        {
         OrganisationName = "Test Pharmacy",
     OrganisationTypeId = "PHA",
      ODSCode = "FA123",
           Address1 = "123 Test St",
         City = "London",
   Postcode = "SW1A 1AA",
         Latitude = 51.5014,
            Longitude = -0.1419
  }
            }
});

        _mockHttpMessageHandler
   .Protected()
  .Setup<Task<HttpResponseMessage>>(
       "SendAsync",
ItExpr.IsAny<HttpRequestMessage>(),
     ItExpr.IsAny<CancellationToken>()
            )
          .ReturnsAsync(new HttpResponseMessage
    {
                StatusCode = HttpStatusCode.OK,
      Content = new StringContent(responseContent)
         });

        var service = new AzureSearchService(_httpClient, _config, _mockLogger.Object);

        // Act
        var results = await service.SearchOrganisationsAsync(organisationType, latitude, longitude, 10);

        // Assert
        results.Should().NotBeNull();
        results.Should().HaveCount(1);
 results[0].OrganisationName.Should().Be("Test Pharmacy");
        results[0].OrganisationTypeID.Should().Be("PHA");
        results[0].Distance.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetHealthTopicAsync_WithValidTopic_ReturnsHealthTopic()
    {
        // Arrange
      var topic = "diabetes";
        var responseContent = JsonSerializer.Serialize(new
        {
     name = "Type 2 diabetes",
            description = "Information about type 2 diabetes",
      url = "https://www.nhs.uk/conditions/type-2-diabetes/",
 dateModified = "2024-01-15",
  lastReviewed = new[] { "2024-01-15" },
     genre = new[] { "Condition" },
mainEntityOfPage = new[]
            {
         new
        {
           headline = "What is type 2 diabetes?",
      text = "Type 2 diabetes is a common condition...",
       description = "Overview of type 2 diabetes"
  }
       }
        });

 _mockHttpMessageHandler
            .Protected()
        .Setup<Task<HttpResponseMessage>>(
           "SendAsync",
    ItExpr.Is<HttpRequestMessage>(req =>
    req.RequestUri != null &&
  req.RequestUri.ToString().Contains("/conditions/diabetes")),
         ItExpr.IsAny<CancellationToken>()
       )
      .ReturnsAsync(new HttpResponseMessage
          {
           StatusCode = HttpStatusCode.OK,
      Content = new StringContent(responseContent)
            });

        var service = new AzureSearchService(_httpClient, _config, _mockLogger.Object);

    // Act
     var result = await service.GetHealthTopicAsync(topic);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Type 2 diabetes");
        result.Description.Should().Contain("type 2 diabetes");
  result.Sections.Should().NotBeNull();
        result.Sections.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public async Task GetHealthTopicAsync_WithInvalidTopic_ReturnsNull()
    {
// Arrange
        var topic = "nonexistenttopic";

        _mockHttpMessageHandler
            .Protected()
  .Setup<Task<HttpResponseMessage>>(
              "SendAsync",
      ItExpr.IsAny<HttpRequestMessage>(),
    ItExpr.IsAny<CancellationToken>()
        )
      .ReturnsAsync(new HttpResponseMessage
     {
 StatusCode = HttpStatusCode.NotFound
       });

      var service = new AzureSearchService(_httpClient, _config, _mockLogger.Object);

        // Act
        var result = await service.GetHealthTopicAsync(topic);

        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [InlineData("SW1A 1AA")]
    [InlineData("sw1a1aa")]
    [InlineData("SW1A1AA")]
    public async Task GetPostcodeCoordinatesAsync_WithDifferentFormats_NormalizesCorrectly(string postcode)
    {
      // Arrange
        var responseContent = JsonSerializer.Serialize(new
   {
            value = new[]
  {
          new { Latitude = 51.5014, Longitude = -0.1419 }
            }
        });

string? capturedUri = null;
        _mockHttpMessageHandler
            .Protected()
.Setup<Task<HttpResponseMessage>>(
         "SendAsync",
            ItExpr.IsAny<HttpRequestMessage>(),
     ItExpr.IsAny<CancellationToken>()
            )
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedUri = req.RequestUri?.ToString())
          .ReturnsAsync(new HttpResponseMessage
     {
StatusCode = HttpStatusCode.OK,
       Content = new StringContent(responseContent)
});

     var service = new AzureSearchService(_httpClient, _config, _mockLogger.Object);

        // Act
        await service.GetPostcodeCoordinatesAsync(postcode);

   // Assert
        capturedUri.Should().NotBeNull();
  capturedUri.Should().Contain("SW1A1AA"); // Should be normalized (no spaces, uppercase)
    }
}
