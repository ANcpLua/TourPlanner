using System.Text;
using System.Text.Json;
using BL.DomainModel;
using BlazorDownloadFile;
using DAL.PersistenceModel;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Configuration;
using Microsoft.JSInterop;
using Moq;
using Serilog;
using UI.Model;
using UI.Service.Interface;
using UI.ViewModel;

namespace Test;

public static class TestData
{
    public const string ValidSearchText = "Sample Tour";
    public const string InvalidSearchText = "NonexistentTour";
    public static readonly Guid TestGuid = new("11111111-1111-1111-1111-111111111111");
    private static readonly DateTime TestDateTime = new(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
    public static readonly (double Latitude, double Longitude) TestCoordinates = (48.2082, 16.3738);
    public static readonly Guid NonexistentGuid = new("99999999-9999-9999-9999-999999999999");

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

    public static Mock<IViewModelHelperService> MockViewModelHelperService()
    {
        return new Mock<IViewModelHelperService>();
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

    public static Mock<IRouteApiService> MockRouteApiService()
    {
        var mock = new Mock<IRouteApiService>();
        mock.Setup(r =>
                r.FetchRouteDataAsync(It.IsAny<(double, double)>(), It.IsAny<(double, double)>(), It.IsAny<string>()))
            .ReturnsAsync((100.5, 60.0));
        return mock;
    }

    public static Mock<IBrowserFile> MockBrowserFile(string content)
    {
        var mock = new Mock<IBrowserFile>();
        mock.Setup(f => f.OpenReadStream(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .Returns(new MemoryStream(Encoding.UTF8.GetBytes(content)));
        return mock;
    }

    public static Mock<MapViewModel> MockMapViewModel()
    {
        return new Mock<MapViewModel>(Mock.Of<IJSRuntime>(), Mock.Of<IHttpService>(), Mock.Of<IToastServiceWrapper>(),
            Mock.Of<ILogger>())
        {
            CallBase = true
        };
    }

    public static void SetupMapViewModel(Mock<MapViewModel> mockMapViewModel,
        (double Latitude, double Longitude) fromCoords,
        (double Latitude, double Longitude) toCoords)
    {
        mockMapViewModel.Setup(m => m.GetCoordinates("Vienna")).Returns(fromCoords);
        mockMapViewModel.Setup(m => m.GetCoordinates("Berlin")).Returns(toCoords);
    }

    public static void SetupRouteApiService(Mock<IRouteApiService> mockRouteApi,
        (double Latitude, double Longitude) fromCoords,
        (double Latitude, double Longitude) toCoords)
    {
        mockRouteApi.Setup(r => r.FetchRouteDataAsync(fromCoords, toCoords, It.IsAny<string>()))
            .ReturnsAsync((523.4, 480.0));
    }

    public static void SetupHttpServicePut(Mock<IHttpService> mockHttpService, Tour tour)
    {
        mockHttpService.Setup(s => s.PutAsync<Tour>($"api/tour/{tour.Id}", It.IsAny<Tour>()))
            .ReturnsAsync(tour);
    }

    public static Tour SampleTour(string name = "Sample Tour")
    {
        return new Tour
        {
            Id = TestGuid,
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

    public static Tour SampleTourWithVariousProperties()
    {
        return new Tour
        {
            Id = TestGuid,
            Name = "Complex Tour",
            Description = "Complex tour for testing",
            From = "Vienna",
            To = "Berlin",
            Distance = 523.4,
            EstimatedTime = 480.0,
            TransportType = "Car",
            ImagePath = "/images/complex.png",
            RouteInformation = "Complex route information"
        };
    }

    public static TourLog SampleTourLogDto(int? rating = 4, int difficulty = 3)
    {
        return new TourLog
        {
            Id = Guid.NewGuid(),
            TourId = TestGuid,
            DateTime = TestDateTime,
            Comment = "Sample tour log comment",
            Difficulty = difficulty,
            TotalDistance = 50.25,
            TotalTime = 60,
            Rating = rating
        };
    }

    public static List<Tour> SampleTourList(int count = 5)
    {
        return Enumerable.Range(0, count).Select(i =>
        {
            var tour = SampleTour($"Tour {i + 1}");
            tour.Id = Guid.NewGuid();
            return tour;
        }).ToList();
    }

    public static List<TourLog> SampleTourLogDtoList(int count = 2)
    {
        return Enumerable.Range(0, count).Select(i => SampleTourLogDto(3 + i)).ToList();
    }

    public static (double Latitude, double Longitude) SampleCoordinates()
    {
        return TestCoordinates;
    }

    public static string SampleTourJson()
    {
        return JsonSerializer.Serialize(SampleTour());
    }

    public static string SampleTourDomainJson()
    {
        return JsonSerializer.Serialize(SampleTourDomain());
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

    public static List<TourPersistence> SampleTourPersistenceList(int count = 1)
    {
        return count == 1
            ? [SampleTourPersistence()]
            : Enumerable.Range(0, count).Select(i =>
            {
                var tour = SampleTourPersistence($"Tour {i + 1}");
                tour.Id = Guid.NewGuid();
                return tour;
            }).ToList();
    }

    public static List<TourLogPersistence> SampleTourLogPersistenceList(int count = 2)
    {
        return Enumerable.Range(0, count).Select(_ => SampleTourLogPersistence()).ToList();
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

    public static List<TourDomain> SampleTourDomainList(int count = 5)
    {
        return Enumerable.Range(0, count).Select(i =>
        {
            var tour = SampleTourDomain($"Tour Domain {i + 1}");
            tour.Id = Guid.NewGuid();
            return tour;
        }).ToList();
    }

    public static List<TourLogDomain> SampleTourLogDomainList(int count = 2)
    {
        return Enumerable.Range(0, count).Select(_ => SampleTourLogDomain()).ToList();
    }
}