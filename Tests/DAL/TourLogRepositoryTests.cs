using DAL.PersistenceModel;
using DAL.Repository;
using Microsoft.EntityFrameworkCore;
using Tests.DAL.Infrastructure;

namespace Tests.DAL;

[TestFixture]
public sealed class TourLogRepositoryTests : SqliteRepositoryTestBase
{
    private TourLogRepository _repository = null!;

    protected override void OnSetUp()
    {
        _repository = new TourLogRepository(DbContext);
    }

    [Test]
    public async Task CreateTourLogAsync_AssignsUserAndPersistsLog()
    {
        var tour = SeedTour();
        var log = NewLog(tour.Id, userId: "ignored-user", comment: "created-log");

        var created = await _repository.CreateTourLogAsync(log, TestConstants.TestUserId);

        await using var verificationContext = CreateContext();
        var persisted = await verificationContext.TourLogsPersistence.SingleAsync(entry => entry.Id == created.Id);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(created.UserId, Is.EqualTo(TestConstants.TestUserId));
            Assert.That(persisted.Comment, Is.EqualTo("created-log"));
            Assert.That(persisted.UserId, Is.EqualTo(TestConstants.TestUserId));
        }
    }

    [Test]
    public void CreateTourLogAsync_RequiresExistingTour()
    {
        var log = NewLog(Guid.NewGuid(), TestConstants.TestUserId, "orphan-log");

        Assert.That(async () => await _repository.CreateTourLogAsync(log, TestConstants.TestUserId),
            Throws.TypeOf<DbUpdateException>());
    }

    [Test]
    public void GetTourLogsByTourId_ReturnsOnlyOwnedLogs()
    {
        var tour = SeedTour();
        SeedLog(tour.Id, TestConstants.TestUserId, "owned-log");
        SeedLog(tour.Id, "other-user", "other-log");

        var logs = _repository.GetTourLogsByTourId(tour.Id, TestConstants.TestUserId).ToList();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(logs, Has.Count.EqualTo(1));
            Assert.That(logs.Single().Comment, Is.EqualTo("owned-log"));
            Assert.That(logs.Single().UserId, Is.EqualTo(TestConstants.TestUserId));
        }
    }

    [Test]
    public void GetTourLogById_WithDifferentUser_ReturnsNull()
    {
        var tour = SeedTour();
        var log = SeedLog(tour.Id, TestConstants.TestUserId, "private-log");

        var result = _repository.GetTourLogById(log.Id, "other-user");

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task UpdateTourLogAsync_PersistsChangesForOwner()
    {
        var tour = SeedTour();
        var log = SeedLog(tour.Id, TestConstants.TestUserId, "before-update");
        log.Comment = "after-update";
        log.Rating = 5;

        var updated = await _repository.UpdateTourLogAsync(log, TestConstants.TestUserId);

        await using var verificationContext = CreateContext();
        var persisted = await verificationContext.TourLogsPersistence.SingleAsync(entry => entry.Id == log.Id);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(updated.Comment, Is.EqualTo("after-update"));
            Assert.That(persisted.Comment, Is.EqualTo("after-update"));
            Assert.That(persisted.Rating, Is.EqualTo(5));
        }
    }

    [Test]
    public void UpdateTourLogAsync_WithDifferentUser_ThrowsInvalidOperationException()
    {
        var tour = SeedTour();
        var log = SeedLog(tour.Id, TestConstants.TestUserId, "private-log");
        log.Comment = "forbidden-update";

        Assert.That(async () => await _repository.UpdateTourLogAsync(log, "other-user"),
            Throws.TypeOf<InvalidOperationException>()
                .With.Message.EqualTo("Tour log not found or access denied."));
    }

    [Test]
    public async Task DeleteTourLogAsync_RemovesOnlyOwnedLog()
    {
        var tour = SeedTour();
        var ownedLog = SeedLog(tour.Id, TestConstants.TestUserId, "owned-log");
        var otherLog = SeedLog(tour.Id, "other-user", "other-log");

        await _repository.DeleteTourLogAsync(ownedLog.Id, TestConstants.TestUserId);

        await using var verificationContext = CreateContext();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(await verificationContext.TourLogsPersistence.AnyAsync(entry => entry.Id == ownedLog.Id), Is.False);
            Assert.That(await verificationContext.TourLogsPersistence.AnyAsync(entry => entry.Id == otherLog.Id), Is.True);
        }
    }

    private TourPersistence SeedTour(string userId = TestConstants.TestUserId)
    {
        var tour = TourTestData.SampleTourPersistence();
        tour.Id = Guid.NewGuid();
        tour.UserId = userId;
        tour.TourLogPersistence = [];
        DbContext.ToursPersistence.Add(tour);
        DbContext.SaveChanges();
        return tour;
    }

    private TourLogPersistence SeedLog(Guid tourId, string userId, string comment)
    {
        var log = NewLog(tourId, userId, comment);
        DbContext.TourLogsPersistence.Add(log);
        DbContext.SaveChanges();
        return log;
    }

    private static TourLogPersistence NewLog(Guid tourId, string userId, string comment)
    {
        var log = TourLogTestData.SampleTourLogPersistence();
        log.Id = Guid.NewGuid();
        log.TourPersistenceId = tourId;
        log.UserId = userId;
        log.Comment = comment;
        return log;
    }
}
