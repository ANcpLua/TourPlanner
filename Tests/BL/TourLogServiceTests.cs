using BL.DomainModel;
using BL.Service;
using DAL.Interface;
using DAL.PersistenceModel;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace Tests.BL;

[TestFixture]
public class TourLogServiceTests
{
    [SetUp]
    public void Setup()
    {
        _mockTourLogRepository = new Mock<ITourLogRepository>();
        _mockMapper = new Mock<IMapper>();
        _sut = new TourLogService(_mockTourLogRepository.Object, _mockMapper.Object);
    }

    private Mock<ITourLogRepository> _mockTourLogRepository = null!;
    private Mock<IMapper> _mockMapper = null!;
    private TourLogService _sut = null!;

    [Test]
    public async Task CreateTourLogAsync_ValidTourLog_ReturnsMappedTourLogDomain()
    {
        var tourLogDomain = TestData.SampleTourLogDomainList().First();
        var tourLogPersistence = TestData.SampleTourLogPersistence();
        _mockMapper
            .Setup(m => m.Map<TourLogPersistence>(tourLogDomain))
            .Returns(tourLogPersistence);
        _mockMapper.Setup(m => m.Map<TourLogDomain>(tourLogPersistence)).Returns(tourLogDomain);
        _mockTourLogRepository
            .Setup(r => r.CreateTourLogAsync(tourLogPersistence, CancellationToken.None))
            .ReturnsAsync(tourLogPersistence);

        var result = await _sut.CreateTourLogAsync(tourLogDomain);

        Assert.That(result, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Id, Is.EqualTo(tourLogDomain.Id));
            Assert.That(result.Comment, Is.EqualTo(tourLogDomain.Comment));
            Assert.That(result.DateTime, Is.EqualTo(tourLogDomain.DateTime));
        }

