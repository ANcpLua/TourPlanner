using BL.DomainModel;
using BL.Interface;
using BL.Service;
using DAL.Interface;
using DAL.PersistenceModel;
using MapsterMapper;

namespace Tests.BL;

[TestFixture]
public class TourServiceTests
{
    [SetUp]
    public void Setup()
    {
        _mockTourRepository = new Mock<ITourRepository>();
        _mockMapper = new Mock<IMapper>();
        _mockUserContext = TestData.MockUserContext();
        _sut = new TourService(_mockTourRepository.Object, _mockMapper.Object, _mockUserContext.Object);
    }

    private Mock<ITourRepository> _mockTourRepository = null!;
    private Mock<IMapper> _mockMapper = null!;
    private Mock<IUserContext> _mockUserContext = null!;
    private TourService _sut = null!;

    [Test]
    public async Task CreateTourAsync_ValidTour_ReturnsMappedTourDomain()
    {
        var tourDomain = TestData.SampleTourDomainList().First();
        var tourPersistence = TestData.SampleTourPersistence();
        _mockMapper.Setup(m => m.Map<TourPersistence>(tourDomain)).Returns(tourPersistence);
        _mockMapper.Setup(m => m.Map<TourDomain>(tourPersistence)).Returns(tourDomain);
        _mockTourRepository
            .Setup(r => r.CreateTourAsync(tourPersistence, TestData.TestUserId, CancellationToken.None))
            .ReturnsAsync(tourPersistence);

        var result = await _sut.CreateTourAsync(tourDomain);

        Assert.That(result, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Id, Is.EqualTo(tourDomain.Id));
            Assert.That(result.Name, Is.EqualTo(tourDomain.Name));
            Assert.That(result.Description, Is.EqualTo(tourDomain.Description));
        }

        _mockTourRepository.Verify(r => r.CreateTourAsync(tourPersistence, TestData.TestUserId, CancellationToken.None), Times.Once);
    }

    [Test]
    public void GetAllToursAsync_ToursExist_ReturnsAllMappedTours()
    {
        var toursPersistence = TestData.SampleTourPersistenceList();
        var toursDomain = TestData.SampleTourDomainList();
        _mockTourRepository.Setup(r => r.GetAllTours(TestData.TestUserId)).Returns(toursPersistence);
        _mockMapper
            .Setup(m => m.Map<IEnumerable<TourDomain>>(toursPersistence))
            .Returns(toursDomain);

        var result = _sut.GetAllTours().ToList();

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Has.Count.EqualTo(toursDomain.Count));
        _mockTourRepository.Verify(r => r.GetAllTours(TestData.TestUserId), Times.Once);
    }

    [Test]
    public void GetAllToursAsync_NoToursExist_ReturnsEmptyList()
    {
        _mockTourRepository
            .Setup(r => r.GetAllTours(TestData.TestUserId))
            .Returns([]);
        _mockMapper
            .Setup(static m => m.Map<IEnumerable<TourDomain>>(It.IsAny<IEnumerable<TourPersistence>>()))
            .Returns([]);

        var result = _sut.GetAllTours();

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void GetTourById_ExistingId_ReturnsMappedTourDomain()
    {
        var tourPersistence = TestData.SampleTourPersistence();
        var tourDomain = TestData.SampleTourDomain();
        _mockTourRepository.Setup(r => r.GetTourById(TestData.TestGuid, TestData.TestUserId)).Returns(tourPersistence);
        _mockMapper.Setup(m => m.Map<TourDomain>(tourPersistence)).Returns(tourDomain);

        var result = _sut.GetTourById(TestData.TestGuid);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Id, Is.EqualTo(TestData.TestGuid));
        _mockTourRepository.Verify(r => r.GetTourById(TestData.TestGuid, TestData.TestUserId), Times.Once);
    }

    [Test]
    public void GetTourById_NonExistingId_ReturnsNull()
    {
        _mockTourRepository
            .Setup(r => r.GetTourById(TestData.NonexistentGuid, TestData.TestUserId))
            .Returns((TourPersistence)null!);

        var result = _sut.GetTourById(TestData.NonexistentGuid);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task UpdateTourAsync_ExistingTour_ReturnsUpdatedMappedTourDomain()
    {
        var tourDomain = TestData.SampleTourDomainList().First();
        var tourPersistence = TestData.SampleTourPersistence();
        _mockMapper.Setup(m => m.Map<TourPersistence>(tourDomain)).Returns(tourPersistence);
        _mockMapper.Setup(m => m.Map<TourDomain>(tourPersistence)).Returns(tourDomain);
        _mockTourRepository
            .Setup(r => r.UpdateTourAsync(tourPersistence, TestData.TestUserId, CancellationToken.None))
            .ReturnsAsync(tourPersistence);

        var result = await _sut.UpdateTourAsync(tourDomain);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Id, Is.EqualTo(tourDomain.Id));
        _mockTourRepository.Verify(r => r.UpdateTourAsync(tourPersistence, TestData.TestUserId, CancellationToken.None), Times.Once);
    }

    [Test]
    public void UpdateTourAsync_NonExistingTour_ThrowsException()
    {
        var tourDomain = TestData.SampleTourDomainList().First();
        var tourPersistence = TestData.SampleTourPersistence();
        _mockMapper.Setup(m => m.Map<TourPersistence>(tourDomain)).Returns(tourPersistence);
        _mockTourRepository
            .Setup(r => r.UpdateTourAsync(tourPersistence, TestData.TestUserId, CancellationToken.None))
            .ThrowsAsync(new InvalidOperationException("Tour not found"));

        Assert.That(
            () => _sut.UpdateTourAsync(tourDomain),
            Throws.TypeOf<InvalidOperationException>()
                .With.Message.EqualTo("Tour not found"));
    }

    [Test]
    public async Task DeleteTourAsync_ExistingId_CallsRepositoryDelete()
    {
        _mockTourRepository
            .Setup(r => r.DeleteTourAsync(TestData.TestGuid, TestData.TestUserId, CancellationToken.None))
            .Returns(Task.CompletedTask);

        await _sut.DeleteTourAsync(TestData.TestGuid);

        _mockTourRepository.Verify(r => r.DeleteTourAsync(TestData.TestGuid, TestData.TestUserId, CancellationToken.None), Times.Once);
    }

    [Test]
    public void SearchTours_ValidSearchText_ReturnsFilteredMappedTours()
    {
        var tourPersistenceList = TestData.SampleTourPersistenceList(3);

        _mockTourRepository.Setup(r => r.SearchToursAsync(TestData.ValidSearchText, TestData.TestUserId))
            .Returns(tourPersistenceList.AsQueryable());

        _mockMapper.Setup(static m => m.Map<TourDomain>(It.IsAny<TourPersistence>()))
            .Returns(static (TourPersistence source) =>
            {
                var domain = TestData.SampleTourDomain();
                domain.Id = source.Id;
                domain.Name = source.Name;
                return domain;
            });

        var result = _sut.SearchTours(TestData.ValidSearchText);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count(), Is.EqualTo(3));
        _mockTourRepository.Verify(r => r.SearchToursAsync(TestData.ValidSearchText, TestData.TestUserId), Times.Once);
    }

    [Test]
    public void SearchTours_NoMatchingTours_ReturnsEmptyQueryable()
    {
        _mockTourRepository
            .Setup(r => r.SearchToursAsync(TestData.InvalidSearchText, TestData.TestUserId))
            .Returns(new List<TourPersistence>().AsQueryable());

        var result = _sut.SearchTours(TestData.InvalidSearchText);

        Assert.That(result, Is.Empty);
    }
}
