using System.Net;
using UI.Service;

namespace Test.UI.Services;

[TestFixture]
public class RouteApiServiceTests
{
    private Mock<HttpMessageHandler> _mockHttpMessageHandler = null!;
    private HttpClient _httpClient = null!;
    private Mock<IConfiguration> _mockConfiguration = null!;
    private RouteApiService _sut = null!;

    [SetUp]
    public void Setup()
    {
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("https://api.openrouteservice.org/")
        };
        _mockConfiguration = TestData.MockConfiguration();
        _sut = new RouteApiService(_httpClient, _mockConfiguration.Object);
    }

    [TearDown]
    public void TearDown()
    {
        _httpClient.Dispose();
    }

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
        const string responseJson = """
                                    {
                                        "routes": [{
                                            "summary": {
                                                "distance": 1000.5,
                                                "duration": 3600.0
                                            }
                                        }]
                                    }
                                    """;

        TestData.SetupHttpMessageHandlerSuccess(_mockHttpMessageHandler, responseJson);

        var result = await _sut.FetchRouteDataAsync(from, to, transportType);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Distance, Is.EqualTo(1000.5));
            Assert.That(result.Duration, Is.EqualTo(3600.0));
        }

        TestData.VerifyHttpPostRequest(_mockHttpMessageHandler, $"v2/directions/{expectedEndpoint}");
    }

    [Test]
    public async Task FetchRouteDataAsync_SuccessfulRequest_ReturnsCorrectData()
    {
        var from = TestData.TestCoordinates;
        var to = (52.5200, 13.4050);
        const string responseJson = """
                                    {
                                        "routes": [{
                                            "summary": {
                                                "distance": 523400.0,
                                                "duration": 18000.0
                                            }
                                        }]
                                    }
                                    """;

        TestData.SetupHttpMessageHandlerSuccess(_mockHttpMessageHandler, responseJson);

        var result = await _sut.FetchRouteDataAsync(from, to, "Car");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Distance, Is.EqualTo(523400.0));
            Assert.That(result.Duration, Is.EqualTo(18000.0));
        }
    }

    [Test]
    public Task FetchRouteDataAsync_HttpError_ThrowsHttpRequestException()
    {
        var from = TestData.TestCoordinates;
        var to = (52.5200, 13.4050);
        TestData.SetupHttpMessageHandlerError(_mockHttpMessageHandler, HttpStatusCode.BadRequest, "Bad Request");

        var ex = Assert.ThrowsAsync<HttpRequestException>(() => _sut.FetchRouteDataAsync(from, to, "Car"));

        Assert.That(ex.Message, Does.Contain("Error fetching route data"));
        Assert.That(ex.Message, Does.Contain("BadRequest"));
        return Task.CompletedTask;
    }

    [Test]
    public Task FetchRouteDataAsync_UnauthorizedError_IncludesErrorContent()
    {
        var from = TestData.TestCoordinates;
        var to = (52.5200, 13.4050);
        TestData.SetupHttpMessageHandlerError(_mockHttpMessageHandler, HttpStatusCode.Unauthorized, "Invalid API key");

        var ex = Assert.ThrowsAsync<HttpRequestException>(() => _sut.FetchRouteDataAsync(from, to, "Car"));

        Assert.That(ex.Message, Does.Contain("Invalid API key"));
        return Task.CompletedTask;
    }

    [Test]
    public async Task FetchRouteDataAsync_SetsCorrectHeaders()
    {
        var from = TestData.TestCoordinates;
        var to = (52.5200, 13.4050);
        const string responseJson = """
                                    {
                                        "routes": [{
                                            "summary": {
                                                "distance": 1000.0,
                                                "duration": 3600.0
                                            }
                                        }]
                                    }
                                    """;

        TestData.SetupHttpMessageHandlerSuccess(_mockHttpMessageHandler, responseJson);

        await _sut.FetchRouteDataAsync(from, to, "Car");

        TestData.VerifyHttpRequestHeaders(_mockHttpMessageHandler, "dummy-api-key");
    }

    [Test]
    public async Task FetchRouteDataAsync_SendsCorrectPayload()
    {
        var from = TestData.TestCoordinates;
        var to = (52.5200, 13.4050);
        const string responseJson = """
                                    {
                                        "routes": [{
                                            "summary": {
                                                "distance": 1000.0,
                                                "duration": 3600.0
                                            }
                                        }]
                                    }
                                    """;

        TestData.SetupHttpMessageHandlerSuccess(_mockHttpMessageHandler, responseJson);

        await _sut.FetchRouteDataAsync(from, to, "Car");

        TestData.VerifyHttpJsonContent(_mockHttpMessageHandler);
    }
}