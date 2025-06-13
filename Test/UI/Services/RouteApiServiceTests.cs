using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;
using UI.Service;

namespace Test.UI.Services;

[TestFixture]
public class RouteApiServiceTests
{
    [SetUp]
    public void Setup()
    {
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("https://api.openrouteservice.org/")
        };
        _mockConfiguration = TestData.MockConfiguration();
        _routeApiService = new RouteApiService(_httpClient, _mockConfiguration.Object);
    }

    [TearDown]
    public void TearDown()
    {
        _httpClient.Dispose();
    }

    private Mock<HttpMessageHandler> _mockHttpMessageHandler = null!;
    private HttpClient _httpClient = null!;
    private Mock<IConfiguration> _mockConfiguration = null!;
    private RouteApiService _routeApiService = null!;

    [TestCase("Car", "driving-car")]
    [TestCase("Bike", "cycling-regular")]
    [TestCase("Foot", "foot-walking")]
    [TestCase("Unknown", "driving-car")]
    [TestCase("", "driving-car")]
    public async Task FetchRouteDataAsync_VariousTransportTypes_UsesCorrectEndpoint(string transportType,
        string expectedEndpoint)
    {
        var from = TestData.TestCoordinates;
        var to = (52.5200, 13.4050);
        var responseJson = """
                           {
                               "routes": [{
                                   "summary": {
                                       "distance": 1000.5,
                                       "duration": 3600.0
                                   }
                               }]
                           }
                           """;

        SetupSuccessfulHttpResponse(responseJson);

        var result = await _routeApiService.FetchRouteDataAsync(from, to, transportType);

        Assert.Multiple(() =>
        {
            Assert.That(result.Distance, Is.EqualTo(1000.5));
            Assert.That(result.Duration, Is.EqualTo(3600.0));
        });

        VerifyHttpPostRequest($"v2/directions/{expectedEndpoint}");
    }

    [Test]
    public async Task FetchRouteDataAsync_SuccessfulRequest_ReturnsCorrectData()
    {
        var from = TestData.TestCoordinates;
        var to = (52.5200, 13.4050);
        var responseJson = """
                           {
                               "routes": [{
                                   "summary": {
                                       "distance": 523400.0,
                                       "duration": 18000.0
                                   }
                               }]
                           }
                           """;

        SetupSuccessfulHttpResponse(responseJson);

        var result = await _routeApiService.FetchRouteDataAsync(from, to, "Car");

        Assert.Multiple(() =>
        {
            Assert.That(result.Distance, Is.EqualTo(523400.0));
            Assert.That(result.Duration, Is.EqualTo(18000.0));
        });
    }

    [Test]
    public Task FetchRouteDataAsync_HttpError_ThrowsHttpRequestException()
    {
        var from = TestData.TestCoordinates;
        var to = (52.5200, 13.4050);
        SetupErrorHttpResponse(HttpStatusCode.BadRequest, "Bad Request");

        var ex = Assert.ThrowsAsync<HttpRequestException>(() => _routeApiService.FetchRouteDataAsync(from, to, "Car"));

        Assert.That(ex.Message, Does.Contain("Error fetching route data"));
        Assert.That(ex.Message, Does.Contain("BadRequest"));
        return Task.CompletedTask;
    }

    [Test]
    public Task FetchRouteDataAsync_UnauthorizedError_IncludesErrorContent()
    {
        var from = TestData.TestCoordinates;
        var to = (52.5200, 13.4050);
        SetupErrorHttpResponse(HttpStatusCode.Unauthorized, "Invalid API key");

        var ex = Assert.ThrowsAsync<HttpRequestException>(() => _routeApiService.FetchRouteDataAsync(from, to, "Car"));

        Assert.That(ex.Message, Does.Contain("Invalid API key"));
        return Task.CompletedTask;
    }

    [Test]
    public async Task FetchRouteDataAsync_SetsCorrectHeaders()
    {
        var from = TestData.TestCoordinates;
        var to = (52.5200, 13.4050);
        var responseJson = """
                           {
                               "routes": [{
                                   "summary": {
                                       "distance": 1000.0,
                                       "duration": 3600.0
                                   }
                               }]
                           }
                           """;

        SetupSuccessfulHttpResponse(responseJson);

        await _routeApiService.FetchRouteDataAsync(from, to, "Car");

        VerifyHttpRequestWithHeaders();
    }

    [Test]
    public async Task FetchRouteDataAsync_SendsCorrectPayload()
    {
        var from = TestData.TestCoordinates;
        var to = (52.5200, 13.4050);
        var responseJson = """
                           {
                               "routes": [{
                                   "summary": {
                                       "distance": 1000.0,
                                       "duration": 3600.0
                                   }
                               }]
                           }
                           """;

        SetupSuccessfulHttpResponse(responseJson);

        await _routeApiService.FetchRouteDataAsync(from, to, "Car");

        VerifyPayloadContent();
    }

    private void SetupSuccessfulHttpResponse(string jsonContent)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
    }

    private void SetupErrorHttpResponse(HttpStatusCode statusCode, string content)
    {
        var response = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(content, Encoding.UTF8, "text/plain")
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
    }

    private void VerifyHttpPostRequest(string expectedEndpoint)
    {
        _mockHttpMessageHandler
            .Protected()
            .Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri!.ToString().Contains(expectedEndpoint)),
                ItExpr.IsAny<CancellationToken>());
    }

    private void VerifyHttpRequestWithHeaders()
    {
        _mockHttpMessageHandler
            .Protected()
            .Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Headers.Authorization != null &&
                    req.Headers.Authorization.Scheme == "Bearer" &&
                    req.Headers.Authorization.Parameter == "dummy-api-key" &&
                    req.Headers.Accept.Any(h => h.MediaType == "application/json")),
                ItExpr.IsAny<CancellationToken>());
    }

    private void VerifyPayloadContent()
    {
        _mockHttpMessageHandler
            .Protected()
            .Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Content != null &&
                    req.Content.Headers.ContentType!.MediaType == "application/json"),
                ItExpr.IsAny<CancellationToken>());
    }
}