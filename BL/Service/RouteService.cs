using BL.Interface;
using DAL.Interface;

namespace BL.Service;

public class RouteService(IRouteRepository routeRepository) : IRouteService
{
    public Task<(double Distance, double Duration)> ResolveRouteAsync(
        (double Latitude, double Longitude) from,
        (double Latitude, double Longitude) to,
        string transportType,
        CancellationToken cancellationToken = default
    )
    {
        return routeRepository.ResolveRouteAsync(from, to, transportType, cancellationToken);
    }
}
