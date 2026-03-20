using API.Endpoints;
using BL.Interface;
using Contracts.Routes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Tests.API;

[TestFixture]
public class RouteEndpointsTests
{
    [SetUp]
    public void Setup()
    {
        _mockRouteService = new Mock<IRouteService>();
    }

    private Mock<IRouteService> _mockRouteService = null!;

    [Test]
    public async Task ResolveRoute_ValidRequest_ReturnsDistanceAndDuration()
    {
        _mockRouteService
            .Setup(static s => s.ResolveRouteAsync(
                It.IsAny<(double, double)>(),
                It.IsAny<(double, double)>(),
                "Car",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((523400.0, 18000.0));

        var request = new ResolveRouteRequest
        {
            FromLatitude = 48.2082,
            FromLongitude = 16.3738,
            ToLatitude = 52.52,
            ToLongitude = 13.405,
            TransportType = "Car"
        };

        var result = await RouteEndpoints.ResolveRoute(
            request, _mockRouteService.Object, CancellationToken.None);

        Assert.That(result, Is.TypeOf<Ok<ResolveRouteResponse>>());
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Value!.Distance, Is.EqualTo(523400.0));
            Assert.That(result.Value!.Duration, Is.EqualTo(18000.0));
        }
    }

    [Test]
    public void MapRouteEndpoints_RegistersEndpoints()
    {
        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();
        var result = app.MapRouteEndpoints();

        Assert.That(result, Is.Not.Null);
        IEndpointRouteBuilder dataSource = app;
        Assert.That(dataSource.DataSources, Is.Not.Empty);
    }

}
