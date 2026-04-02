using System.Net;
using System.Net.Http.Json;
using API.Endpoints;
using Contracts.Routes;

namespace Tests.API.Integration;

[TestFixture]
public sealed class RouteApiIntegrationTests : ApiIntegrationTestBase
{
    private Mock<IHttpClientFactory> _httpClientFactory = null!;
    private RecordingRouteHttpMessageHandler _handler = null!;

    [TearDown]
    public void TearDown()
    {
        _handler.Dispose();
    }

    protected override TourPlannerApplication CreateApplication()
    {
        _handler = new RecordingRouteHttpMessageHandler();
        _httpClientFactory = new Mock<IHttpClientFactory>();
        _httpClientFactory.Setup(static factory => factory.CreateClient("OpenRouteService"))
            .Returns(() => new HttpClient(_handler, disposeHandler: false));

        return new TourPlannerApplication(configureServices: services =>
        {
            services.AddSingleton(_httpClientFactory.Object);
        });
    }

    [Test]
    public async Task ResolveRouteAsync_ValidRequest_ReturnsServiceResult()
    {
        await AuthenticateAsync();
        _handler.ResponseFactory = static () => SuccessfulRouteResponse();

        var response = await Client.PostAsJsonAsync(ApiRoute.Routes.Resolve, new ResolveRouteRequest
        {
            FromLatitude = 48.2082,
            FromLongitude = 16.3738,
            ToLatitude = 52.52,
            ToLongitude = 13.405,
            TransportType = "Car"
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), await response.Content.ReadAsStringAsync());
        var body = (await response.Content.ReadFromJsonAsync<ResolveRouteResponse>())!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(body.Distance, Is.EqualTo(523400.0));
            Assert.That(body.Duration, Is.EqualTo(18000.0));
            Assert.That(_handler.LastRequestUri, Is.EqualTo("https://example.test/v2/directions/driving-car"));
        }

        _httpClientFactory.Verify(static factory => factory.CreateClient("OpenRouteService"), Times.Once);
    }

    [Test]
    public async Task ResolveRouteAsync_InvalidPayload_ReturnsValidationProblemWithoutCallingService()
    {
        await AuthenticateAsync();

        var response = await Client.PostAsJsonAsync(ApiRoute.Routes.Resolve, new
        {
            fromLatitude = 48.2082,
            fromLongitude = 16.3738,
            toLatitude = 52.52,
            toLongitude = 13.405
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        _httpClientFactory.Verify(static factory => factory.CreateClient("OpenRouteService"), Times.Never);
    }

    private static HttpResponseMessage SuccessfulRouteResponse()
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

    private sealed class RecordingRouteHttpMessageHandler : HttpMessageHandler
    {
        public Func<HttpResponseMessage> ResponseFactory { get; set; } = SuccessfulRouteResponse;
        public string LastRequestUri { get; private set; } = string.Empty;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequestUri = request.RequestUri!.ToString();
            return Task.FromResult(ResponseFactory());
        }
    }
}