        _mockTourLogRepository.Verify(
            r => r.CreateTourLogAsync(tourLogPersistence, CancellationToken.None),
            Times.Once
        );
    }

    [Test]
    public void GetTourLogById_ExistingId_ReturnsMappedTourLogDomain()
    {
        var tourLogPersistence = TestData.SampleTourLogPersistence();

        _mockTourLogRepository
            .Setup(r => r.GetTourLogById(tourLogPersistence.Id))
            .Returns(tourLogPersistence);

        _mockMapper.Setup(static m => m.Map<TourLogDomain>(It.IsAny<TourLogPersistence>()))
            .Returns(static (TourLogPersistence source) =>
            {
                var domain = TestData.SampleTourLogDomain();
                domain.Id = source.Id;
                return domain;
            });

        var result = _sut.GetTourLogById(tourLogPersistence.Id);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Id, Is.EqualTo(tourLogPersistence.Id));
        _mockTourLogRepository.Verify(r => r.GetTourLogById(tourLogPersistence.Id), Times.Once);
    }

    [Test]
    public void GetTourLogById_NonExistingId_ReturnsNull()
    {
        _mockTourLogRepository
            .Setup(static r => r.GetTourLogById(TestData.NonexistentGuid))
            .Returns((TourLogPersistence)null!);

        var result = _sut.GetTourLogById(TestData.NonexistentGuid);

        Assert.That(result, Is.Null);
    }

    [Test]
    public void GetTourLogsByTourId_ExistingTourId_ReturnsAllMappedTourLogs()
    {
        var tourLogsPersistence = TestData.SampleTourLogPersistenceList();
        var tourLogsDomain = TestData.SampleTourLogDomainList();
        _mockTourLogRepository
            .Setup(r => r.GetTourLogsByTourId(TestData.TestGuid)).Returns(tourLogsPersistence);

        _mockMapper
            .Setup(m => m.Map<IEnumerable<TourLogDomain>>(tourLogsPersistence))
            .Returns(tourLogsDomain);

        var result = _sut.GetTourLogsByTourId(TestData.TestGuid).ToList();

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Has.Count.EqualTo(tourLogsDomain.Count));
        _mockTourLogRepository.Verify(
            r => r.GetTourLogsByTourId(TestData.TestGuid),
            Times.Once
        );
    }

    [Test]
    public void GetTourLogsByTourId_NonExistingTourId_ReturnsEmptyList()
    {
        _mockTourLogRepository
            .Setup(static r => r.GetTourLogsByTourId(TestData.NonexistentGuid));
        _mockMapper
            .Setup(static m =>
                m.Map<IEnumerable<TourLogDomain>>(It.IsAny<IEnumerable<TourLogPersistence>>())
            )
            .Returns([]);

        var result = _sut.GetTourLogsByTourId(TestData.NonexistentGuid);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task UpdateTourLogAsync_ExistingTourLog_ReturnsUpdatedMappedTourLogDomain()
    {
        var tourLogDomain = TestData.SampleTourLogDomainList().First();
        var tourLogPersistence = TestData.SampleTourLogPersistence();
        _mockMapper
            .Setup(m => m.Map<TourLogPersistence>(tourLogDomain))
            .Returns(tourLogPersistence);
        _mockMapper.Setup(m => m.Map<TourLogDomain>(tourLogPersistence)).Returns(tourLogDomain);
        _mockTourLogRepository
            .Setup(r => r.UpdateTourLogAsync(tourLogPersistence, CancellationToken.None))
            .ReturnsAsync(tourLogPersistence);

        var result = await _sut.UpdateTourLogAsync(tourLogDomain);

        Assert.That(result, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Id, Is.EqualTo(tourLogDomain.Id));
            Assert.That(result.Comment, Is.EqualTo(tourLogDomain.Comment));
            Assert.That(result.DateTime, Is.EqualTo(tourLogDomain.DateTime));
        }

        _mockTourLogRepository.Verify(
            r => r.UpdateTourLogAsync(tourLogPersistence, CancellationToken.None),
            Times.Once
        );
    }

    [Test]
    public void UpdateTourLogAsync_NonExistingTourLog_ThrowsException()
    {
        var tourLogDomain = TestData.SampleTourLogDomainList().First();
        var tourLogPersistence = TestData.SampleTourLogPersistence();
        _mockMapper
            .Setup(m => m.Map<TourLogPersistence>(tourLogDomain))
            .Returns(tourLogPersistence);
        _mockTourLogRepository
            .Setup(r => r.UpdateTourLogAsync(tourLogPersistence, CancellationToken.None))
            .ThrowsAsync(new InvalidOperationException("Tour log not found"));

        Assert.That(
            () => _sut.UpdateTourLogAsync(tourLogDomain),
            Throws.TypeOf<InvalidOperationException>()
                .With.Message.EqualTo("Tour log not found"));
    }

    [Test]
    public async Task DeleteTourLogAsync_ExistingId_CallsRepositoryDelete()
    {
        var tourLogId = TestData.SampleTourLogPersistence().Id;
        _mockTourLogRepository
            .Setup(r => r.DeleteTourLogAsync(tourLogId, CancellationToken.None))
            .Returns(Task.CompletedTask);

        await _sut.DeleteTourLogAsync(tourLogId);

        _mockTourLogRepository.Verify(r => r.DeleteTourLogAsync(tourLogId, CancellationToken.None), Times.Once);
    }

    [Test]
    public void DeleteTourLogAsync_NonExistingId_DoesNotThrowException()
    {
        _mockTourLogRepository
            .Setup(r => r.DeleteTourLogAsync(TestData.NonexistentGuid, CancellationToken.None))
            .Returns(Task.CompletedTask);

        Assert.That(
            () => _sut.DeleteTourLogAsync(TestData.NonexistentGuid),
            Throws.Nothing);
    }

    [Test]
    public void CreateTourLogAsync_CancellationRequested_ThrowsOperationCanceledException()
    {
        var tourLogDomain = TestData.SampleTourLogDomainList().First();
        var tourLogPersistence = TestData.SampleTourLogPersistence();
        _mockMapper
            .Setup(m => m.Map<TourLogPersistence>(tourLogDomain))
            .Returns(tourLogPersistence);
        _mockTourLogRepository
            .Setup(r => r.CreateTourLogAsync(tourLogPersistence, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.That(
            () => _sut.CreateTourLogAsync(tourLogDomain, cts.Token),
            Throws.TypeOf<OperationCanceledException>());
    }

    [Test]
    public void GetTourLogsByTourId_LargeTourLogCount_HandlesLargeDataSet()
    {
        List<TourLogPersistence> largeTourLogList = [.. Enumerable
            .Range(0, 10000)
            .Select(static _ => TestData.SampleTourLogPersistence())];

        List<TourLogDomain> largeTourLogDomainList = [.. Enumerable
            .Range(0, 10000)
            .Select(static _ => TestData.SampleTourLogDomainList().First())];

        _mockTourLogRepository
            .Setup(r => r.GetTourLogsByTourId(TestData.TestGuid)).Returns(largeTourLogList);

        _mockMapper
            .Setup(m => m.Map<IEnumerable<TourLogDomain>>(largeTourLogList))
            .Returns(largeTourLogDomainList);

        var result = _sut.GetTourLogsByTourId(TestData.TestGuid).ToList();

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Has.Count.EqualTo(largeTourLogDomainList.Count));
        _mockTourLogRepository.Verify(
            r => r.GetTourLogsByTourId(TestData.TestGuid),
            Times.Once
        );
    }

    [Test]
    public async Task UpdateTourLogAsync_ConcurrentUpdates_HandlesRaceCondition()
    {
        var tourLogDomain = TestData.SampleTourLogDomainList().First();
        var tourLogPersistence = TestData.SampleTourLogPersistence();
        _mockMapper
            .Setup(m => m.Map<TourLogPersistence>(tourLogDomain))
            .Returns(tourLogPersistence);
        _mockMapper.Setup(m => m.Map<TourLogDomain>(tourLogPersistence)).Returns(tourLogDomain);

        _mockTourLogRepository
            .SetupSequence(r => r.UpdateTourLogAsync(tourLogPersistence, CancellationToken.None))
            .ThrowsAsync(new DbUpdateConcurrencyException("Update conflict"))
            .ReturnsAsync(tourLogPersistence);

        Assert.That(
            () => _sut.UpdateTourLogAsync(tourLogDomain),
            Throws.TypeOf<DbUpdateConcurrencyException>());
        var result = await _sut.UpdateTourLogAsync(tourLogDomain);
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Id, Is.EqualTo(tourLogDomain.Id));
    }
}