using DAL.PersistenceModel;

namespace DAL.Interface;

public interface ITourRepository
{
    Task<TourPersistence> CreateTourAsync(TourPersistence tour, CancellationToken cancellationToken = default);
    IEnumerable<TourPersistence> GetAllTours();
    TourPersistence? GetTourById(Guid id);
    Task<TourPersistence> UpdateTourAsync(TourPersistence tour, CancellationToken cancellationToken = default);
    Task DeleteTourAsync(Guid id, CancellationToken cancellationToken = default);
    IQueryable<TourPersistence> SearchToursAsync(string searchText);
}