using BL.DomainModel;
using BL.Mapper;
using DAL.PersistenceModel;
using MapsterMapper;
using UI.Model;

namespace Test.BL;

[TestFixture]
public class MappingConfigurationTests
{
    private readonly Mapper _mapper;

    public MappingConfigurationTests()
    {
        var config = MappingConfiguration.RegisterMapping();
        _mapper = new Mapper(config);
    }

    [Test]
    public void TourPersistence_To_TourDomain_MapsAllPropertiesCorrectly()
    {
        var tourPersistence = new TourPersistence
        {
            Id = Guid.NewGuid(),
            Name = "Test Tour",
            Description = "Test Description",
            From = "Start Location",
            To = "End Location",
            Distance = 100.5,
            EstimatedTime = 60.0,
            TransportType = "Car",
            ImagePath = "/test/image.png",
            RouteInformation = "Route info",
            TourLogPersistence =
            [
                new TourLogPersistence
                {
                    Id = Guid.NewGuid(),
                    Comment = "Test comment",
                    DateTime = DateTime.Now,
                    Difficulty = 3.0,
                    TotalDistance = 50.0,
                    TotalTime = 30.0,
                    Rating = 4.0
                }
            ]
        };

        var tourDomain = _mapper.Map<TourDomain>(tourPersistence);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(tourDomain.Id, Is.EqualTo(tourPersistence.Id));
            Assert.That(tourDomain.Name, Is.EqualTo(tourPersistence.Name));
            Assert.That(tourDomain.Description, Is.EqualTo(tourPersistence.Description));
            Assert.That(tourDomain.From, Is.EqualTo(tourPersistence.From));
            Assert.That(tourDomain.To, Is.EqualTo(tourPersistence.To));
            Assert.That(tourDomain.Distance, Is.EqualTo(tourPersistence.Distance));
            Assert.That(tourDomain.EstimatedTime, Is.EqualTo(tourPersistence.EstimatedTime));
            Assert.That(tourDomain.TransportType, Is.EqualTo(tourPersistence.TransportType));
            Assert.That(tourDomain.ImagePath, Is.EqualTo(tourPersistence.ImagePath));
            Assert.That(tourDomain.RouteInformation, Is.EqualTo(tourPersistence.RouteInformation));
            Assert.That(tourDomain.Logs, Has.Count.EqualTo(tourPersistence.TourLogPersistence.Count));
        }
    }

    [Test]
    public void TourDomain_To_TourPersistence_MapsAllPropertiesCorrectly()
    {
        var tourDomain = new TourDomain
        {
            Id = Guid.NewGuid(),
            Name = "Test Tour Domain",
            Description = "Test Description Domain",
            From = "Start Domain",
            To = "End Domain",
            Distance = 200.5,
            EstimatedTime = 120.0,
            TransportType = "Bike",
            ImagePath = "/domain/image.png",
            RouteInformation = "Domain route info",
            Logs =
            [
                new TourLogDomain
                {
                    Id = Guid.NewGuid(),
                    Comment = "Domain comment",
                    DateTime = DateTime.Now,
                    Difficulty = 4.0,
                    TotalDistance = 75.0,
                    TotalTime = 45.0,
                    Rating = 5.0,
                    TourDomainId = Guid.NewGuid()
                }
            ]
        };

        var tourPersistence = _mapper.Map<TourPersistence>(tourDomain);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(tourPersistence.Id, Is.EqualTo(tourDomain.Id));
            Assert.That(tourPersistence.Name, Is.EqualTo(tourDomain.Name));
            Assert.That(tourPersistence.Description, Is.EqualTo(tourDomain.Description));
            Assert.That(tourPersistence.From, Is.EqualTo(tourDomain.From));
            Assert.That(tourPersistence.To, Is.EqualTo(tourDomain.To));
            Assert.That(tourPersistence.Distance, Is.EqualTo(tourDomain.Distance));
            Assert.That(tourPersistence.EstimatedTime, Is.EqualTo(tourDomain.EstimatedTime));
            Assert.That(tourPersistence.TransportType, Is.EqualTo(tourDomain.TransportType));
            Assert.That(tourPersistence.ImagePath, Is.EqualTo(tourDomain.ImagePath));
            Assert.That(tourPersistence.RouteInformation, Is.EqualTo(tourDomain.RouteInformation));
            Assert.That(tourPersistence.TourLogPersistence, Has.Count.EqualTo(tourDomain.Logs.Count));
        }
    }

    [Test]
    public void TourDomain_To_Tour_MapsAllPropertiesCorrectly()
    {
        var tourDomain = TestData.SampleTourDomain();
        tourDomain.Logs = [TestData.SampleTourLogDomain()];

        var tour = _mapper.Map<Tour>(tourDomain);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(tour.Id, Is.EqualTo(tourDomain.Id));
            Assert.That(tour.Name, Is.EqualTo(tourDomain.Name));
            Assert.That(tour.TourLogs, Has.Count.EqualTo(tourDomain.Logs.Count));
        }
    }

    [Test]
    public void Tour_To_TourDomain_MapsAllPropertiesCorrectly()
    {
        var tour = TestData.SampleTour();
        tour.TourLogs = [TestData.SampleTourLogDto()];

        var tourDomain = _mapper.Map<TourDomain>(tour);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(tourDomain.Id, Is.EqualTo(tour.Id));
            Assert.That(tourDomain.Name, Is.EqualTo(tour.Name));
            Assert.That(tourDomain.Logs, Has.Count.EqualTo(tour.TourLogs.Count));
        }
    }

    [Test]
    public void TourLogDomain_To_TourLogPersistence_MapsAllPropertiesCorrectly()
    {
        var tourLogDomain = new TourLogDomain
        {
            Id = Guid.NewGuid(),
            DateTime = DateTime.Now,
            Comment = "Comprehensive test comment",
            Difficulty = 3.5,
            TotalDistance = 25.5,
            TotalTime = 15.5,
            Rating = 4.5,
            TourDomainId = Guid.NewGuid()
        };

        var tourLogPersistence = _mapper.Map<TourLogPersistence>(tourLogDomain);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(tourLogPersistence.Id, Is.EqualTo(tourLogDomain.Id));
            Assert.That(tourLogPersistence.DateTime, Is.EqualTo(tourLogDomain.DateTime));
            Assert.That(tourLogPersistence.Comment, Is.EqualTo(tourLogDomain.Comment));
            Assert.That(tourLogPersistence.Difficulty, Is.EqualTo(tourLogDomain.Difficulty));
            Assert.That(tourLogPersistence.TotalDistance, Is.EqualTo(tourLogDomain.TotalDistance));
            Assert.That(tourLogPersistence.TotalTime, Is.EqualTo(tourLogDomain.TotalTime));
            Assert.That(tourLogPersistence.Rating, Is.EqualTo(tourLogDomain.Rating));
            Assert.That(tourLogPersistence.TourPersistenceId, Is.EqualTo(tourLogDomain.TourDomainId));
        }
    }

    [Test]
    public void TourLogPersistence_To_TourLogDomain_MapsAllPropertiesCorrectly()
    {
        var tourLogPersistence = new TourLogPersistence
        {
            Id = Guid.NewGuid(),
            DateTime = DateTime.Now,
            Comment = "Persistence test comment",
            Difficulty = 2.5,
            TotalDistance = 35.5,
            TotalTime = 25.5,
            Rating = 3.5,
            TourPersistenceId = Guid.NewGuid()
        };

        var tourLogDomain = _mapper.Map<TourLogDomain>(tourLogPersistence);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(tourLogDomain.Id, Is.EqualTo(tourLogPersistence.Id));
            Assert.That(tourLogDomain.DateTime, Is.EqualTo(tourLogPersistence.DateTime));
            Assert.That(tourLogDomain.Comment, Is.EqualTo(tourLogPersistence.Comment));
            Assert.That(tourLogDomain.Difficulty, Is.EqualTo(tourLogPersistence.Difficulty));
            Assert.That(tourLogDomain.TotalDistance, Is.EqualTo(tourLogPersistence.TotalDistance));
            Assert.That(tourLogDomain.TotalTime, Is.EqualTo(tourLogPersistence.TotalTime));
            Assert.That(tourLogDomain.Rating, Is.EqualTo(tourLogPersistence.Rating));
            Assert.That(tourLogDomain.TourDomainId, Is.EqualTo(tourLogPersistence.TourPersistenceId));
        }
    }

    [Test]
    public void TourLogDomain_To_TourLog_MapsAllPropertiesCorrectly()
    {
        var tourLogDomain = TestData.SampleTourLogDomain();

        var tourLog = _mapper.Map<TourLog>(tourLogDomain);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(tourLog.TourId, Is.EqualTo(tourLogDomain.TourDomainId));
            Assert.That(tourLog.Id, Is.EqualTo(tourLogDomain.Id));
            Assert.That(tourLog.DateTime, Is.EqualTo(tourLogDomain.DateTime));
            Assert.That(tourLog.Comment, Is.EqualTo(tourLogDomain.Comment));
            Assert.That(tourLog.Difficulty, Is.EqualTo(tourLogDomain.Difficulty));
            Assert.That(tourLog.TotalDistance, Is.EqualTo(tourLogDomain.TotalDistance));
            Assert.That(tourLog.TotalTime, Is.EqualTo(tourLogDomain.TotalTime));
            Assert.That(tourLog.Rating, Is.EqualTo(tourLogDomain.Rating));
        }
    }

    [Test]
    public void TourLog_To_TourLogDomain_MapsAllPropertiesCorrectly()
    {
        var tourLog = TestData.SampleTourLogDto();

        var tourLogDomain = _mapper.Map<TourLogDomain>(tourLog);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(tourLogDomain.TourDomainId, Is.EqualTo(tourLog.TourId));
            Assert.That(tourLogDomain.Id, Is.EqualTo(tourLog.Id));
            Assert.That(tourLogDomain.DateTime, Is.EqualTo(tourLog.DateTime));
            Assert.That(tourLogDomain.Comment, Is.EqualTo(tourLog.Comment));
            Assert.That(tourLogDomain.Difficulty, Is.EqualTo(tourLog.Difficulty));
            Assert.That(tourLogDomain.TotalDistance, Is.EqualTo(tourLog.TotalDistance));
            Assert.That(tourLogDomain.TotalTime, Is.EqualTo(tourLog.TotalTime));
            Assert.That(tourLogDomain.Rating, Is.EqualTo(tourLog.Rating));
        }
    }

    [Test]
    public void TourLogPersistence_DefaultDateTime_IsSetAutomatically()
    {
        var tourLogPersistence = new TourLogPersistence
        {
            Comment = "Default DateTime test"
        };

        Assert.That(tourLogPersistence.DateTime, Is.Not.Default);
    }

    [Test]
    public void TourLogPersistence_WithTourPersistence_NavigationPropertyWorks()
    {
        var tour = new TourPersistence { Name = "Navigation Test Tour" };
        var tourLog = new TourLogPersistence
        {
            TourPersistence = tour,
            Comment = "Navigation test"
        };
        using (Assert.EnterMultipleScope())
        {
            Assert.That(tourLog.TourPersistence, Is.Not.Null);
            Assert.That(tourLog.TourPersistence.Name, Is.EqualTo("Navigation Test Tour"));
        }
    }
}