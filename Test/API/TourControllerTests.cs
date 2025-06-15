using API.Controllers;
using BL.DomainModel;
using BL.Interface;
using MapsterMapper;
using Microsoft.AspNetCore.Mvc;
using UI.Model;

namespace Test.API;

[TestFixture]
public class TourControllerTests
{
    private Mock<ITourService> _mockTourService = null!;
    private Mock<IMapper> _mockMapper = null!;
    private TourController _controller = null!;

    [SetUp]
    public void Setup()
    {
        _mockTourService = new Mock<ITourService>();
        _mockMapper = new Mock<IMapper>();
        _controller = new TourController(_mockTourService.Object, _mockMapper.Object);
    }

    [Test]
    public async Task CreateTourAsync_HappyPath_ReturnsCreatedTour()
    {
        var tourDto = TestData.SampleTour();
        var tourDomain = TestData.SampleTourDomain();
        _mockMapper.Setup(m => m.Map<TourDomain>(tourDto)).Returns(tourDomain);
        _mockTourService.Setup(s => s.CreateTourAsync(tourDomain)).ReturnsAsync(tourDomain);
        _mockMapper.Setup(m => m.Map<Tour>(tourDomain)).Returns(tourDto);

        var result = await _controller.CreateTour(tourDto);

        Assert.That(result.Result, Is.TypeOf<OkObjectResult>());
        var okResult = (OkObjectResult)result.Result;
        Assert.That(okResult.Value, Is.EqualTo(tourDto));
    }

    [Test]
    public Task CreateTourAsync_UnhappyPath_ValidationFails()
    {
        var tourDto = TestData.SampleTour();
        var tourDomain = TestData.SampleTourDomain();
        _mockMapper.Setup(m => m.Map<TourDomain>(tourDto)).Returns(tourDomain);
        _mockTourService
            .Setup(s => s.CreateTourAsync(tourDomain))
            .ThrowsAsync(new ArgumentException("Invalid tour data"));

        Assert.ThrowsAsync<ArgumentException>(() => _controller.CreateTour(tourDto));
        return Task.CompletedTask;
    }

    [Test]
    public void GetAllTours_HappyPath_ReturnsAllTours()
    {
        var toursDomain = TestData.SampleTourDomainList();
        var toursDto = TestData.SampleTourList();
        _mockTourService.Setup(s => s.GetAllTours()).Returns(toursDomain);
        _mockMapper.Setup(m => m.Map<IEnumerable<Tour>>(toursDomain)).Returns(toursDto);

        var result = _controller.GetAllTours();

        Assert.That(result.Result, Is.TypeOf<OkObjectResult>());
        var okResult = (OkObjectResult)result.Result;
        Assert.That(okResult.Value, Is.EqualTo(toursDto));
    }

    [Test]
    public void GetAllTours_UnhappyPath_DatabaseError()
    {
        _mockTourService
            .Setup(s => s.GetAllTours())
            .Throws(new Exception("Database connection error"));

        Assert.Throws<Exception>(() => _controller.GetAllTours());
    }

    [Test]
    public void GetTourById_HappyPath_ReturnsTour()
    {
        var tourId = Guid.NewGuid();
        var tourDomain = TestData.SampleTourDomain();
        var tourDto = TestData.SampleTour();
        _mockTourService.Setup(s => s.GetTourById(tourId)).Returns(tourDomain);
        _mockMapper.Setup(m => m.Map<Tour>(tourDomain)).Returns(tourDto);

        var result = _controller.GetTourById(tourId);

        Assert.That(result.Result, Is.TypeOf<OkObjectResult>());
        var okResult = (OkObjectResult)result.Result;
        Assert.That(okResult.Value, Is.EqualTo(tourDto));
    }

    [Test]
    public void GetTourById_UnhappyPath_TourNotFound()
    {
        var tourId = TestData.NonexistentGuid;
        _mockTourService
            .Setup(s => s.GetTourById(tourId))
            .Throws(new KeyNotFoundException("Tour not found"));

        Assert.Throws<KeyNotFoundException>(() => _controller.GetTourById(tourId));
    }

