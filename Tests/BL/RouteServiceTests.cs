using BL.Service;
using DAL.Interface;

namespace Tests.BL;

[TestFixture]
public sealed class RouteServiceTests
{
    private Mock<IRouteRepository> _routeRepository = null!;
    private RouteService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _routeRepository = new Mock<IRouteRepository>();
        _sut = new RouteService(_routeRepository.Object);
    }

    [Test]
    public async Task ResolveRouteAsync_ForwardsArgumentsAndCancellationToken()
    {
        var cancellationToken = new CancellationTokenSource().Token;
        _routeRepository.Setup(repository => repository.ResolveRouteAsync(
                It.Is<(double Latitude, double Longitude)>(from => from.Latitude == 48.2082 && from.Longitude == 16.3738),
                It.Is<(double Latitude, double Longitude)>(to => to.Latitude == 52.52 && to.Longitude == 13.405),
                "Car",
                cancellationToken))
            .ReturnsAsync((523400.0, 18000.0));

        var result = await _sut.ResolveRouteAsync((48.2082, 16.3738), (52.52, 13.405), "Car", cancellationToken);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Distance, Is.EqualTo(523400.0));
            Assert.That(result.Duration, Is.EqualTo(18000.0));
        }

        _routeRepository.Verify(repository => repository.ResolveRouteAsync(
            It.Is<(double Latitude, double Longitude)>(from => from.Latitude == 48.2082 && from.Longitude == 16.3738),
            It.Is<(double Latitude, double Longitude)>(to => to.Latitude == 52.52 && to.Longitude == 13.405),
            "Car",
            cancellationToken), Times.Once);
    }

    [Test]
    public void ResolveRouteAsync_RepositoryFailure_PropagatesException()
    {
        _routeRepository.Setup(repository => repository.ResolveRouteAsync(
                It.IsAny<(double Latitude, double Longitude)>(),
                It.IsAny<(double Latitude, double Longitude)>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("route failed"));

        Assert.That(() => _sut.ResolveRouteAsync((48.2082, 16.3738), (52.52, 13.405), "Car"),
            Throws.TypeOf<HttpRequestException>().With.Message.EqualTo("route failed"));
    }
}
