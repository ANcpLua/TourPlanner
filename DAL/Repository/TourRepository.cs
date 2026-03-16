using DAL.Infrastructure;
using DAL.Interface;
using DAL.PersistenceModel;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repository;

public class TourRepository(TourPlannerContext dbContext) : ITourRepository
{
    public async Task<TourPersistence> CreateTourAsync(TourPersistence tour,
        CancellationToken cancellationToken = default)
    {
        dbContext.Set<TourPersistence>().Add(tour);
        await dbContext.SaveChangesAsync(cancellationToken);
        return tour;
    }

    public IEnumerable<TourPersistence> GetAllTours()
    {
        return [.. dbContext
            .Set<TourPersistence>()
            .Include(t => t.TourLogPersistence)];
    }

    public TourPersistence? GetTourById(Guid id)
    {
        return dbContext
            .Set<TourPersistence>()
            .Include(t => t.TourLogPersistence)
            .FirstOrDefault(t => t.Id == id);
    }

    public async Task<TourPersistence> UpdateTourAsync(TourPersistence tour,
        CancellationToken cancellationToken = default)
    {
        dbContext.Set<TourPersistence>().Update(tour);
        await dbContext.SaveChangesAsync(cancellationToken);
        return tour;
    }

    public async Task DeleteTourAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tour = await dbContext.Set<TourPersistence>().FindAsync([id], cancellationToken);
        if (tour is not null)
        {
            dbContext.Set<TourPersistence>().Remove(tour);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public IQueryable<TourPersistence> SearchToursAsync(string searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText)) return dbContext.ToursPersistence;

        return dbContext
            .ToursPersistence.Include(t => t.TourLogPersistence)
            .Where(t =>
                t.Name.Contains(searchText) ||
                t.Description.Contains(searchText) ||
                t.From.Contains(searchText) ||
                t.To.Contains(searchText) ||
                t.TourLogPersistence.Any(tl => tl.Comment.Contains(searchText))
            );
    }
}
