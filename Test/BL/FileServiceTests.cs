using System.Text.Json;
using BL.DomainModel;
using BL.Interface;
using BL.Service;
using Moq;

namespace Test.BL;

[TestFixture]
public class FileServiceTests
{
    [SetUp]
    public void Setup()
    {
        _mockTourService = new Mock<ITourService>();
        _mockPdfReportService = new Mock<IPdfReportService>();
        _fileService = new FileService(_mockTourService.Object, _mockPdfReportService.Object);
    }

    private Mock<ITourService> _mockTourService;
    private Mock<IPdfReportService> _mockPdfReportService;
    private FileService _fileService;

    [Test]
    public void GenerateTourReport_ValidTourId_ReturnsPdfBytes()
    {
        var tourId = TestData.TestGuid;
        var tour = TestData.SampleTourDomain();
        byte[] expectedPdfBytes =
        [
            1, 2, 3, 4, 5
        ];

        _mockTourService.Setup(s => s.GetTourById(tourId)).Returns(tour);
        _mockPdfReportService.Setup(s => s.GenerateTourReport(tour)).Returns(expectedPdfBytes);


        var result = _fileService.GenerateTourReport(tourId);


        Assert.That(result, Is.EqualTo(expectedPdfBytes));
        _mockTourService.Verify(s => s.GetTourById(tourId), Times.Once);
        _mockPdfReportService.Verify(s => s.GenerateTourReport(tour), Times.Once);
    }

    [Test]
    public void GenerateSummaryReport_ValidTours_ReturnsPdfBytes()
    {
        var tours = TestData.SampleTourDomainList();
        byte[] expectedPdfBytes =
        [
            1, 2, 3, 4, 5
        ];

        _mockPdfReportService.Setup(s => s.GenerateSummaryReport(tours)).Returns(expectedPdfBytes);


        var result = _fileService.GenerateSummaryReport(tours);


        Assert.That(result, Is.EqualTo(expectedPdfBytes));
        _mockPdfReportService.Verify(s => s.GenerateSummaryReport(tours), Times.Once);
    }

    [Test]
    public void ExportTourToJson_ValidTourId_ReturnsTourDomain()
    {
        var tourId = TestData.TestGuid;
        var expectedTour = TestData.SampleTourDomain();

        _mockTourService.Setup(s => s.GetTourById(tourId)).Returns(expectedTour);


        var result = _fileService.ExportTourToJson(tourId);


        Assert.That(result, Is.EqualTo(expectedTour));
        _mockTourService.Verify(s => s.GetTourById(tourId), Times.Once);
    }

    [Test]
    public void GenerateSummaryReport_LargeTourList_HandlesLargeDataSet()
    {
        var largeTourList = Enumerable
            .Range(0, 1000)
            .Select(_ => TestData.SampleTourDomain())
            .ToList();
        var expectedPdfBytes = new byte[1024 * 1024];

        _mockPdfReportService
            .Setup(s => s.GenerateSummaryReport(largeTourList))
            .Returns(expectedPdfBytes);


        var result = _fileService.GenerateSummaryReport(largeTourList);


        Assert.That(result, Is.EqualTo(expectedPdfBytes));
        _mockPdfReportService.Verify(s => s.GenerateSummaryReport(largeTourList), Times.Once);
    }

    [Test]
    public void ExportTourToJsonAsync_TourWithLargeLogs_HandlesLargeDataSet()
    {
        var tourId = TestData.TestGuid;
        var tourWithLargeLogs = TestData.SampleTourDomain();
        tourWithLargeLogs.Logs = Enumerable
            .Range(0, 10000)
            .Select(_ => TestData.SampleTourLogDomain())
            .ToList();

        _mockTourService.Setup(s => s.GetTourById(tourId)).Returns(tourWithLargeLogs);


        var result = _fileService.ExportTourToJson(tourId);


        Assert.That(result, Is.EqualTo(tourWithLargeLogs));
        Assert.That(result.Logs, Has.Count.EqualTo(10000));
        _mockTourService.Verify(s => s.GetTourById(tourId), Times.Once);
    }

    [Test]
    public void ExportTourToJsonAsync_InvalidTourId_ReturnsNull()
    {
        var invalidTourId = TestData.NonexistentGuid;
        _mockTourService.Setup(s => s.GetTourById(invalidTourId)).Returns((TourDomain)null!);


        var result = _fileService.ExportTourToJson(invalidTourId);


        Assert.That(result, Is.Null);
        _mockTourService.Verify(s => s.GetTourById(invalidTourId), Times.Once);
    }

    [Test]
    public void GenerateTourReportAsync_InvalidTourId_ReturnsEmptyByteArray()
    {
        var invalidTourId = TestData.NonexistentGuid;
        _mockTourService.Setup(s => s.GetTourById(invalidTourId)).Returns((TourDomain)null!);
        _mockPdfReportService.Setup(s => s.GenerateTourReport(null!)).Returns([]);


        var result = _fileService.GenerateTourReport(invalidTourId);


        Assert.That(result, Is.Empty);
        _mockTourService.Verify(s => s.GetTourById(invalidTourId), Times.Once);
        _mockPdfReportService.Verify(s => s.GenerateTourReport(null!), Times.Once);
    }

    [Test]
    public async Task ImportTourFromJsonAsync_ValidJson_CreatesTour()
    {
        var expectedTour = TestData.SampleTourDomain();
        var json = TestData.SampleTourDomainJson();

        _mockTourService
            .Setup(s => s.CreateTourAsync(It.IsAny<TourDomain>()))
            .ReturnsAsync(expectedTour);


        await _fileService.ImportTourFromJsonAsync(json);


        _mockTourService.Verify(
            s => s.CreateTourAsync(It.Is<TourDomain>(t => t.Id == expectedTour.Id)),
            Times.Once
        );
    }

    [Test]
    public Task ImportTourFromJsonAsync_InvalidJson_DoesNotCreateTour()
    {
        const string invalidJson = "{invalid json}";
        _mockTourService
            .Setup(s => s.CreateTourAsync(It.IsAny<TourDomain>()))
            .ReturnsAsync((TourDomain)null!);


        Assert.ThrowsAsync<JsonException>(() => _fileService.ImportTourFromJsonAsync(invalidJson));
        _mockTourService.Verify(s => s.CreateTourAsync(It.IsAny<TourDomain>()), Times.Never);
        return Task.CompletedTask;
    }
}