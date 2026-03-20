using BL.Interface;
using Contracts.Routes;
using Microsoft.AspNetCore.Http.HttpResults;

namespace API.Endpoints;

public static class RouteEndpoints
{
    public static IEndpointRouteBuilder MapRouteEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var routes = endpoints.MapGroup("/api/routes").WithTags("Routes");
        routes.MapPost("/resolve", ResolveRoute);
        return endpoints;
    }

    internal static async Task<Ok<ResolveRouteResponse>> ResolveRoute(
        ResolveRouteRequest request,
        IRouteService routeService,
        CancellationToken cancellationToken)
    {
        var (distance, duration) = await routeService.ResolveRouteAsync(
            (request.FromLatitude!.Value, request.FromLongitude!.Value),
            (request.ToLatitude!.Value, request.ToLongitude!.Value),
            request.TransportType,
            cancellationToken
        );

        return TypedResults.Ok(new ResolveRouteResponse
        {
            Distance = distance,
            Duration = duration
        });
    }
}
