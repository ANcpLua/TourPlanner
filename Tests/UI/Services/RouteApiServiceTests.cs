using System.Net;
using Contracts.Routes;
using UI.Service;

namespace Tests.UI.Services;

[TestFixture]
public class RouteApiServiceTests
{
    [SetUp]
    public void Setup()
    {
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("https://localhost:7102/")
        };
        _sut = new RouteApiService(_httpClient);
    }

    [TearDown]
    public void TearDown()
    {
        _httpClient.Dispose();
    }

    private Mock<HttpMessageHandler> _mockHttpMessageHandler = null!;
    private HttpClient _httpClient = null!;
    private RouteApiService _sut = null!;

    [TestCase("Car")]
    [TestCase("Bike")]
    [TestCase("Foot")]
    [TestCase("Unknown")]
    [TestCase("")]
    public async Task FetchRouteDataAsync_VariousTransportTypes_UsesBackendEndpoint(string transportType)
    {
        var from = TestData.TestCoordinates;
        var to = (52.5200, 13.4050);
        const string responseJson = """
                                    {
                                        "distance": 1000.5,
                                        "duration": 3600.0
                                    }
                                    """;

        TestData.SetupHttpMessageHandlerSuccess(_mockHttpMessageHandler, responseJson);

        var (distance, duration) = await _sut.FetchRouteDataAsync(from, to, transportType);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(distance, Is.EqualTo(1000.5));
            Assert.That(duration, Is.EqualTo(3600.0));
        }

        TestData.VerifyHttpPostRequest(_mockHttpMessageHandler, "api/routes/resolve");
    }

    [Test]
    public async Task FetchRouteDataAsync_SuccessfulRequest_ReturnsCorrectData()
    {
        var from = TestData.TestCoordinates;
        var to = (52.5200, 13.4050);
        const string responseJson = """
                                    {
                                        "distance": 523400.0,
                                        "duration": 18000.0
                                    }
                                    """;

        TestData.SetupHttpMessageHandlerSuccess(_mockHttpMessageHandler, responseJson);

        var (distance, duration) = await _sut.FetchRouteDataAsync(from, to, "Car");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(distance, Is.EqualTo(523400.0));
            Assert.That(duration, Is.EqualTo(18000.0));
        }
    }

    [Test]
    public Task FetchRouteDataAsync_HttpError_ThrowsHttpRequestException()
    {
        var from = TestData.TestCoordinates;
        var to = (52.5200, 13.4050);
        TestData.SetupHttpMessageHandlerError(_mockHttpMessageHandler, HttpStatusCode.BadRequest, "Bad Request");

        Assert.ThrowsAsync<HttpRequestException>(() => _sut.FetchRouteDataAsync(from, to, "Car"));
        TestData.VerifyHttpPostRequest(_mockHttpMessageHandler, "api/routes/resolve");
        return Task.CompletedTask;
    }

    [Test]
    public Task FetchRouteDataAsync_EmptyContent_ThrowsHttpRequestException()
    {
        try
        {
            var from = TestData.TestCoordinates;
            var to = (52.5200, 13.4050);
            TestData.SetupHttpMessageHandlerSuccess(_mockHttpMessageHandler, "");

            Assert.ThrowsAsync<HttpRequestException>(() => _sut.FetchRouteDataAsync(from, to, "Car"));
            TestData.VerifyHttpPostRequest(_mockHttpMessageHandler, "api/routes/resolve");
            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            return Task.FromException(exception);
        }
    }

    [Test]
    public async Task FetchRouteDataAsync_SendsBackendRouteRequestBody()
    {
        var from = TestData.TestCoordinates;
        var to = (52.5200, 13.4050);
        const string responseJson = """
                                    {
                                        "distance": 1000.0,
                                        "duration": 3600.0
                                    }
                                    """;

        TestData.SetupHttpMessageHandlerSuccess(_mockHttpMessageHandler, responseJson);

        await _sut.FetchRouteDataAsync(from, to, "Car");
        TestData.VerifyHttpJsonContent<ResolveRouteRequest>(_mockHttpMessageHandler, static request =>
            request is
            {
                FromLatitude: 48.2082, FromLongitude: 16.3738, ToLatitude: 52.5200, ToLongitude: 13.4050,
                TransportType: "Car"
            });
    }
}
