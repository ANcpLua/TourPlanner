using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace API.Extensions;

public static class HealthEndpointExtensions
{
    public const string HealthPath = API.Endpoints.ApiRoute.Health;
    public const string HealthLivePath = API.Endpoints.ApiRoute.HealthLive;
    public const string HealthReadyPath = API.Endpoints.ApiRoute.HealthReady;
    public const string SelfTag = API.Endpoints.ApiRoute.HealthChecks.Self;
    public const string ReadyTag = API.Endpoints.ApiRoute.HealthChecks.Ready;

    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapHealthChecks(HealthLivePath, CreateOptions(SelfTag)).AllowAnonymous();
        app.MapHealthChecks(HealthReadyPath, CreateOptions(ReadyTag)).AllowAnonymous();
        app.MapHealthChecks(HealthPath, CreateOptions(SelfTag)).AllowAnonymous();

        return app;
    }

    private static HealthCheckOptions CreateOptions(string tag) => new()
    {
        Predicate = registration => registration.Tags.Contains(tag)
    };
}
