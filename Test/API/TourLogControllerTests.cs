using API.Controllers;
using BL.DomainModel;
using BL.Interface;
using MapsterMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UI.Model;

namespace Test.API;

[TestFixture]
public class TourLogControllerTests
{
    private Mock<ITourLogService> _mockTourLogService = null!;
    private Mock<IMapper> _mockMapper = null!;
    private TourLogController _controller = null!;

    [SetUp]
    public void Setup()
    {
        _mockTourLogService = new Mock<ITourLogService>();
        _mockMapper = new Mock<IMapper>();
        _controller = new TourLogController(_mockTourLogService.Object, _mockMapper.Object);
    }

    [Test]
    public async Task CreateTourLogAsync_HappyPath_ReturnsCreatedTourLog()
    {
        var tourLogDto = TestData.SampleTourLogDto();
        var tourLogDomain = TestData.SampleTourLogDomain();
        _mockMapper.Setup(m => m.Map<TourLogDomain>(tourLogDto)).Returns(tourLogDomain);
        _mockTourLogService
            .Setup(s => s.CreateTourLogAsync(tourLogDomain, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tourLogDomain);
        _mockMapper.Setup(m => m.Map<TourLog>(tourLogDomain)).Returns(tourLogDto);

        var result = await _controller.CreateTourLog(tourLogDto);

        Assert.That(result, Is.TypeOf<OkObjectResult>());
        var okResult = (OkObjectResult)result;
        Assert.That(okResult.Value, Is.EqualTo(tourLogDto));
    }

    [Test]
    public Task CreateTourLogAsync_UnhappyPath_ValidationFails()
    {
        var tourLogDto = TestData.SampleTourLogDto();
        var tourLogDomain = TestData.SampleTourLogDomain();
        _mockMapper.Setup(m => m.Map<TourLogDomain>(tourLogDto)).Returns(tourLogDomain);
        _mockTourLogService
            .Setup(s => s.CreateTourLogAsync(tourLogDomain, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Invalid tour log data"));

        Assert.ThrowsAsync<ArgumentException>(() => _controller.CreateTourLog(tourLogDto));
        return Task.CompletedTask;
    }

    [Test]
    public Task CreateTourLogAsync_UnhappyPath_DuplicateTourLog()
    {
        var tourLogDto = TestData.SampleTourLogDto();
        var tourLogDomain = TestData.SampleTourLogDomain();
        _mockMapper.Setup(m => m.Map<TourLogDomain>(tourLogDto)).Returns(tourLogDomain);
        _mockTourLogService
            .Setup(s => s.CreateTourLogAsync(tourLogDomain, It.IsAny<CancellationToken>()))
            .ThrowsAsync(
                new InvalidOperationException(
                    "TourLog with the same date already exists for this tour"
                )
            );

        Assert.ThrowsAsync<InvalidOperationException>(() => _controller.CreateTourLog(tourLogDto));
        return Task.CompletedTask;
    }

    [Test]
    public void GetTourLogById_HappyPath_ReturnsTourLog()
    {
        var tourLogId = Guid.NewGuid();
        var tourLogDomain = TestData.SampleTourLogDomain();
        var tourLogDto = TestData.SampleTourLogDto();
        _mockTourLogService.Setup(s => s.GetTourLogById(tourLogId)).Returns(tourLogDomain);
        _mockMapper.Setup(m => m.Map<TourLog>(tourLogDomain)).Returns(tourLogDto);

        var result = _controller.GetTourLogById(tourLogId);

        Assert.That(result, Is.TypeOf<OkObjectResult>());
        var okResult = (OkObjectResult)result;
        Assert.That(okResult.Value, Is.EqualTo(tourLogDto));
    }

    [Test]
    public void GetTourLogById_UnhappyPath_TourLogNotFound()
    {
        var tourLogId = TestData.NonexistentGuid;
        _mockTourLogService.Setup(s => s.GetTourLogById(tourLogId)).Returns((TourLogDomain)null!);

        var result = _controller.GetTourLogById(tourLogId);

        Assert.That(result, Is.TypeOf<NotFoundResult>());
    }

    [Test]
    public void GetTourLogsByTourId_HappyPath_ReturnsTourLogs()
    {
        var tourId = Guid.NewGuid();
        var tourLogsDomain = TestData.SampleTourLogDomainList();
        var tourLogsDto = TestData.SampleTourLogDtoList();
        _mockTourLogService
            .Setup(s => s.GetTourLogsByTourId(tourId))
            .Returns(tourLogsDomain);
        _mockMapper.Setup(m => m.Map<IEnumerable<TourLog>>(tourLogsDomain)).Returns(tourLogsDto);

        var result = _controller.GetTourLogsByTourId(tourId);

        Assert.That(result, Is.TypeOf<OkObjectResult>());
        var okResult = (OkObjectResult)result;
        Assert.That(okResult.Value, Is.EqualTo(tourLogsDto));
    }

    [Test]
    public void GetTourLogsByTourId_UnhappyPath_TourNotFound()
    {
        var tourId = TestData.NonexistentGuid;
        _mockTourLogService
            .Setup(s => s.GetTourLogsByTourId(tourId))
            .Throws(new KeyNotFoundException("Tour not found"));

        Assert.Throws<KeyNotFoundException>(() => _controller.GetTourLogsByTourId(tourId));
    }

    [Test]
    public void GetTourLogsByTourId_UnhappyPath_DatabaseError()
    {
        var tourId = Guid.NewGuid();
        _mockTourLogService
            .Setup(s => s.GetTourLogsByTourId(tourId))
            .Throws(new Exception("Database connection error"));

        Assert.Throws<Exception>(() => _controller.GetTourLogsByTourId(tourId));
    }

    [Test]
    public async Task UpdateTourLogAsync_HappyPath_ReturnsUpdatedTourLog()
    {
        var tourLogId = Guid.NewGuid();
        var tourLogDto = TestData.SampleTourLogDto();
        var tourLogDomain = TestData.SampleTourLogDomain();
        _mockMapper.Setup(m => m.Map<TourLogDomain>(tourLogDto)).Returns(tourLogDomain);
        _mockTourLogService
            .Setup(s => s.UpdateTourLogAsync(tourLogDomain, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tourLogDomain);
        _mockMapper.Setup(m => m.Map<TourLog>(tourLogDomain)).Returns(tourLogDto);

        var result = await _controller.UpdateTourLog(tourLogId, tourLogDto);

        Assert.That(result, Is.TypeOf<OkObjectResult>());
        var okResult = (OkObjectResult)result;
        Assert.That(okResult.Value, Is.EqualTo(tourLogDto));
    }

    [Test]
    public Task UpdateTourLogAsync_UnhappyPath_ConcurrencyConflict()
    {
        var tourLogId = TestData.NonexistentGuid;
        var tourLogDto = TestData.SampleTourLogDto();
        var tourLogDomain = TestData.SampleTourLogDomain();
        _mockMapper.Setup(m => m.Map<TourLogDomain>(tourLogDto)).Returns(tourLogDomain);
        _mockTourLogService
            .Setup(s => s.UpdateTourLogAsync(tourLogDomain, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException("Concurrency conflict occurred"));

        Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => _controller.UpdateTourLog(tourLogId, tourLogDto)
        );
        return Task.CompletedTask;
    }

    [Test]
    public async Task DeleteTourLogAsync_HappyPath_ReturnsNoContent()
    {
        var tourLogId = Guid.NewGuid();
        _mockTourLogService
            .Setup(s => s.DeleteTourLogAsync(tourLogId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _controller.DeleteTourLog(tourLogId);

        Assert.That(result, Is.TypeOf<NoContentResult>());
    }

    [Test]
    public Task DeleteTourLogAsync_UnhappyPath_TourLogNotFound()
    {
        var tourLogId = TestData.NonexistentGuid;
        _mockTourLogService
            .Setup(s => s.DeleteTourLogAsync(tourLogId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException("TourLog not found"));

        Assert.ThrowsAsync<KeyNotFoundException>(() => _controller.DeleteTourLog(tourLogId));
        return Task.CompletedTask;
    }
}