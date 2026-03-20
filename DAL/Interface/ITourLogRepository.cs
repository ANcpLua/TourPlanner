using DAL.PersistenceModel;

namespace DAL.Interface;

public interface ITourLogRepository
{
    Task<TourLogPersistence> CreateTourLogAsync(TourLogPersistence newTourLogPersistence, string userId,
        CancellationToken cancellationToken = default);

    IEnumerable<TourLogPersistence> GetTourLogsByTourId(Guid tourId, string userId);
    TourLogPersistence? GetTourLogById(Guid id, string userId);

    Task<TourLogPersistence> UpdateTourLogAsync(TourLogPersistence updatedTourLogPersistence, string userId,
        CancellationToken cancellationToken = default);

    Task DeleteTourLogAsync(Guid id, string userId, CancellationToken cancellationToken = default);
}
