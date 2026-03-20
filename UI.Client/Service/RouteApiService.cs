using System.Net.Http.Json;
using System.Text.Json;
using Contracts.Routes;
using UI.Decorator;
using UI.Service.Interface;

namespace UI.Service;

public class RouteApiService(HttpClient httpClient) : IRouteApiService
{
    [UiMethodDecorator]
    public async Task<(double Distance, double Duration)> FetchRouteDataAsync(
        (double Latitude, double Longitude) from,
        (double Latitude, double Longitude) to,
        string transportType)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/routes/resolve",
            new ResolveRouteRequest
            {
                FromLatitude = from.Latitude,
                FromLongitude = from.Longitude,
                ToLatitude = to.Latitude,
                ToLongitude = to.Longitude,
                TransportType = transportType
            });
        response.EnsureSuccessStatusCode();

        try
        {
            var route = await response.Content.ReadFromJsonAsync<ResolveRouteResponse>()
                        ?? throw new HttpRequestException("Route resolution returned no content.");

            return (route.Distance, route.Duration);
        }
        catch (JsonException exception)
        {
            throw new HttpRequestException("Route resolution returned invalid content.", exception);
        }
    }
}
