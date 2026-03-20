using DAL.Infrastructure;
using DAL.Repository;
using Microsoft.EntityFrameworkCore;

namespace Tests.DAL;

[TestFixture]
public class TourRepositoryTests
{
    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<TourPlannerContext>()
            .UseInMemoryDatabase($"TourPlannerTestDb_{Guid.NewGuid()}")
            .Options;
        _context = new TourPlannerContext(options);
        _repository = new TourRepository(_context);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    private TourPlannerContext _context = null!;
    private TourRepository _repository = null!;

    [Test]
    public async Task CreateTourAsync_WithValidTour_ReturnsSavedTour()
    {
        var tour = TestData.SampleTourPersistence();

        var result = await _repository.CreateTourAsync(tour, TestData.TestUserId);
        var tourCount = await _context.ToursPersistence.CountAsync();

        Assert.That(result, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Id, Is.EqualTo(tour.Id));
            Assert.That(result.Name, Is.EqualTo(tour.Name));
            Assert.That(result.UserId, Is.EqualTo(TestData.TestUserId));
            Assert.That(tourCount, Is.EqualTo(1));
        }
    }

    [Test]
    public void CreateTourAsync_WithInvalidTour_ThrowsDbUpdateException()
    {
        var tour = TestData.SampleTourPersistence();
        tour.Name = null!;

        Assert.That(async () => await _repository.CreateTourAsync(tour, TestData.TestUserId), Throws.InstanceOf<DbUpdateException>());
    }

    [Test]
    public void GetAllTours_WithExistingTours_ReturnsOnlyOwnedTours()
    {
        var tours = TestData.SampleTourPersistenceList();
        _context.ToursPersistence.AddRange(tours);
        _context.SaveChanges();

        var result = _repository.GetAllTours(TestData.TestUserId).ToList();

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Has.Count.EqualTo(tours.Count));
    }

    [Test]
    public void GetAllTours_WithNoExistingTours_ReturnsEmptyList()
    {
        var result = _repository.GetAllTours(TestData.TestUserId).ToList();

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void GetAllTours_WithDifferentUser_ReturnsEmpty()
    {
        var tours = TestData.SampleTourPersistenceList();
        _context.ToursPersistence.AddRange(tours);
        _context.SaveChanges();

        var result = _repository.GetAllTours("other-user-id").ToList();

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void GetTourById_WithExistingId_ReturnsTour()
    {
        var tour = TestData.SampleTourPersistence();
        _context.ToursPersistence.Add(tour);
        _context.SaveChanges();

        var result = _repository.GetTourById(tour.Id, TestData.TestUserId);

        Assert.That(result, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Id, Is.EqualTo(tour.Id));
            Assert.That(result.Name, Is.EqualTo(tour.Name));
        }
    }

    [Test]
    public void GetTourById_WithNonExistingId_ReturnsNull()
    {
        var nonExistingId = Guid.NewGuid();

        var result = _repository.GetTourById(nonExistingId, TestData.TestUserId);

        Assert.That(result, Is.Null);
    }

    [Test]
    public void GetTourById_WithDifferentUser_ReturnsNull()
    {
        var tour = TestData.SampleTourPersistence();
        _context.ToursPersistence.Add(tour);
        _context.SaveChanges();

        var result = _repository.GetTourById(tour.Id, "other-user-id");

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task UpdateTourAsync_WithExistingTour_ReturnsUpdatedTour()
    {
        var tour = TestData.SampleTourPersistence();
        await _context.ToursPersistence.AddAsync(tour);
        await _context.SaveChangesAsync();

        tour.Name = "Updated Tour Name";

        var result = await _repository.UpdateTourAsync(tour, TestData.TestUserId);
        var dbTour = await _context.ToursPersistence.FirstAsync(t => t.Id == tour.Id);

        Assert.That(result, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Name, Is.EqualTo("Updated Tour Name"));
            Assert.That(dbTour.Name, Is.EqualTo("Updated Tour Name"));
        }
    }

    [Test]
    public void UpdateTourAsync_WithNonExistingTour_ThrowsInvalidOperationException()
    {
        var tour = TestData.SampleTourPersistence();

        Assert.That(async () => await _repository.UpdateTourAsync(tour, TestData.TestUserId),
            Throws.InstanceOf<InvalidOperationException>());
    }

    [Test]
    public async Task DeleteTourAsync_WithExistingId_RemovesTourFromDatabase()
    {
        var tour = TestData.SampleTourPersistence();
        _context.ToursPersistence.Add(tour);
        await _context.SaveChangesAsync();

        await _repository.DeleteTourAsync(tour.Id, TestData.TestUserId);

        Assert.That(await _context.ToursPersistence.CountAsync(), Is.Zero);
    }

    [Test]
    public async Task DeleteTourAsync_WithNonExistingId_DoesNotRemoveAnyTourFromDatabase()
    {
        var tour = TestData.SampleTourPersistence();
        _context.ToursPersistence.Add(tour);
        await _context.SaveChangesAsync();

        await _repository.DeleteTourAsync(Guid.NewGuid(), TestData.TestUserId);

        Assert.That(await _context.ToursPersistence.CountAsync(), Is.EqualTo(1));
    }

    [Test]
    public async Task DeleteTourAsync_WithDifferentUser_DoesNotDelete()
    {
        var tour = TestData.SampleTourPersistence();
        _context.ToursPersistence.Add(tour);
        await _context.SaveChangesAsync();

        await _repository.DeleteTourAsync(tour.Id, "other-user-id");

        Assert.That(await _context.ToursPersistence.CountAsync(), Is.EqualTo(1));
    }

    [Test]
    public async Task SearchToursAsync_WithMatchingSearchText_ReturnsMatchingTours()
    {
        var tour = TestData.SampleTourPersistence();
        await _context.ToursPersistence.AddAsync(tour);
        await _context.SaveChangesAsync();

        var result = await _repository.SearchToursAsync("Sample", TestData.TestUserId).ToListAsync();

        Assert.That(result, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result.First().Name, Is.EqualTo("Sample Tour"));
        }
    }

    [Test]
    public async Task SearchToursAsync_WithNonMatchingSearchText_ReturnsEmptyList()
    {
        var tours = TestData.SampleTourPersistenceList();
        await _context.ToursPersistence.AddRangeAsync(tours);
        await _context.SaveChangesAsync();

        var result = _repository.SearchToursAsync("NonExistingTour", TestData.TestUserId);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count(), Is.Zero);
    }

    [Test]
    public void SearchToursAsync_WithNullOrEmptySearchText_ReturnsAllOwnedTours()
    {
        var tours = TestData.SampleTourPersistenceList();
        _context.ToursPersistence.AddRange(tours);
        _context.SaveChanges();

        var result = _repository.SearchToursAsync(null!, TestData.TestUserId);

        Assert.That(result.Count(), Is.EqualTo(tours.Count));
    }
}
