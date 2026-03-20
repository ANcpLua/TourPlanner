using BL.Service;
using DAL.Interface;

namespace Tests.BL;

[TestFixture]
public class RouteServiceTests
{
    [SetUp]
    public void Setup()
    {
        _mockRepository = new Mock<IRouteRepository>();
        _sut = new RouteService(_mockRepository.Object);
    }

    private Mock<IRouteRepository> _mockRepository = null!;
    private RouteService _sut = null!;

    [Test]
    public async Task ResolveRouteAsync_DelegatesToRepository()
    {
        _mockRepository
            .Setup(static r => r.ResolveRouteAsync(
                It.IsAny<(double, double)>(),
                It.IsAny<(double, double)>(),
                "Car",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((523400.0, 18000.0));

        var (distance, duration) = await _sut.ResolveRouteAsync(
            (48.2082, 16.3738), (52.52, 13.405), "Car");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(distance, Is.EqualTo(523400.0));
            Assert.That(duration, Is.EqualTo(18000.0));
        }
    }

}
