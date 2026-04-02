using BL.DomainModel;
using BL.Interface;
using BL.Service;
using DAL.Interface;
using DAL.PersistenceModel;
using MapsterMapper;

namespace Tests.BL;

[TestFixture]
public sealed class TourServiceTests
{
    private Mock<ITourRepository> _tourRepository = null!;
    private Mock<IMapper> _mapper = null!;
    private Mock<IUserContext> _userContext = null!;
    private TourService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _tourRepository = new Mock<ITourRepository>();
        _mapper = new Mock<IMapper>();
        _userContext = TestMocks.UserContext();
        _sut = new TourService(_tourRepository.Object, _mapper.Object, _userContext.Object);
    }

    [Test]
    public async Task CreateTourAsync_MapsTourAndUsesCurrentUserAndCancellationToken()
    {
        var tourDomain = TourTestData.SampleTourDomain();
        var tourPersistence = TourTestData.SampleTourPersistence();
        var cancellationToken = new CancellationTokenSource().Token;

        _mapper.Setup(mapper => mapper.Map<TourPersistence>(tourDomain)).Returns(tourPersistence);
        _tourRepository.Setup(repository => repository.CreateTourAsync(tourPersistence, TestConstants.TestUserId, cancellationToken))
            .ReturnsAsync(tourPersistence);
        _mapper.Setup(mapper => mapper.Map<TourDomain>(tourPersistence)).Returns(tourDomain);

        var created = await _sut.CreateTourAsync(tourDomain, cancellationToken);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(created, Is.SameAs(tourDomain));
            _tourRepository.Verify(repository => repository.CreateTourAsync(tourPersistence, TestConstants.TestUserId, cancellationToken), Times.Once);
            _mapper.Verify(mapper => mapper.Map<TourPersistence>(tourDomain), Times.Once);
            _mapper.Verify(mapper => mapper.Map<TourDomain>(tourPersistence), Times.Once);
        }
    }

    [Test]
    public void GetAllTours_UsesCurrentUserAndMapsRepositoryCollection()
    {
        var persistenceTours = TourTestData.SampleTourPersistenceList(2);
        var domainTours = TourTestData.SampleTourDomainList(2);

        _tourRepository.Setup(repository => repository.GetAllTours(TestConstants.TestUserId)).Returns(persistenceTours);
        _mapper.Setup(mapper => mapper.Map<IEnumerable<TourDomain>>(persistenceTours)).Returns(domainTours);

        var tours = _sut.GetAllTours().ToList();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(tours, Is.EqualTo(domainTours));
            _tourRepository.Verify(repository => repository.GetAllTours(TestConstants.TestUserId), Times.Once);
            _mapper.Verify(mapper => mapper.Map<IEnumerable<TourDomain>>(persistenceTours), Times.Once);
        }
    }

    [Test]
    public void GetTourById_WhenRepositoryReturnsNull_ReturnsNullWithoutMapping()
    {
        _tourRepository.Setup(static repository => repository.GetTourById(TestConstants.NonexistentGuid, TestConstants.TestUserId))
            .Returns((TourPersistence?)null);

        var result = _sut.GetTourById(TestConstants.NonexistentGuid);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.Null);
            _mapper.Verify(static mapper => mapper.Map<TourDomain>(It.IsAny<TourPersistence>()), Times.Never);
        }
    }

    [Test]
    public async Task UpdateTourAsync_UsesCurrentUserAndReturnsMappedTour()
    {
        var tourDomain = TourTestData.SampleTourDomain();
        var tourPersistence = TourTestData.SampleTourPersistence();
        var cancellationToken = new CancellationTokenSource().Token;

        _mapper.Setup(mapper => mapper.Map<TourPersistence>(tourDomain)).Returns(tourPersistence);
        _tourRepository.Setup(repository => repository.UpdateTourAsync(tourPersistence, TestConstants.TestUserId, cancellationToken))
            .ReturnsAsync(tourPersistence);
        _mapper.Setup(mapper => mapper.Map<TourDomain>(tourPersistence)).Returns(tourDomain);

        var updated = await _sut.UpdateTourAsync(tourDomain, cancellationToken);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(updated, Is.SameAs(tourDomain));
            _tourRepository.Verify(repository => repository.UpdateTourAsync(tourPersistence, TestConstants.TestUserId, cancellationToken), Times.Once);
        }
    }

    [Test]
    public void UpdateTourAsync_RepositoryFailure_PropagatesException()
    {
        var tourDomain = TourTestData.SampleTourDomain();
        var tourPersistence = TourTestData.SampleTourPersistence();

        _mapper.Setup(mapper => mapper.Map<TourPersistence>(tourDomain)).Returns(tourPersistence);
        _tourRepository.Setup(repository => repository.UpdateTourAsync(tourPersistence, TestConstants.TestUserId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Tour not found"));

        Assert.That(() => _sut.UpdateTourAsync(tourDomain),
            Throws.TypeOf<InvalidOperationException>().With.Message.EqualTo("Tour not found"));
    }

    [Test]
    public async Task DeleteTourAsync_ForwardsCurrentUserAndCancellationToken()
    {
        var cancellationToken = new CancellationTokenSource().Token;
        _tourRepository.Setup(repository => repository.DeleteTourAsync(TestConstants.TestGuid, TestConstants.TestUserId, cancellationToken))
            .Returns(Task.CompletedTask);

        await _sut.DeleteTourAsync(TestConstants.TestGuid, cancellationToken);

        _tourRepository.Verify(repository => repository.DeleteTourAsync(TestConstants.TestGuid, TestConstants.TestUserId, cancellationToken), Times.Once);
    }

    [Test]
    public void SearchTours_MapsEveryRepositoryResultForCurrentUser()
    {
        var first = TourTestData.SampleTourPersistence("First Tour");
        first.Id = Guid.NewGuid();
        var second = TourTestData.SampleTourPersistence("Second Tour");
        second.Id = Guid.NewGuid();
        var repositoryResults = new[] { first, second }.AsQueryable();

        _tourRepository.Setup(static repository => repository.SearchToursAsync("Tour", TestConstants.TestUserId))
            .Returns(repositoryResults);
        _mapper.Setup(static mapper => mapper.Map<TourDomain>(It.IsAny<TourPersistence>()))
            .Returns(static (TourPersistence source) => new TourDomain
            {
                Id = source.Id,
                Name = source.Name,
                Description = source.Description,
                From = source.From,
                To = source.To,
                TransportType = source.TransportType,
                Distance = source.Distance,
                EstimatedTime = source.EstimatedTime,
                ImagePath = source.ImagePath,
                RouteInformation = source.RouteInformation,
                Logs = []
            });

        var results = _sut.SearchTours("Tour").ToList();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(results.Select(static result => result.Id), Is.EqualTo(new[] { first.Id, second.Id }));
            _tourRepository.Verify(static repository => repository.SearchToursAsync("Tour", TestConstants.TestUserId), Times.Once);
            _mapper.Verify(static mapper => mapper.Map<TourDomain>(It.IsAny<TourPersistence>()), Times.Exactly(2));
        }
    }
}
