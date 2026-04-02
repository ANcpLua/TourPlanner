using DAL.PersistenceModel;
using DAL.Repository;
using Microsoft.EntityFrameworkCore;
using Tests.DAL.Infrastructure;

namespace Tests.DAL;

[TestFixture]
public sealed class TourRepositoryTests : SqliteRepositoryTestBase
{
    private TourRepository _repository = null!;
    private static readonly string[] AllOwnedTourNames = ["One", "Two"];

    protected override void OnSetUp()
    {
        _repository = new TourRepository(DbContext);
    }

    [Test]
    public async Task CreateTourAsync_AssignsUserAndPersistsTour()
    {
        var tour = NewTour(name: "Created Tour", userId: "ignored-user");

        var created = await _repository.CreateTourAsync(tour, TestConstants.TestUserId);

        await using var verificationContext = CreateContext();
        var persisted = await verificationContext.ToursPersistence.SingleAsync(t => t.Id == created.Id);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(created.UserId, Is.EqualTo(TestConstants.TestUserId));
            Assert.That(persisted.Name, Is.EqualTo("Created Tour"));
            Assert.That(persisted.UserId, Is.EqualTo(TestConstants.TestUserId));
        }
    }

    [Test]
    public void CreateTourAsync_MissingRequiredName_ThrowsDbUpdateException()
    {
        var tour = NewTour();
        tour.Name = null!;

        Assert.That(async () => await _repository.CreateTourAsync(tour, TestConstants.TestUserId),
            Throws.TypeOf<DbUpdateException>());
    }

    [Test]
    public void GetAllTours_ReturnsOnlyOwnedToursWithLoadedLogs()
    {
        var ownedTour = SeedTour(name: "Owned Tour", userId: TestConstants.TestUserId, logComment: "owned-log");
        SeedTour(name: "Other Tour", userId: "other-user", logComment: "other-log");

        var tours = _repository.GetAllTours(TestConstants.TestUserId).ToList();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(tours, Has.Count.EqualTo(1));
            Assert.That(tours.Single().Id, Is.EqualTo(ownedTour.Id));
            Assert.That(tours.Single().TourLogPersistence.Select(static log => log.Comment), Contains.Item("owned-log"));
        }
    }

    [Test]
    public void GetTourById_WithDifferentUser_ReturnsNull()
    {
        var tour = SeedTour();

        var result = _repository.GetTourById(tour.Id, "other-user");

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task UpdateTourAsync_PersistsChangesForOwner()
    {
        var tour = SeedTour(name: "Before Update");
        tour.Name = "After Update";
        tour.Description = "Updated description";

        var updated = await _repository.UpdateTourAsync(tour, TestConstants.TestUserId);

        await using var verificationContext = CreateContext();
        var persisted = await verificationContext.ToursPersistence.SingleAsync(t => t.Id == tour.Id);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(updated.Name, Is.EqualTo("After Update"));
            Assert.That(persisted.Name, Is.EqualTo("After Update"));
            Assert.That(persisted.Description, Is.EqualTo("Updated description"));
            Assert.That(persisted.UserId, Is.EqualTo(TestConstants.TestUserId));
        }
    }

    [Test]
    public void UpdateTourAsync_WithDifferentUser_ThrowsInvalidOperationException()
    {
        var tour = SeedTour();
        tour.Name = "Should Fail";

        Assert.That(async () => await _repository.UpdateTourAsync(tour, "other-user"),
            Throws.TypeOf<InvalidOperationException>()
                .With.Message.EqualTo("Tour not found or access denied."));
    }

    [Test]
    public async Task DeleteTourAsync_RemovesOnlyOwnedTour()
    {
        var ownedTour = SeedTour(name: "Owned Tour", userId: TestConstants.TestUserId);
        var otherUserTour = SeedTour(name: "Other Tour", userId: "other-user");

        await _repository.DeleteTourAsync(ownedTour.Id, TestConstants.TestUserId);

        await using var verificationContext = CreateContext();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(await verificationContext.ToursPersistence.AnyAsync(t => t.Id == ownedTour.Id), Is.False);
            Assert.That(await verificationContext.ToursPersistence.AnyAsync(t => t.Id == otherUserTour.Id), Is.True);
        }
    }

    [Test]
    public async Task SearchToursAsync_MatchesTourFieldsAndLogCommentsForOwner()
    {
        var commentMatch = SeedTour(name: "Lake Route", userId: TestConstants.TestUserId, logComment: "glacier-view");
        SeedTour(name: "glacier-view hidden", userId: "other-user", logComment: "glacier-view");
        SeedTour(name: "City Walk", userId: TestConstants.TestUserId, logComment: "urban");

        var results = await _repository.SearchToursAsync("glacier-view", TestConstants.TestUserId).ToListAsync();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(results, Has.Count.EqualTo(1));
            Assert.That(results.Single().Id, Is.EqualTo(commentMatch.Id));
            Assert.That(results.Single().TourLogPersistence.Select(static log => log.Comment), Contains.Item("glacier-view"));
        }
    }

    [Test]
    public async Task SearchToursAsync_BlankSearch_ReturnsAllOwnedTours()
    {
        SeedTour(name: "One", userId: TestConstants.TestUserId);
        SeedTour(name: "Two", userId: TestConstants.TestUserId);
        SeedTour(name: "Three", userId: "other-user");

        var results = await _repository.SearchToursAsync("   ", TestConstants.TestUserId).ToListAsync();

        Assert.That(results.Select(static t => t.Name), Is.EquivalentTo(AllOwnedTourNames));
    }

    private TourPersistence SeedTour(string name = "Sample Tour", string userId = TestConstants.TestUserId, string? logComment = null)
    {
        var tour = NewTour(name, userId);
        DbContext.ToursPersistence.Add(tour);
        DbContext.SaveChanges();

        if (logComment is not null)
        {
            DbContext.TourLogsPersistence.Add(NewLog(tour.Id, userId, logComment));
            DbContext.SaveChanges();
        }

        return tour;
    }

    private static TourPersistence NewTour(string name = "Sample Tour", string userId = TestConstants.TestUserId)
    {
        var tour = TourTestData.SampleTourPersistence(name);
        tour.Id = Guid.NewGuid();
        tour.UserId = userId;
        tour.TourLogPersistence = [];
        return tour;
    }

    private static TourLogPersistence NewLog(Guid tourId, string userId, string comment)
    {
        var log = TourLogTestData.SampleTourLogPersistence();
        log.Id = Guid.NewGuid();
        log.UserId = userId;
        log.TourPersistenceId = tourId;
        log.Comment = comment;
        return log;
    }
}
