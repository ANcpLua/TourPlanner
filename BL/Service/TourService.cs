using BL.DomainModel;
using BL.Interface;
using DAL.Interface;
using DAL.PersistenceModel;
using MapsterMapper;

namespace BL.Service;

public class TourService(ITourRepository tourRepository, IMapper mapper) : ITourService
{
    public async Task<TourDomain> CreateTourAsync(TourDomain tour, CancellationToken cancellationToken = default)
    {
        var tourPersistence = mapper.Map<TourPersistence>(tour);
        var createdTour = await tourRepository.CreateTourAsync(tourPersistence, cancellationToken);
        return mapper.Map<TourDomain>(createdTour);
    }

    public IEnumerable<TourDomain> GetAllTours()
    {
        var tours = tourRepository.GetAllTours();
        return mapper.Map<IEnumerable<TourDomain>>(tours);
    }

    public TourDomain? GetTourById(Guid id)
    {
        var tourPersistence = tourRepository.GetTourById(id);
        return tourPersistence is null ? null : mapper.Map<TourDomain>(tourPersistence);
    }

    public async Task<TourDomain> UpdateTourAsync(TourDomain tour, CancellationToken cancellationToken = default)
    {
        var tourPersistence = mapper.Map<TourPersistence>(tour);
        var updatedTour = await tourRepository.UpdateTourAsync(tourPersistence, cancellationToken);
        return mapper.Map<TourDomain>(updatedTour);
    }

    public Task DeleteTourAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return tourRepository.DeleteTourAsync(id, cancellationToken);
    }

    public IQueryable<TourDomain> SearchTours(string searchText)
    {
        var tourPersistence = tourRepository.SearchToursAsync(searchText);
        return tourPersistence.Select(t => mapper.Map<TourDomain>(t));
    }
}