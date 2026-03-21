using System.Net;
using System.Net.Http.Json;
using BL.DomainModel;
using BL.Interface;
using Contracts.TourLogs;
using Contracts.Tours;
using DAL.PersistenceModel;
using UI.Decorator;
using UI.Model;
using UI.Service.Interface;
using UI.ViewModel;

namespace Tests;

public static class TestData
{
    public const string TestUserId = "test-user-id-12345";
    public const string ValidSearchText = "Sample Tour";
    public const string InvalidSearchText = "NonexistentTour";
    public static readonly Guid TestGuid = new("11111111-1111-1111-1111-111111111111");
    public static readonly Guid NonexistentGuid = new("99999999-9999-9999-9999-999999999999");

    private static readonly DateTime TestDateTime = new(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
    public static readonly (double Latitude, double Longitude) TestCoordinates = (48.2082, 16.3738);

    public static Mock<IUserContext> MockUserContext()
    {
        var mock = new Mock<IUserContext>();
        mock.Setup(static u => u.UserId).Returns(TestUserId);
        return mock;
    }

    public static Mock<ILogger> MockLogger()
    {
        return new Mock<ILogger>();
    }

    public static Mock<IJSRuntime> MockJsRuntime()
    {
        return new Mock<IJSRuntime>();
    }

    public static Mock<IBlazorDownloadFileService> MockBlazorDownloadFileService()
    {
        return new Mock<IBlazorDownloadFileService>();
    }

    public static Mock<IRouteApiService> MockRouteApiService()
    {
        return new Mock<IRouteApiService>();
    }

    public static Mock<IToastServiceWrapper> MockToastService()
    {
        var mock = new Mock<IToastServiceWrapper>();
        mock.Setup(static t => t.ShowSuccess(It.IsAny<string>()));
        mock.Setup(static t => t.ShowError(It.IsAny<string>()));
        return mock;
    }

    public static Mock<IConfiguration> MockConfiguration()
    {
        var mock = new Mock<IConfiguration>();
        mock.Setup(static c => c["AppSettings:ImageBasePath"]).Returns("/images/");
        return mock;
    }

    public static (HttpClient Client, Mock<HttpMessageHandler> Handler) MockedHttpClient()
    {
        var handler = new Mock<HttpMessageHandler>();
        SetupHttpMessageHandlerSuccess(handler, "[]");
        var client = new HttpClient(handler.Object) { BaseAddress = new Uri("https://test.invalid/") };
        return (client, handler);
    }

    public static void SetupHandler(
        Mock<HttpMessageHandler> handler,
        HttpMethod method,
        string urlContains,
        object responseBody,
        HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        handler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == method &&
                    req.RequestUri!.ToString().Contains(urlContains)),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(statusCode)
            {
                Content = responseBody is string s
                    ? new StringContent(s, Encoding.UTF8, "application/json")
                    : JsonContent.Create(responseBody, responseBody.GetType())
            });
    }

    public static void SetupHandlerBytes(
        Mock<HttpMessageHandler> handler,
        string urlContains,
        byte[] content,
        HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        handler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri!.ToString().Contains(urlContains)),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(statusCode)
            {
                Content = new ByteArrayContent(content)
            });
    }

    public static void VerifyHandler(
        Mock<HttpMessageHandler> handler,
        HttpMethod method,
        string urlContains,
        Times times)
    {
        handler
            .Protected()
            .Verify(
                "SendAsync",
                times,
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == method &&
                    req.RequestUri!.ToString().Contains(urlContains)),
                ItExpr.IsAny<CancellationToken>());
    }

    public static Mock<MapViewModel> MockMapViewModel()
    {
        return new Mock<MapViewModel>(
            MockJsRuntime().Object,
            new HttpClient(),
            MockToastService().Object,
            MockTryCatchToastWrapper())
        {
            DefaultValue = DefaultValue.Mock,
            CallBase = false
        };
    }

    public static TryCatchToastWrapper MockTryCatchToastWrapper(IToastServiceWrapper? toastService = null)
    {
        return new TryCatchToastWrapper(toastService ?? MockToastService().Object, MockLogger().Object);
    }

    public static Tour SampleTour(
        string name = "Sample Tour",
        Guid? id = null,
        string from = "City1",
        string to = "City2",
        string description = "Sample tour for testing",
        string transportType = "Car",
        double? distance = 100.5,
        double? estimatedTime = 60.0,
        string? imagePath = "/images/sample.png",
        string? routeInformation = "Sample route information",
        List<TourLog>? tourLogs = null)
    {
        return new Tour
        {
            Id = id ?? TestGuid,
            Name = name,
            Description = description,
            From = from,
            To = to,
            Distance = distance,
            EstimatedTime = estimatedTime,
            TransportType = transportType,
            ImagePath = imagePath,
            RouteInformation = routeInformation,
            TourLogs = tourLogs ?? []
        };
    }

    public static List<Tour> SampleTourList(int count = 5)
    {
        return [.. Enumerable.Range(1, count).Select(static i => SampleTour($"Tour {i}", Guid.NewGuid()))];
    }

    public static TourDto SampleTourDto(string name = "Sample Tour", Guid? id = null)
    {
        return new TourDto
        {
            Id = id ?? TestGuid,
            Name = name,
            Description = "Sample tour for testing",
            From = "City1",
            To = "City2",
            Distance = 100.5,
            EstimatedTime = 60.0,
            TransportType = "Car",
            ImagePath = "/images/sample.png",
            RouteInformation = "Sample route information",
            TourLogs = []
        };
    }

    public static List<TourDto> SampleTourDtoList(int count = 5)
    {
        return [.. Enumerable.Range(1, count).Select(static i => SampleTourDto($"Tour {i}", Guid.NewGuid()))];
    }

    public static TourLog SampleTourLog(double? rating = 4, double difficulty = 3, Guid? tourId = null, Guid? id = null)
    {
        return new TourLog
        {
            Id = id ?? Guid.NewGuid(),
            TourId = tourId ?? TestGuid,
            DateTime = TestDateTime,
            Comment = "Sample tour log comment",
            Difficulty = difficulty,
            TotalDistance = 50.25,
            TotalTime = 60,
            Rating = rating
        };
    }

    public static List<TourLog> SampleTourLogList(int count = 2, Guid? tourId = null)
    {
        return [.. Enumerable.Range(0, count).Select(i => SampleTourLog(3 + i, 3, tourId))];
    }

    public static TourPersistence SampleTourPersistence(string name = "Sample Tour")
    {
        return new TourPersistence
        {
            Id = TestGuid,
            UserId = TestUserId,
            Name = name,
            Description = "This is a sample tour for testing",
            From = "Start City",
            To = "End City",
            Distance = 100.5,
            EstimatedTime = 60.0,
            TransportType = "Car",
            ImagePath = "/images/sample.png",
            RouteInformation = "Sample route information",
            TourLogPersistence = []
        };
    }

    public static List<TourPersistence> SampleTourPersistenceList(int count = 1)
    {
        return
        [
            .. Enumerable.Range(1, count).Select(static i => new TourPersistence
            {
                Id = Guid.NewGuid(),
                UserId = TestUserId,
                Name = $"Tour {i}",
                Description = "This is a sample tour for testing",
                From = "Start City",
                To = "End City",
                Distance = 100.5,
                EstimatedTime = 60.0,
                TransportType = "Car",
                ImagePath = "/images/sample.png",
                RouteInformation = "Sample route information",
                TourLogPersistence = []
            })
        ];
    }

    public static TourLogPersistence SampleTourLogPersistence()
    {
        return new TourLogPersistence
        {
            Id = Guid.NewGuid(),
            UserId = TestUserId,
            TourPersistenceId = TestGuid,
            DateTime = TestDateTime,
            Comment = "Sample tour log comment",
            Difficulty = 3,
            TotalDistance = 50.25,
            TotalTime = 60,
            Rating = 4
        };
    }

    public static List<TourLogPersistence> SampleTourLogPersistenceList(int count = 2)
    {
        return [.. Enumerable.Range(0, count).Select(static _ => SampleTourLogPersistence())];
    }

    public static TourDomain SampleTourDomain(string name = "Sample Tour Domain")
    {
        return new TourDomain
        {
            Id = TestGuid,
            Name = name,
            Description = "This is a sample tour domain for testing",
            From = "Start City Domain",
            To = "End City Domain",
            Distance = 100.5,
            EstimatedTime = 60.0,
            TransportType = "Car Domain",
            ImagePath = "/images/sample_domain.png",
            RouteInformation = "Sample route information domain",
            Logs = []
        };
    }

    public static List<TourDomain> SampleTourDomainList(int count = 5)
    {
        return
        [
            .. Enumerable.Range(1, count).Select(static i => new TourDomain
            {
                Id = Guid.NewGuid(),
                Name = $"Tour Domain {i}",
                Description = "This is a sample tour domain for testing",
                From = "Start City Domain",
                To = "End City Domain",
                Distance = 100.5,
                EstimatedTime = 60.0,
                TransportType = "Car Domain",
                ImagePath = "/images/sample_domain.png",
                RouteInformation = "Sample route information domain",
                Logs = []
            })
        ];
    }

    public static TourLogDomain SampleTourLogDomain()
    {
        return new TourLogDomain
        {
            Id = Guid.NewGuid(),
            TourDomainId = TestGuid,
            DateTime = TestDateTime,
            Comment = "Sample tour log domain comment",
            Difficulty = 3,
            TotalDistance = 50.25,
            TotalTime = 60,
            Rating = 4
        };
    }

    public static TourLogDto SampleTourLogDto(double? rating = 4, double difficulty = 3, Guid? tourId = null)
    {
        return new TourLogDto
        {
            Id = Guid.NewGuid(),
            TourId = tourId ?? TestGuid,
            DateTime = TestDateTime,
            Comment = "Sample tour log comment",
            Difficulty = difficulty,
            TotalDistance = 50.25,
            TotalTime = 60,
            Rating = rating
        };
    }

    public static List<TourLogDto> SampleTourLogDtoList(int count = 2, Guid? tourId = null)
    {
        return [.. Enumerable.Range(0, count).Select(_ => SampleTourLogDto(4, 3, tourId))];
    }

    public static List<TourLogDomain> SampleTourLogDomainList(int count = 2)
    {
        return [.. Enumerable.Range(0, count).Select(static _ => SampleTourLogDomain())];
    }

    public static Mock<IBrowserFile> MockBrowserFile(string content)
    {
        var mock = new Mock<IBrowserFile>();
        mock.Setup(static f => f.OpenReadStream(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .Returns(new MemoryStream(Encoding.UTF8.GetBytes(content)));
        return mock;
    }

    public static InputFileChangeEventArgs MakeFile(string content)
    {
        var fileMock = MockBrowserFile(content);
        return new InputFileChangeEventArgs([fileMock.Object]);
    }

    public static string SampleTourJson()
    {
        return JsonSerializer.Serialize(SampleTourDto());
    }

    public static string SampleTourDomainJson()
    {
        return JsonSerializer.Serialize(SampleTourDomain());
    }

    public static void SetupHttpMessageHandlerSuccess(Mock<HttpMessageHandler> mockHandler, object responseBody)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = responseBody is string s
                ? new StringContent(s, Encoding.UTF8, "application/json")
                : JsonContent.Create(responseBody, responseBody.GetType())
        };

        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
    }

    public static void SetupHttpMessageHandlerError(Mock<HttpMessageHandler> mockHandler, HttpStatusCode statusCode,
        string content)
    {
        var response = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(content, Encoding.UTF8, "text/plain")
        };

        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
    }

    public static void VerifyHttpPostRequest(Mock<HttpMessageHandler> mockHandler, string expectedEndpoint)
    {
        mockHandler
            .Protected()
            .Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri!.ToString().Contains(expectedEndpoint)),
                ItExpr.IsAny<CancellationToken>());
    }

    public static void VerifyHttpRequestHeaders(Mock<HttpMessageHandler> mockHandler, string expectedApiKey)
    {
        mockHandler
            .Protected()
            .Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Headers.Authorization != null &&
                    req.Headers.Authorization.Scheme == "Bearer" &&
                    req.Headers.Authorization.Parameter == expectedApiKey &&
                    req.Headers.Accept.Any(static h => h.MediaType == "application/json")),
                ItExpr.IsAny<CancellationToken>());
    }

    public static void VerifyHttpJsonContent<T>(
        Mock<HttpMessageHandler> mockHandler,
        Func<T, bool> predicate
    )
    {
        mockHandler
            .Protected()
            .Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Content != null &&
                    req.Content.Headers.ContentType!.MediaType == "application/json" &&
                    predicate(
                        req.Content.ReadFromJsonAsync<T>().GetAwaiter().GetResult()!
                    )),
                ItExpr.IsAny<CancellationToken>());
    }
    
    public static void AssertValidPdf(byte[] pdfBytes)
    {
        Assert.That(pdfBytes, Is.Not.Null.And.Not.Empty);
        Assert.That(pdfBytes[..4], Is.EqualTo("%PDF"u8.ToArray()));
    }
}
