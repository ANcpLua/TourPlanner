using BL.DomainModel;
using BL.Interface;
using BL.Service;
using DAL.Interface;
using DAL.PersistenceModel;
using MapsterMapper;

namespace Tests.BL;

[TestFixture]
public sealed class TourLogServiceTests
{
    private Mock<ITourLogRepository> _tourLogRepository = null!;
    private Mock<IMapper> _mapper = null!;
    private Mock<IUserContext> _userContext = null!;
    private TourLogService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _tourLogRepository = new Mock<ITourLogRepository>();
        _mapper = new Mock<IMapper>();
        _userContext = TestMocks.UserContext();
        _sut = new TourLogService(_tourLogRepository.Object, _mapper.Object, _userContext.Object);
    }

    [Test]
    public async Task CreateTourLogAsync_MapsLogAndUsesCurrentUserAndCancellationToken()
    {
        var logDomain = TourLogTestData.SampleTourLogDomain();
        var logPersistence = TourLogTestData.SampleTourLogPersistence();
        var cancellationToken = new CancellationTokenSource().Token;

        _mapper.Setup(mapper => mapper.Map<TourLogPersistence>(logDomain)).Returns(logPersistence);
        _tourLogRepository.Setup(repository => repository.CreateTourLogAsync(logPersistence, TestConstants.TestUserId, cancellationToken))
            .ReturnsAsync(logPersistence);
        _mapper.Setup(mapper => mapper.Map<TourLogDomain>(logPersistence)).Returns(logDomain);

        var created = await _sut.CreateTourLogAsync(logDomain, cancellationToken);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(created, Is.SameAs(logDomain));
            _tourLogRepository.Verify(repository => repository.CreateTourLogAsync(logPersistence, TestConstants.TestUserId, cancellationToken), Times.Once);
        }
    }

    [Test]
    public void GetTourLogsByTourId_UsesCurrentUserAndMapsCollection()
    {
        var persistenceLogs = TourLogTestData.SampleTourLogPersistenceList();
        var domainLogs = TourLogTestData.SampleTourLogDomainList();

        _tourLogRepository.Setup(repository => repository.GetTourLogsByTourId(TestConstants.TestGuid, TestConstants.TestUserId))
            .Returns(persistenceLogs);
        _mapper.Setup(mapper => mapper.Map<IEnumerable<TourLogDomain>>(persistenceLogs)).Returns(domainLogs);

        var logs = _sut.GetTourLogsByTourId(TestConstants.TestGuid).ToList();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(logs, Is.EqualTo(domainLogs));
            _tourLogRepository.Verify(repository => repository.GetTourLogsByTourId(TestConstants.TestGuid, TestConstants.TestUserId), Times.Once);
        }
    }

    [Test]
    public void GetTourLogById_WhenRepositoryReturnsNull_ReturnsNullWithoutMapping()
    {
        _tourLogRepository.Setup(static repository => repository.GetTourLogById(TestConstants.NonexistentGuid, TestConstants.TestUserId))
            .Returns((TourLogPersistence?)null);

        var result = _sut.GetTourLogById(TestConstants.NonexistentGuid);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.Null);
            _mapper.Verify(static mapper => mapper.Map<TourLogDomain>(It.IsAny<TourLogPersistence>()), Times.Never);
        }
    }

    [Test]
    public async Task UpdateTourLogAsync_UsesCurrentUserAndReturnsMappedLog()
    {
        var logDomain = TourLogTestData.SampleTourLogDomain();
        var logPersistence = TourLogTestData.SampleTourLogPersistence();
        var cancellationToken = new CancellationTokenSource().Token;

        _mapper.Setup(mapper => mapper.Map<TourLogPersistence>(logDomain)).Returns(logPersistence);
        _tourLogRepository.Setup(repository => repository.UpdateTourLogAsync(logPersistence, TestConstants.TestUserId, cancellationToken))
            .ReturnsAsync(logPersistence);
        _mapper.Setup(mapper => mapper.Map<TourLogDomain>(logPersistence)).Returns(logDomain);

        var updated = await _sut.UpdateTourLogAsync(logDomain, cancellationToken);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(updated, Is.SameAs(logDomain));
            _tourLogRepository.Verify(repository => repository.UpdateTourLogAsync(logPersistence, TestConstants.TestUserId, cancellationToken), Times.Once);
        }
    }

    [Test]
    public void UpdateTourLogAsync_RepositoryFailure_PropagatesException()
    {
        var logDomain = TourLogTestData.SampleTourLogDomain();
        var logPersistence = TourLogTestData.SampleTourLogPersistence();

        _mapper.Setup(mapper => mapper.Map<TourLogPersistence>(logDomain)).Returns(logPersistence);
        _tourLogRepository.Setup(repository => repository.UpdateTourLogAsync(logPersistence, TestConstants.TestUserId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Tour log not found"));

        Assert.That(() => _sut.UpdateTourLogAsync(logDomain),
            Throws.TypeOf<InvalidOperationException>().With.Message.EqualTo("Tour log not found"));
    }

    [Test]
    public async Task DeleteTourLogAsync_ForwardsCurrentUserAndCancellationToken()
    {
        var cancellationToken = new CancellationTokenSource().Token;
        _tourLogRepository.Setup(repository => repository.DeleteTourLogAsync(TestConstants.TestGuid, TestConstants.TestUserId, cancellationToken))
            .Returns(Task.CompletedTask);

        await _sut.DeleteTourLogAsync(TestConstants.TestGuid, cancellationToken);

        _tourLogRepository.Verify(repository => repository.DeleteTourLogAsync(TestConstants.TestGuid, TestConstants.TestUserId, cancellationToken), Times.Once);
    }
}
