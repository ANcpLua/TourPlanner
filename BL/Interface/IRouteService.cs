namespace BL.Interface;

public interface IRouteService
{
    Task<(double Distance, double Duration)> ResolveRouteAsync(
        (double Latitude, double Longitude) from,
        (double Latitude, double Longitude) to,
        string transportType,
        CancellationToken cancellationToken = default
    );
}
