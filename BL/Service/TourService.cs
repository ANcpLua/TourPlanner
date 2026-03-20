using BL.DomainModel;
using BL.Interface;
using DAL.Interface;
using DAL.PersistenceModel;
using MapsterMapper;

namespace BL.Service;

public class TourService(ITourRepository tourRepository, IMapper mapper, IUserContext userContext) : ITourService
{
    public async Task<TourDomain> CreateTourAsync(TourDomain tour, CancellationToken cancellationToken = default)
    {
        var tourPersistence = mapper.Map<TourPersistence>(tour);
        var createdTour = await tourRepository.CreateTourAsync(tourPersistence, userContext.UserId, cancellationToken);
        return mapper.Map<TourDomain>(createdTour);
    }

    public IEnumerable<TourDomain> GetAllTours()
    {
        var tours = tourRepository.GetAllTours(userContext.UserId);
        return mapper.Map<IEnumerable<TourDomain>>(tours);
    }

    public TourDomain? GetTourById(Guid id)
    {
        var tourPersistence = tourRepository.GetTourById(id, userContext.UserId);
        return tourPersistence is null ? null : mapper.Map<TourDomain>(tourPersistence);
    }

    public async Task<TourDomain> UpdateTourAsync(TourDomain tour, CancellationToken cancellationToken = default)
    {
        var tourPersistence = mapper.Map<TourPersistence>(tour);
        var updatedTour = await tourRepository.UpdateTourAsync(tourPersistence, userContext.UserId, cancellationToken);
        return mapper.Map<TourDomain>(updatedTour);
    }

    public Task DeleteTourAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return tourRepository.DeleteTourAsync(id, userContext.UserId, cancellationToken);
    }

    public IQueryable<TourDomain> SearchTours(string searchText)
    {
        var tourPersistence = tourRepository.SearchToursAsync(searchText, userContext.UserId);
        return tourPersistence.Select(t => mapper.Map<TourDomain>(t));
    }
}
