using DAL.Infrastructure;
using DAL.Interface;
using DAL.PersistenceModel;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repository;

public class TourRepository(TourPlannerContext dbContext) : ITourRepository
{
    public async Task<TourPersistence> CreateTourAsync(TourPersistence tour)
    {
        dbContext.Set<TourPersistence>().Add(tour);
        await dbContext.SaveChangesAsync();
        return tour;
    }

    public IEnumerable<TourPersistence> GetAllTours() =>
        dbContext
            .Set<TourPersistence>()
            .Include(t => t.TourLogPersistence)
            .ToList();

    public TourPersistence? GetTourById(Guid id) =>
        dbContext
            .Set<TourPersistence>()
            .Include(t => t.TourLogPersistence)
            .FirstOrDefault(t => t.Id == id);

    public async Task<TourPersistence> UpdateTourAsync(TourPersistence tour)
    {
        dbContext.Set<TourPersistence>().Update(tour);
        await dbContext.SaveChangesAsync();
        return tour;
    }

    public async Task DeleteTourAsync(Guid id)
    {
        var tour = await dbContext.Set<TourPersistence>().FindAsync(id);
        if (tour is not null)
        {
            dbContext.Set<TourPersistence>().Remove(tour);
            await dbContext.SaveChangesAsync();
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
