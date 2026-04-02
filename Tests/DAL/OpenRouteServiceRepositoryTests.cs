using System.Net;
using System.Net.Http.Headers;
using System.Text;
using DAL.Adapter;

namespace Tests.DAL;

[TestFixture]
public sealed class OpenRouteServiceRepositoryTests
{
    [TearDown]
    public void TearDown() => _handler.Dispose();

    private RecordingHttpMessageHandler _handler = null!;
    private Mock<IConfiguration> _configuration = null!;
    private OpenRouteServiceRepository _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _handler = new RecordingHttpMessageHandler();
        _configuration = new Mock<IConfiguration>();
        _configuration.Setup(static config => config["AppSettings:OpenRouteServiceApiKey"]).Returns("test-api-key");
        _configuration.Setup(static config => config["AppSettings:OpenRouteServiceApiBaseUrl"]).Returns("https://api.openrouteservice.org");

        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory.Setup(static factory => factory.CreateClient("OpenRouteService"))
            .Returns(new HttpClient(_handler));

        _sut = new OpenRouteServiceRepository(httpClientFactory.Object, _configuration.Object);
    }

    [TestCase("Car", "driving-car")]
    [TestCase("Bike", "cycling-regular")]
    [TestCase("Foot", "foot-walking")]
    public async Task ResolveRouteAsync_MapsTransportTypeToExpectedEndpoint(string transportType, string expectedEndpoint)
    {
        _handler.ResponseFactory = static () => SuccessfulResponse();

        await _sut.ResolveRouteAsync(TestConstants.TestCoordinates, (52.52, 13.405), transportType);

        Assert.That(_handler.LastRequestUri, Is.EqualTo($"https://api.openrouteservice.org/v2/directions/{expectedEndpoint}"));
    }

    [Test]
    public async Task ResolveRouteAsync_SendsAuthorizationHeaderAndLongitudeLatitudeCoordinateOrder()
    {
        _handler.ResponseFactory = static () => SuccessfulResponse();

        await _sut.ResolveRouteAsync((48.2082, 16.3738), (52.52, 13.405), "Car");

        using var document = JsonDocument.Parse(_handler.LastRequestBody);
        var coordinates = document.RootElement.GetProperty("coordinates");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_handler.LastAuthorization, Is.EqualTo(new AuthenticationHeaderValue("Bearer", "test-api-key")));
            Assert.That(_handler.LastAcceptMediaTypes, Contains.Item("application/json"));
            Assert.That(coordinates[0][0].GetDouble(), Is.EqualTo(16.3738));
            Assert.That(coordinates[0][1].GetDouble(), Is.EqualTo(48.2082));
            Assert.That(coordinates[1][0].GetDouble(), Is.EqualTo(13.405));
            Assert.That(coordinates[1][1].GetDouble(), Is.EqualTo(52.52));
        }
    }

    [Test]
    public async Task ResolveRouteAsync_ValidResponse_ReturnsDistanceAndDuration()
    {
        _handler.ResponseFactory = static () => SuccessfulResponse();

        var (distance, duration) = await _sut.ResolveRouteAsync(TestConstants.TestCoordinates, (52.52, 13.405), "Car");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(distance, Is.EqualTo(523400.0));
            Assert.That(duration, Is.EqualTo(18000.0));
        }
    }

    [Test]
    public void ResolveRouteAsync_UnsupportedTransportType_ThrowsArgumentOutOfRangeException()
    {
        Assert.That(() => _sut.ResolveRouteAsync(TestConstants.TestCoordinates, (52.52, 13.405), "Segway"),
            Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void ResolveRouteAsync_ServerError_ThrowsHttpRequestException()
    {
        _handler.ResponseFactory = () => new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("Server Error", Encoding.UTF8, "text/plain")
        };

        Assert.That(() => _sut.ResolveRouteAsync(TestConstants.TestCoordinates, (52.52, 13.405), "Car"),
            Throws.TypeOf<HttpRequestException>());
    }

    [Test]
    public void ResolveRouteAsync_MissingApiKey_ThrowsInvalidOperationException()
    {
        _configuration.Setup(config => config["AppSettings:OpenRouteServiceApiKey"]).Returns((string?)null);

        Assert.That(() => _sut.ResolveRouteAsync(TestConstants.TestCoordinates, (52.52, 13.405), "Car"),
            Throws.TypeOf<InvalidOperationException>()
                .With.Message.EqualTo("OpenRouteService API key is not configured."));
    }

    [Test]
    public void ResolveRouteAsync_MissingBaseUrl_ThrowsInvalidOperationException()
    {
        _configuration.Setup(config => config["AppSettings:OpenRouteServiceApiBaseUrl"]).Returns((string?)null);

        Assert.That(() => _sut.ResolveRouteAsync(TestConstants.TestCoordinates, (52.52, 13.405), "Car"),
            Throws.TypeOf<InvalidOperationException>()
                .With.Message.EqualTo("OpenRouteService base URL is not configured."));
    }

    private static HttpResponseMessage SuccessfulResponse()
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """
                {
                  "routes": [
                    {
                      "summary": {
                        "distance": 523400.0,
                        "duration": 18000.0
                      }
                    }
                  ]
                }
                """,
                Encoding.UTF8,
                "application/json")
        };
    }

    private sealed class RecordingHttpMessageHandler : HttpMessageHandler
    {
        public Func<HttpResponseMessage> ResponseFactory { get; set; } = SuccessfulResponse;
        public string LastRequestUri { get; private set; } = string.Empty;
        public string LastRequestBody { get; private set; } = string.Empty;
        public AuthenticationHeaderValue? LastAuthorization { get; private set; }
        public List<string> LastAcceptMediaTypes { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequestUri = request.RequestUri!.ToString();
            LastAuthorization = request.Headers.Authorization;
            LastAcceptMediaTypes.Clear();
            LastAcceptMediaTypes.AddRange(request.Headers.Accept.Select(static header => header.MediaType!).Where(static mediaType => mediaType is not null));
            LastRequestBody = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken);

            return ResponseFactory();
        }
    }
}
