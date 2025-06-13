using DAL.Infrastructure;
using DAL.Repository;
using Microsoft.EntityFrameworkCore;

namespace Test.DAL;

[TestFixture]
public class TourLogRepositoryTests
{
    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<TourPlannerContext>()
            .UseInMemoryDatabase($"TourPlannerTestDb_{Guid.NewGuid()}")
            .Options;
        _context = new TourPlannerContext(options);
        _repository = new TourLogRepository(_context);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    private TourPlannerContext _context;
    private TourLogRepository _repository;

    [Test]
    public async Task CreateTourLogAsync_WithValidTourLog_ReturnsSavedTourLog()
    {
        var tourLog = TestData.SampleTourLogPersistence();


        var result = await _repository.CreateTourLogAsync(tourLog);
        var logCount = await _context.TourLogsPersistence.CountAsync();


        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.Id, Is.EqualTo(tourLog.Id));
            Assert.That(logCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void GetTourLogsByTourId_WithExistingTourId_ReturnsAllTourLogs()
    {
        var tourLogs = TestData.SampleTourLogPersistenceList();
        _context.TourLogsPersistence.AddRange(tourLogs);
        _context.SaveChanges();


        var result = _repository.GetTourLogsByTourId(TestData.TestGuid).ToList();


        Assert.That(result, Is.Not.Null);
        Assert.That(result, Has.Count.EqualTo(tourLogs.Count));
    }

    [Test]
    public void GetTourLogsByTourId_WithNonExistentTourId_ReturnsEmptyList()
    {
        var nonExistentTourId = Guid.NewGuid();


        var result = _repository.GetTourLogsByTourId(nonExistentTourId);


        Assert.That(result, Is.Empty);
    }

    [Test]
    public void GetTourLogById_WithExistingId_ReturnsTourLog()
    {
        var tourLog = TestData.SampleTourLogPersistence();
        _context.TourLogsPersistence.Add(tourLog);
        _context.SaveChanges();


        var result = _repository.GetTourLogById(tourLog.Id);


        Assert.That(result, Is.Not.Null);
        Assert.That(result.Id, Is.EqualTo(tourLog.Id));
    }

    [Test]
    public void GetTourLogById_WithNonExistingId_ReturnsNull()
    {
        var nonExistingId = Guid.NewGuid();


        var result = _repository.GetTourLogById(nonExistingId);


        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task UpdateTourLogAsync_WithExistingTourLog_ReturnsUpdatedTourLog()
    {
        var tourLog = TestData.SampleTourLogPersistence();
        _context.TourLogsPersistence.Add(tourLog);
        await _context.SaveChangesAsync();
        tourLog.Comment = "Updated comment";


        var result = await _repository.UpdateTourLogAsync(tourLog);
        var dbTourLog = await _context.TourLogsPersistence
            .FirstAsync(t => t.Id == tourLog.Id);


        Assert.That(result, Is.Not.Null);

        Assert.Multiple(() =>
        {
            Assert.That(result.Comment, Is.EqualTo("Updated comment"));
            Assert.That(dbTourLog.Comment, Is.EqualTo("Updated comment"));
        });
    }

    [Test]
    public async Task DeleteTourLogAsync_WithExistingId_RemovesTourLogFromDatabase()
    {
        var tourLog = TestData.SampleTourLogPersistence();
        _context.TourLogsPersistence.Add(tourLog);
        await _context.SaveChangesAsync();


        await _repository.DeleteTourLogAsync(tourLog.Id);


        Assert.That(await _context.TourLogsPersistence.CountAsync(), Is.EqualTo(0));
    }

    [Test]
    public async Task CreateTourLogAsync_WithLocalDateTime_SavesAsUtc()
    {
        var tourLog = TestData.SampleTourLogPersistence();
        tourLog.DateTime = DateTime.Now;


        var result = await _repository.CreateTourLogAsync(tourLog);


        Assert.That(result, Is.Not.Null);
        Assert.That(result.DateTime.Kind, Is.EqualTo(DateTimeKind.Local));
    }

    [Test]
    public async Task CreateTourLogAsync_WithPreciseDistance_MaintainsPrecision()
    {
        var tourLog = TestData.SampleTourLogPersistence();
        tourLog.TotalDistance = 10.123456789;


        var result = await _repository.CreateTourLogAsync(tourLog);


        Assert.That(result, Is.Not.Null);
        Assert.That(result.TotalDistance, Is.EqualTo(10.123456789).Within(0.000000001),
            "Distance should maintain its precision"
        );
    }

    [Test]
    public async Task CreateTourLogAsync_WithFutureDate_SavesSuccessfully()
    {
        var tourLog = TestData.SampleTourLogPersistence();
        tourLog.DateTime = DateTime.UtcNow.AddYears(1);


        var result = await _repository.CreateTourLogAsync(tourLog);


        Assert.That(result, Is.Not.Null);
        Assert.That(result.DateTime, Is.GreaterThan(DateTime.UtcNow));
    }
}