    [Test]
    public async Task UpdateTourAsync_HappyPath_ReturnsUpdatedTour()
    {
        var tourId = Guid.NewGuid();
        var tourDto = TestData.SampleTour();
        tourDto.Id = tourId;
        var tourDomain = TestData.SampleTourDomain();
        _mockMapper.Setup(m => m.Map<TourDomain>(tourDto)).Returns(tourDomain);
        _mockTourService.Setup(s => s.UpdateTourAsync(tourDomain)).ReturnsAsync(tourDomain);
        _mockMapper.Setup(m => m.Map<Tour>(tourDomain)).Returns(tourDto);

        var result = await _controller.UpdateTour(tourId, tourDto);

        Assert.That(result.Result, Is.TypeOf<OkObjectResult>());
        var okResult = (OkObjectResult)result.Result;
        Assert.That(okResult.Value, Is.EqualTo(tourDto));
    }

    [Test]
    public async Task UpdateTourAsync_UnhappyPath_IdMismatch()
    {
        var tourId = Guid.NewGuid();
        var tourDto = TestData.SampleTour();
        tourDto.Id = Guid.NewGuid();

        var result = await _controller.UpdateTour(tourId, tourDto);

        Assert.That(result.Result, Is.TypeOf<BadRequestObjectResult>());
        var badRequestResult = (BadRequestObjectResult)result.Result;
        Assert.That(badRequestResult.Value, Is.EqualTo("ID mismatch"));
    }

    [Test]
    public async Task DeleteTourAsync_HappyPath_ReturnsNoContent()
    {
        var tourId = Guid.NewGuid();
        _mockTourService.Setup(s => s.DeleteTourAsync(tourId)).Returns(Task.CompletedTask);

        var result = await _controller.DeleteTour(tourId);

        Assert.That(result, Is.TypeOf<NoContentResult>());
    }

    [Test]
    public Task DeleteTourAsync_UnhappyPath_TourNotFound()
    {
        var tourId = Guid.NewGuid();
        _mockTourService
            .Setup(s => s.DeleteTourAsync(tourId))
            .ThrowsAsync(new KeyNotFoundException("Tour not found"));

        Assert.ThrowsAsync<KeyNotFoundException>(() => _controller.DeleteTour(tourId));
        return Task.CompletedTask;
    }

    [Test]
    public void SearchTours_HappyPath_ReturnsMatchingTours()
    {
        const string searchText = TestData.ValidSearchText;
        var toursDomain = TestData.SampleTourDomainList().AsQueryable();
        var toursDto = TestData.SampleTourList();
        _mockTourService.Setup(s => s.SearchTours(searchText)).Returns(toursDomain);
        _mockMapper
            .Setup(m => m.Map<IEnumerable<Tour>>(It.IsAny<IEnumerable<TourDomain>>()))
            .Returns(toursDto);

        var result = _controller.SearchTours(searchText);

        Assert.That(result, Is.TypeOf<OkObjectResult>());
        var okResult = (OkObjectResult)result;
        Assert.That(okResult.Value, Is.EqualTo(toursDto));
    }

    [Test]
    public void SearchTours_UnhappyPath_NoMatchingTours()
    {
        const string searchText = TestData.InvalidSearchText;
        _mockTourService
            .Setup(s => s.SearchTours(searchText))
            .Returns(new List<TourDomain>().AsQueryable());
        _mockMapper
            .Setup(m => m.Map<IEnumerable<Tour>>(It.IsAny<IEnumerable<TourDomain>>()))
            .Returns(new List<Tour>());

        var result = _controller.SearchTours(searchText);

        Assert.That(result, Is.TypeOf<OkObjectResult>());
        var okResult = (OkObjectResult)result;
        Assert.That(((IEnumerable<Tour>)okResult.Value!).Count(), Is.Zero);
    }
}