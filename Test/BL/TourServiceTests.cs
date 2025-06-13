using BL.DomainModel;
using BL.Service;
using DAL.Interface;
using DAL.PersistenceModel;
using MapsterMapper;
using Moq;

namespace Test.BL;

[TestFixture]
public class TourServiceTests
{
    [SetUp]
    public void Setup()
    {
        _mockTourRepository = new Mock<ITourRepository>();
        _mockMapper = new Mock<IMapper>();
        _tourService = new TourService(_mockTourRepository.Object, _mockMapper.Object);
    }

    private Mock<ITourRepository> _mockTourRepository;
    private Mock<IMapper> _mockMapper;
    private TourService _tourService;

    [Test]
    public async Task CreateTourAsync_ValidTour_ReturnsMappedTourDomain()
    {
        var tourDomain = TestData.SampleTourDomainList().First();
        var tourPersistence = TestData.SampleTourPersistence();
        _mockMapper.Setup(m => m.Map<TourPersistence>(tourDomain)).Returns(tourPersistence);
        _mockMapper.Setup(m => m.Map<TourDomain>(tourPersistence)).Returns(tourDomain);
        _mockTourRepository
            .Setup(r => r.CreateTourAsync(tourPersistence))
            .ReturnsAsync(tourPersistence);


        var result = await _tourService.CreateTourAsync(tourDomain);


        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.Id, Is.EqualTo(tourDomain.Id));
            Assert.That(result.Name, Is.EqualTo(tourDomain.Name));
            Assert.That(result.Description, Is.EqualTo(tourDomain.Description));
        });
        _mockTourRepository.Verify(r => r.CreateTourAsync(tourPersistence), Times.Once);
    }

    [Test]
    public Task CreateTourAsync_RepositoryThrowsException_PropagatesException()
    {
        var tourDomain = TestData.SampleTourDomainList().First();
        var tourPersistence = TestData.SampleTourPersistence();
        _mockMapper.Setup(m => m.Map<TourPersistence>(tourDomain)).Returns(tourPersistence);
        _mockTourRepository
            .Setup(r => r.CreateTourAsync(tourPersistence))
            .ThrowsAsync(new Exception("Database error"));


        var ex = Assert.ThrowsAsync<Exception>(async () => await _tourService.CreateTourAsync(tourDomain)
        );
        Assert.That(ex.Message, Is.EqualTo("Database error"));
        return Task.CompletedTask;
    }

    [Test]
    public void GetAllToursAsync_ToursExist_ReturnsAllMappedTours()
    {
        var toursPersistence = TestData.SampleTourPersistenceList();
        var toursDomain = TestData.SampleTourDomainList();
        _mockTourRepository.Setup(r => r.GetAllTours()).Returns(toursPersistence);
        _mockMapper
            .Setup(m => m.Map<IEnumerable<TourDomain>>(toursPersistence))
            .Returns(toursDomain);


        var result = _tourService.GetAllTours().ToList();


        Assert.That(result, Is.Not.Null);
        Assert.That(result, Has.Count.EqualTo(toursDomain.Count));
        _mockTourRepository.Verify(r => r.GetAllTours(), Times.Once);
    }

    [Test]
    public void GetAllToursAsync_NoToursExist_ReturnsEmptyList()
    {
        _mockTourRepository
            .Setup(r => r.GetAllTours())
            .Returns(new List<TourPersistence>());
        _mockMapper
            .Setup(m => m.Map<IEnumerable<TourDomain>>(It.IsAny<IEnumerable<TourPersistence>>()))
            .Returns(new List<TourDomain>());


        var result = _tourService.GetAllTours();


        Assert.That(result, Is.Empty);
    }

    [Test]
    public void GetTourById_ExistingId_ReturnsMappedTourDomain()
    {
        var tourPersistence = TestData.SampleTourPersistence();
        var tourDomain = TestData.SampleTourDomain();
        _mockTourRepository.Setup(r => r.GetTourById(TestData.TestGuid)).Returns(tourPersistence);
        _mockMapper.Setup(m => m.Map<TourDomain>(tourPersistence)).Returns(tourDomain);

        var result = _tourService.GetTourById(TestData.TestGuid);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Id, Is.EqualTo(TestData.TestGuid));
        _mockTourRepository.Verify(r => r.GetTourById(TestData.TestGuid), Times.Once);
    }

    [Test]
    public void GetTourById_NonExistingId_ReturnsNull()
    {
        _mockTourRepository
            .Setup(r => r.GetTourById(TestData.NonexistentGuid))
            .Returns((TourPersistence)null!);


        var result = _tourService.GetTourById(TestData.NonexistentGuid);


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
            .Setup(r => r.UpdateTourAsync(tourPersistence))
            .ReturnsAsync(tourPersistence);


        var result = await _tourService.UpdateTourAsync(tourDomain);


        Assert.That(result, Is.Not.Null);
        Assert.That(result.Id, Is.EqualTo(tourDomain.Id));
        _mockTourRepository.Verify(r => r.UpdateTourAsync(tourPersistence), Times.Once);
    }

    [Test]
    public Task UpdateTourAsync_NonExistingTour_ThrowsException()
    {
        var tourDomain = TestData.SampleTourDomainList().First();
        var tourPersistence = TestData.SampleTourPersistence();
        _mockMapper.Setup(m => m.Map<TourPersistence>(tourDomain)).Returns(tourPersistence);
        _mockTourRepository
            .Setup(r => r.UpdateTourAsync(tourPersistence))
            .ThrowsAsync(new InvalidOperationException("Tour not found"));


        var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _tourService.UpdateTourAsync(tourDomain)
        );
        Assert.That(ex.Message, Is.EqualTo("Tour not found"));
        return Task.CompletedTask;
    }

    [Test]
    public async Task DeleteTourAsync_ExistingId_CallsRepositoryDelete()
    {
        _mockTourRepository
            .Setup(r => r.DeleteTourAsync(TestData.TestGuid))
            .Returns(Task.CompletedTask);


        await _tourService.DeleteTourAsync(TestData.TestGuid);


        _mockTourRepository.Verify(r => r.DeleteTourAsync(TestData.TestGuid), Times.Once);
    }

    [Test]
    public void SearchTours_ValidSearchText_ReturnsFilteredMappedTours()
    {
        var tourPersistenceList = TestData.SampleTourPersistenceList(3);

        _mockTourRepository.Setup(r => r.SearchToursAsync(TestData.ValidSearchText))
            .Returns(tourPersistenceList.AsQueryable());

        _mockMapper.Setup(m => m.Map<TourDomain>(It.IsAny<TourPersistence>()))
            .Returns((TourPersistence source) =>
            {
                var domain = TestData.SampleTourDomain();
                domain.Id = source.Id;
                domain.Name = source.Name;
                return domain;
            });

        var result = _tourService.SearchTours(TestData.ValidSearchText);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count(), Is.EqualTo(3));
        _mockTourRepository.Verify(r => r.SearchToursAsync(TestData.ValidSearchText), Times.Once);
    }

    [Test]
    public void SearchTours_NoMatchingTours_ReturnsEmptyQueryable()
    {
        _mockTourRepository
            .Setup(r => r.SearchToursAsync(TestData.InvalidSearchText))
            .Returns(new List<TourPersistence>().AsQueryable());


        var result = _tourService.SearchTours(TestData.InvalidSearchText);


        Assert.That(result, Is.Empty);
    }
}