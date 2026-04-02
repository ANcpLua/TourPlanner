using BL.DomainModel;
using Contracts.Tours;
using DAL.PersistenceModel;
using UI.Model;

namespace Tests.Fixtures;

public static class TourTestData
{
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
            Id = id ?? TestConstants.TestGuid,
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
            Id = id ?? TestConstants.TestGuid,
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

    public static TourDomain SampleTourDomain(string name = "Sample Tour Domain")
    {
        return new TourDomain
        {
            Id = TestConstants.TestGuid,
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

    public static TourPersistence SampleTourPersistence(string name = "Sample Tour")
    {
        return new TourPersistence
        {
            Id = TestConstants.TestGuid,
            UserId = TestConstants.TestUserId,
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
                UserId = TestConstants.TestUserId,
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

    public static string SampleTourJson()
    {
        return JsonSerializer.Serialize(SampleTourDto());
    }

    public static string SampleTourDomainJson()
    {
        return JsonSerializer.Serialize(SampleTourDomain());
    }
}
