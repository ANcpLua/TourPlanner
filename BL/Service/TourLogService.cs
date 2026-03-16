using BL.DomainModel;
using BL.Interface;
using DAL.Interface;
using DAL.PersistenceModel;
using MapsterMapper;

namespace BL.Service;

public class TourLogService(ITourLogRepository tourLogRepository, IMapper mapper) : ITourLogService
{
    public async Task<TourLogDomain> CreateTourLogAsync(TourLogDomain tourLog,
        CancellationToken cancellationToken = default)
    {
        var tourLogPersistence = mapper.Map<TourLogPersistence>(tourLog);
        var createdTourLogPersistence =
            await tourLogRepository.CreateTourLogAsync(tourLogPersistence, cancellationToken);
        return mapper.Map<TourLogDomain>(createdTourLogPersistence);
    }

    public IEnumerable<TourLogDomain> GetTourLogsByTourId(Guid tourId)
    {
        var tourLogPersistence = tourLogRepository.GetTourLogsByTourId(tourId);
        return mapper.Map<IEnumerable<TourLogDomain>>(tourLogPersistence);
    }

    public TourLogDomain? GetTourLogById(Guid id)
    {
        var tourLogPersistence = tourLogRepository.GetTourLogById(id);
        return tourLogPersistence is null ? null : mapper.Map<TourLogDomain>(tourLogPersistence);
    }

    public async Task<TourLogDomain> UpdateTourLogAsync(TourLogDomain tourLog,
        CancellationToken cancellationToken = default)
    {
        var tourLogPersistence = mapper.Map<TourLogPersistence>(tourLog);
        var updatedTourLogPersistence =
            await tourLogRepository.UpdateTourLogAsync(tourLogPersistence, cancellationToken);
        return mapper.Map<TourLogDomain>(updatedTourLogPersistence);
    }

    public Task DeleteTourLogAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return tourLogRepository.DeleteTourLogAsync(id, cancellationToken);
    }
}