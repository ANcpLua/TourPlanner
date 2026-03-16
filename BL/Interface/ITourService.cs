using BL.DomainModel;

namespace BL.Interface;

public interface ITourService
{
    Task<TourDomain> CreateTourAsync(TourDomain tour, CancellationToken cancellationToken = default);
    IEnumerable<TourDomain> GetAllTours();
    TourDomain? GetTourById(Guid id);
    Task<TourDomain> UpdateTourAsync(TourDomain tour, CancellationToken cancellationToken = default);
    Task DeleteTourAsync(Guid id, CancellationToken cancellationToken = default);
    IQueryable<TourDomain> SearchTours(string searchText);
}