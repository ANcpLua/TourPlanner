using System.Net;
using BL.DomainModel;
using DAL.PersistenceModel;
using UI.Model;
using UI.Service.Interface;
using UI.ViewModel;

namespace Test;

public static class TestData
{
    public const string ValidSearchText = "Sample Tour";
    public const string InvalidSearchText = "NonexistentTour";
    public static readonly Guid TestGuid = new("11111111-1111-1111-1111-111111111111");
    public static readonly Guid NonexistentGuid = new("99999999-9999-9999-9999-999999999999");

    private static readonly DateTime TestDateTime = new(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
    public static readonly (double Latitude, double Longitude) TestCoordinates = (48.2082, 16.3738);

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
        mock.Setup(t => t.ShowSuccess(It.IsAny<string>()));
        mock.Setup(t => t.ShowError(It.IsAny<string>()));
        return mock;
    }

    public static Mock<IConfiguration> MockConfiguration()
    {
        var mock = new Mock<IConfiguration>();
        mock.Setup(c => c["AppSettings:OpenRouteServiceApiKey"]).Returns("dummy-api-key");
        mock.Setup(c => c["AppSettings:OpenRouteServiceApiBaseUrl"]).Returns("https://api.openrouteservice.org");
        mock.Setup(c => c["AppSettings:ImageBasePath"]).Returns("/images/");
        return mock;
    }

    public static Mock<IHttpService> MockHttpService()
    {
        var mock = new Mock<IHttpService>();
        mock.Setup(s => s.GetAsync<Tour>(It.IsAny<string>())).ReturnsAsync(SampleTour());
        mock.Setup(s => s.GetListAsync<Tour>(It.IsAny<string>())).ReturnsAsync(SampleTourList());
        mock.Setup(s => s.PostAsync<Tour>(It.IsAny<string>(), It.IsAny<object>())).ReturnsAsync(SampleTour());
        mock.Setup(s => s.PutAsync<Tour>(It.IsAny<string>(), It.IsAny<object>())).ReturnsAsync(SampleTour());
        mock.Setup(s => s.DeleteAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
        mock.Setup(s => s.GetStringAsync(It.IsAny<string>())).ReturnsAsync("Sample string response");
        mock.Setup(s => s.GetByteArrayAsync(It.IsAny<string>())).ReturnsAsync([1, 2, 3, 4, 5]);
        return mock;
    }

    public static Mock<MapViewModel> MockMapViewModel()
    {
        return new Mock<MapViewModel>(
            MockJsRuntime().Object,
            MockHttpService().Object,
            MockToastService().Object,
            MockLogger().Object)
        {
            DefaultValue = DefaultValue.Mock,
            CallBase = false
        };
    }

    public static Tour SampleTour(string name = "Sample Tour", Guid? id = null)
    {
        return new Tour
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
            RouteInformation = "Sample route information"
        };
    }

    public static List<Tour> SampleTourList(int count = 5)
    {
        return [.. Enumerable.Range(1, count).Select(i => SampleTour($"Tour {i}", Guid.NewGuid()))];
    }

    public static TourLog SampleTourLog(int? rating = 4, int difficulty = 3, Guid? tourId = null, Guid? id = null)
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
            .. Enumerable.Range(1, count).Select(i => new TourPersistence
            {
                Id = Guid.NewGuid(),
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
        return [.. Enumerable.Range(0, count).Select(_ => SampleTourLogPersistence())];
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
            .. Enumerable.Range(1, count).Select(i => new TourDomain
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

    public static TourLog SampleTourLogDto(int? rating = 4, int difficulty = 3, Guid? tourId = null)
    {
        return SampleTourLog(rating, difficulty, tourId);
    }

    public static List<TourLog> SampleTourLogDtoList(int count = 2, Guid? tourId = null)
    {
        return SampleTourLogList(count, tourId);
    }

    public static List<TourLogDomain> SampleTourLogDomainList(int count = 2)
    {
        return [.. Enumerable.Range(0, count).Select(_ => SampleTourLogDomain())];
    }

    public static Mock<IBrowserFile> MockBrowserFile(string content)
    {
        var mock = new Mock<IBrowserFile>();
        mock.Setup(f => f.OpenReadStream(It.IsAny<long>(), It.IsAny<CancellationToken>()))
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
        return JsonSerializer.Serialize(SampleTour());
    }

    public static string SampleTourDomainJson()
    {
        return JsonSerializer.Serialize(SampleTourDomain());
    }

    public static void SetupHttpMessageHandlerSuccess(Mock<HttpMessageHandler> mockHandler, string jsonContent)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
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
                    req.Headers.Accept.Any(h => h.MediaType == "application/json")),
                ItExpr.IsAny<CancellationToken>());
    }

    public static void VerifyHttpJsonContent(Mock<HttpMessageHandler> mockHandler)
    {
        mockHandler
            .Protected()
            .Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Content != null &&
                    req.Content.Headers.ContentType!.MediaType == "application/json"),
                ItExpr.IsAny<CancellationToken>());
    }

    public static void SetupHttpMessageHandlerBytes(Mock<HttpMessageHandler> mockHandler, byte[] content,
        HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var response = new HttpResponseMessage(statusCode)
        {
            Content = new ByteArrayContent(content)
        };

        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
    }

    public static void VerifyHttpRequest(Mock<HttpMessageHandler> mockHandler, HttpMethod method, string expectedUri)
    {
        mockHandler
            .Protected()
            .Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == method &&
                    req.RequestUri!.ToString().Contains(expectedUri)),
                ItExpr.IsAny<CancellationToken>());
    }

    public static void AssertValidPdf(byte[] pdfBytes)
    {
        Assert.That(pdfBytes, Is.Not.Null.And.Not.Empty);
        Assert.That(pdfBytes[..4], Is.EqualTo("%PDF"u8.ToArray()));
    }
}