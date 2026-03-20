using DAL.Infrastructure;
using DAL.Repository;
using Microsoft.EntityFrameworkCore;

namespace Tests.DAL;

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

    private TourPlannerContext _context = null!;
    private TourLogRepository _repository = null!;

    [Test]
    public async Task CreateTourLogAsync_WithValidTourLog_ReturnsSavedTourLog()
    {
        var tourLog = TestData.SampleTourLogPersistence();

        var result = await _repository.CreateTourLogAsync(tourLog, TestData.TestUserId);
        var logCount = await _context.TourLogsPersistence.CountAsync();

        Assert.That(result, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Id, Is.EqualTo(tourLog.Id));
            Assert.That(result.UserId, Is.EqualTo(TestData.TestUserId));
            Assert.That(logCount, Is.EqualTo(1));
        }
    }

    [Test]
    public void GetTourLogsByTourId_WithExistingTourId_ReturnsAllTourLogs()
    {
        var tourLogs = TestData.SampleTourLogPersistenceList();
        _context.TourLogsPersistence.AddRange(tourLogs);
        _context.SaveChanges();

        var result = _repository.GetTourLogsByTourId(TestData.TestGuid, TestData.TestUserId).ToList();

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Has.Count.EqualTo(tourLogs.Count));
    }

    [Test]
    public void GetTourLogsByTourId_WithNonExistentTourId_ReturnsEmptyList()
    {
        var nonExistentTourId = Guid.NewGuid();

        var result = _repository.GetTourLogsByTourId(nonExistentTourId, TestData.TestUserId);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void GetTourLogsByTourId_WithDifferentUser_ReturnsEmpty()
    {
        var tourLogs = TestData.SampleTourLogPersistenceList();
        _context.TourLogsPersistence.AddRange(tourLogs);
        _context.SaveChanges();

        var result = _repository.GetTourLogsByTourId(TestData.TestGuid, "other-user-id").ToList();

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void GetTourLogById_WithExistingId_ReturnsTourLog()
    {
        var tourLog = TestData.SampleTourLogPersistence();
        _context.TourLogsPersistence.Add(tourLog);
        _context.SaveChanges();

        var result = _repository.GetTourLogById(tourLog.Id, TestData.TestUserId);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Id, Is.EqualTo(tourLog.Id));
    }

    [Test]
    public void GetTourLogById_WithNonExistingId_ReturnsNull()
    {
        var nonExistingId = Guid.NewGuid();

        var result = _repository.GetTourLogById(nonExistingId, TestData.TestUserId);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task UpdateTourLogAsync_WithExistingTourLog_ReturnsUpdatedTourLog()
    {
        var tourLog = TestData.SampleTourLogPersistence();
        _context.TourLogsPersistence.Add(tourLog);
        await _context.SaveChangesAsync();
        tourLog.Comment = "Updated comment";

        var result = await _repository.UpdateTourLogAsync(tourLog, TestData.TestUserId);
        var dbTourLog = await _context.TourLogsPersistence
            .FirstAsync(t => t.Id == tourLog.Id);

        Assert.That(result, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Comment, Is.EqualTo("Updated comment"));
            Assert.That(dbTourLog.Comment, Is.EqualTo("Updated comment"));
        }
    }

    [Test]
    public async Task DeleteTourLogAsync_WithExistingId_RemovesTourLogFromDatabase()
    {
        var tourLog = TestData.SampleTourLogPersistence();
        _context.TourLogsPersistence.Add(tourLog);
        await _context.SaveChangesAsync();

        await _repository.DeleteTourLogAsync(tourLog.Id, TestData.TestUserId);

        Assert.That(await _context.TourLogsPersistence.CountAsync(), Is.Zero);
    }

    [Test]
    public async Task DeleteTourLogAsync_WithDifferentUser_DoesNotDelete()
    {
        var tourLog = TestData.SampleTourLogPersistence();
        _context.TourLogsPersistence.Add(tourLog);
        await _context.SaveChangesAsync();

        await _repository.DeleteTourLogAsync(tourLog.Id, "other-user-id");

        Assert.That(await _context.TourLogsPersistence.CountAsync(), Is.EqualTo(1));
    }
}
