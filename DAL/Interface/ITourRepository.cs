using DAL.PersistenceModel;

namespace DAL.Interface;

public interface ITourRepository
{
    Task<TourPersistence> CreateTourAsync(TourPersistence tour, string userId, CancellationToken cancellationToken = default);
    IEnumerable<TourPersistence> GetAllTours(string userId);
    TourPersistence? GetTourById(Guid id, string userId);
    Task<TourPersistence> UpdateTourAsync(TourPersistence tour, string userId, CancellationToken cancellationToken = default);
    Task DeleteTourAsync(Guid id, string userId, CancellationToken cancellationToken = default);
    IQueryable<TourPersistence> SearchToursAsync(string searchText, string userId);
}
