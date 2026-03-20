using System.Net;
using DAL.Adapter;

namespace Tests.DAL;

[TestFixture]
public class OpenRouteServiceRepositoryTests
{
    [SetUp]
    public void Setup()
    {
        _mockHandler = new Mock<HttpMessageHandler>();
        _mockConfig = new Mock<IConfiguration>();
        _mockConfig.Setup(static c => c["AppSettings:OpenRouteServiceApiKey"]).Returns("test-api-key");
        _mockConfig.Setup(static c => c["AppSettings:OpenRouteServiceApiBaseUrl"]).Returns("https://api.openrouteservice.org");

        var httpClient = new HttpClient(_mockHandler.Object);
        var mockFactory = new Mock<IHttpClientFactory>();
        mockFactory.Setup(static f => f.CreateClient("OpenRouteService")).Returns(httpClient);
        _sut = new OpenRouteServiceRepository(mockFactory.Object, _mockConfig.Object);
    }

    private Mock<HttpMessageHandler> _mockHandler = null!;
    private Mock<IConfiguration> _mockConfig = null!;
    private OpenRouteServiceRepository _sut = null!;

    private const string ValidRouteResponse = """
        {
            "routes": [{
                "summary": {
                    "distance": 523400.0,
                    "duration": 18000.0
                }
            }]
        }
        """;

    [TestCase("Car", "driving-car")]
    [TestCase("Bike", "cycling-regular")]
    [TestCase("Foot", "foot-walking")]
    public async Task ResolveRouteAsync_TransportTypes_MapsToCorrectEndpoint(string transportType, string expectedEndpoint)
    {
        TestData.SetupHttpMessageHandlerSuccess(_mockHandler, ValidRouteResponse);

        await _sut.ResolveRouteAsync(TestData.TestCoordinates, (52.52, 13.405), transportType);

        TestData.VerifyHttpPostRequest(_mockHandler, $"v2/directions/{expectedEndpoint}");
    }

    [Test]
    public async Task ResolveRouteAsync_ValidResponse_ReturnsDistanceAndDuration()
    {
        TestData.SetupHttpMessageHandlerSuccess(_mockHandler, ValidRouteResponse);

        var (distance, duration) = await _sut.ResolveRouteAsync(
            TestData.TestCoordinates, (52.52, 13.405), "Car");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(distance, Is.EqualTo(523400.0));
            Assert.That(duration, Is.EqualTo(18000.0));
        }
    }

    [Test]
    public async Task ResolveRouteAsync_SetsAuthorizationAndAcceptHeaders()
    {
        TestData.SetupHttpMessageHandlerSuccess(_mockHandler, ValidRouteResponse);

        await _sut.ResolveRouteAsync(TestData.TestCoordinates, (52.52, 13.405), "Car");

        TestData.VerifyHttpRequestHeaders(_mockHandler, "test-api-key");
    }

    [Test]
    public async Task ResolveRouteAsync_PostsToCorrectEndpoint()
    {
        TestData.SetupHttpMessageHandlerSuccess(_mockHandler, ValidRouteResponse);

        await _sut.ResolveRouteAsync((48.2082, 16.3738), (52.52, 13.405), "Car");

        TestData.VerifyHttpPostRequest(_mockHandler, "v2/directions/driving-car");
    }

    [Test]
    public void ResolveRouteAsync_UnsupportedTransportType_ThrowsArgumentOutOfRangeException()
    {
        Assert.That(
            () => _sut.ResolveRouteAsync(TestData.TestCoordinates, (52.52, 13.405), "Segway"),
            Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void ResolveRouteAsync_ServerError_ThrowsHttpRequestException()
    {
        TestData.SetupHttpMessageHandlerError(_mockHandler, HttpStatusCode.InternalServerError, "Server Error");

        Assert.That(
            () => _sut.ResolveRouteAsync(TestData.TestCoordinates, (52.52, 13.405), "Car"),
            Throws.TypeOf<HttpRequestException>());
    }

    [Test]
    public void ResolveRouteAsync_MissingApiKey_ThrowsInvalidOperationException()
    {
        _mockConfig.Setup(static c => c["AppSettings:OpenRouteServiceApiKey"]).Returns((string?)null);

        Assert.That(
            () => _sut.ResolveRouteAsync(TestData.TestCoordinates, (52.52, 13.405), "Car"),
            Throws.TypeOf<InvalidOperationException>());
    }

    [Test]
    public void ResolveRouteAsync_MissingBaseUrl_ThrowsInvalidOperationException()
    {
        _mockConfig.Setup(static c => c["AppSettings:OpenRouteServiceApiBaseUrl"]).Returns((string?)null);

        Assert.That(
            () => _sut.ResolveRouteAsync(TestData.TestCoordinates, (52.52, 13.405), "Car"),
            Throws.TypeOf<InvalidOperationException>());
    }
}
