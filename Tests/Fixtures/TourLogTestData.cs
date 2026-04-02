using BL.DomainModel;
using Contracts.TourLogs;
using DAL.PersistenceModel;
using UI.Model;

namespace Tests.Fixtures;

public static class TourLogTestData
{
    public static TourLog SampleTourLog(double? rating = 4, double difficulty = 3, Guid? tourId = null, Guid? id = null)
    {
        return new TourLog
        {
            Id = id ?? Guid.NewGuid(),
            TourId = tourId ?? TestConstants.TestGuid,
            DateTime = TestConstants.TestDateTime,
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

    public static TourLogDto SampleTourLogDto(double? rating = 4, double difficulty = 3, Guid? tourId = null)
    {
        return new TourLogDto
        {
            Id = Guid.NewGuid(),
            TourId = tourId ?? TestConstants.TestGuid,
            DateTime = TestConstants.TestDateTime,
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

    public static TourLogDomain SampleTourLogDomain()
    {
        return new TourLogDomain
        {
            Id = Guid.NewGuid(),
            TourDomainId = TestConstants.TestGuid,
            DateTime = TestConstants.TestDateTime,
            Comment = "Sample tour log domain comment",
            Difficulty = 3,
            TotalDistance = 50.25,
            TotalTime = 60,
            Rating = 4
        };
    }

    public static List<TourLogDomain> SampleTourLogDomainList(int count = 2)
    {
        return [.. Enumerable.Range(0, count).Select(static _ => SampleTourLogDomain())];
    }

    public static TourLogPersistence SampleTourLogPersistence()
    {
        return new TourLogPersistence
        {
            Id = Guid.NewGuid(),
            UserId = TestConstants.TestUserId,
            TourPersistenceId = TestConstants.TestGuid,
            DateTime = TestConstants.TestDateTime,
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
}
