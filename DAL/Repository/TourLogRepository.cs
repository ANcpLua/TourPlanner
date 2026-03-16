using DAL.Infrastructure;
using DAL.Interface;
using DAL.PersistenceModel;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repository;

public class TourLogRepository(TourPlannerContext dbContext) : ITourLogRepository
{
    public async Task<TourLogPersistence> CreateTourLogAsync(TourLogPersistence newTourLogPersistence,
        CancellationToken cancellationToken = default)
    {
        dbContext.TourLogsPersistence.Add(newTourLogPersistence);
        await dbContext.SaveChangesAsync(cancellationToken);
        return newTourLogPersistence;
    }

    public IEnumerable<TourLogPersistence> GetTourLogsByTourId(Guid tourId)
    {
        return [.. dbContext
            .TourLogsPersistence.Where(t => t.TourPersistenceId == tourId)];
    }

    public TourLogPersistence? GetTourLogById(Guid id)
    {
        return dbContext.TourLogsPersistence.FirstOrDefault(t => t.Id == id);
    }

    public async Task<TourLogPersistence> UpdateTourLogAsync(TourLogPersistence updatedTourLogPersistence,
        CancellationToken cancellationToken = default)
    {
        dbContext.TourLogsPersistence.Update(updatedTourLogPersistence);
        await dbContext.SaveChangesAsync(cancellationToken);
        return updatedTourLogPersistence;
    }

    public async Task DeleteTourLogAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tourLogPersistence =
            await dbContext.TourLogsPersistence.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (tourLogPersistence is not null)
        {
            dbContext.TourLogsPersistence.Remove(tourLogPersistence);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
