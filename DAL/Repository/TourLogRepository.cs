using DAL.Infrastructure;
using DAL.Interface;
using DAL.PersistenceModel;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repository;

public class TourLogRepository(TourPlannerContext dbContext) : ITourLogRepository
{
    public async Task<TourLogPersistence> CreateTourLogAsync(TourLogPersistence newTourLogPersistence, string userId,
        CancellationToken cancellationToken = default)
    {
        newTourLogPersistence.UserId = userId;
        dbContext.TourLogsPersistence.Add(newTourLogPersistence);
        await dbContext.SaveChangesAsync(cancellationToken);
        return newTourLogPersistence;
    }

    public IEnumerable<TourLogPersistence> GetTourLogsByTourId(Guid tourId, string userId)
    {
        return [.. dbContext
            .TourLogsPersistence.Where(t => t.TourPersistenceId == tourId && t.UserId == userId)];
    }

    public TourLogPersistence? GetTourLogById(Guid id, string userId)
    {
        return dbContext.TourLogsPersistence.FirstOrDefault(t => t.Id == id && t.UserId == userId);
    }

    public async Task<TourLogPersistence> UpdateTourLogAsync(TourLogPersistence updatedTourLogPersistence, string userId,
        CancellationToken cancellationToken = default)
    {
        var existing = await dbContext.TourLogsPersistence
            .FirstOrDefaultAsync(t => t.Id == updatedTourLogPersistence.Id && t.UserId == userId, cancellationToken)
            ?? throw new InvalidOperationException("Tour log not found or access denied.");
        dbContext.Entry(existing).CurrentValues.SetValues(updatedTourLogPersistence);
        existing.UserId = userId;
        await dbContext.SaveChangesAsync(cancellationToken);
        return existing;
    }

    public async Task DeleteTourLogAsync(Guid id, string userId, CancellationToken cancellationToken = default)
    {
        var tourLogPersistence =
            await dbContext.TourLogsPersistence.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId, cancellationToken);
        if (tourLogPersistence is not null)
        {
            dbContext.TourLogsPersistence.Remove(tourLogPersistence);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
