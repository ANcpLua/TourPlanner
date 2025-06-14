using API.Controllers;
using BL.Interface;
using MapsterMapper;
using Microsoft.AspNetCore.Mvc;
using UI.Model;

namespace Test.API;

[TestFixture]
public class FileControllerTests
{
    private Mock<IFileService> _mockFileService = null!;
    private Mock<ITourService> _mockTourService = null!;
    private Mock<IMapper> _mockMapper = null!;
    private FileController _controller = null!;

    [SetUp]
    public void Setup()
    {
        _mockFileService = new Mock<IFileService>();
        _mockTourService = new Mock<ITourService>();
        _mockMapper = new Mock<IMapper>();
        _controller = new FileController(
            _mockFileService.Object,
            _mockTourService.Object,
            _mockMapper.Object
        );
    }

    [Test]
    public void GetSummaryReport_HappyPath_ReturnsPdfFile()
    {
        var tours = TestData.SampleTourDomainList();
        byte[] pdfBytes =
        [
            1, 2, 3
        ];
        _mockTourService.Setup(s => s.GetAllTours()).Returns(tours);
        _mockFileService.Setup(s => s.GenerateSummaryReport(tours)).Returns(pdfBytes);

        var result = _controller.GetSummaryReport();

        Assert.That(result, Is.TypeOf<FileContentResult>());
        var fileResult = (FileContentResult)result;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(fileResult.FileContents, Is.EqualTo(pdfBytes));
            Assert.That(fileResult.ContentType, Is.EqualTo("application/pdf"));
            Assert.That(fileResult.FileDownloadName, Is.EqualTo("SummaryReport.pdf"));
        }
    }

    [Test]
    public void GetSummaryReport_UnhappyPath_ThrowsException()
    {
        _mockTourService
            .Setup(s => s.GetAllTours())
            .Throws(new Exception("Database connection error"));

        Assert.Throws<Exception>(() => _controller.GetSummaryReport());
    }

    [Test]
    public void GetTourReport_HappyPath_ReturnsPdfFile()
    {
        var tourId = TestData.TestGuid;
        byte[] pdfBytes =
        [
            4, 5, 6
        ];
        _mockFileService.Setup(s => s.GenerateTourReport(tourId)).Returns(pdfBytes);

        var result = _controller.GetTourReport(tourId);

        Assert.That(result, Is.TypeOf<FileContentResult>());
        var fileResult = (FileContentResult)result;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(fileResult.FileContents, Is.EqualTo(pdfBytes));
            Assert.That(fileResult.ContentType, Is.EqualTo("application/pdf"));
            Assert.That(fileResult.FileDownloadName, Is.EqualTo($"TourReport_{tourId}.pdf"));
        }
    }

    [Test]
    public void GetTourReport_UnhappyPath_ReportGenerationFails()
    {
        var tourId = TestData.NonexistentGuid;
        _mockFileService
            .Setup(s => s.GenerateTourReport(tourId))
            .Throws(new InvalidOperationException("Report generation failed"));

        Assert.Throws<InvalidOperationException>(() => _controller.GetTourReport(tourId));
    }

    [Test]
    public void ExportTourToJson_HappyPath_ReturnsJsonResult()
    {
        var tourId = Guid.NewGuid();
        var tourDomain = TestData.SampleTourDomain();
        var tourDto = TestData.SampleTour();
        _mockFileService.Setup(s => s.ExportTourToJson(tourId)).Returns(tourDomain);
        _mockMapper.Setup(m => m.Map<Tour>(tourDomain)).Returns(tourDto);

        var result = _controller.ExportTourToJson(tourId);

        Assert.That(result, Is.TypeOf<JsonResult>());
        var jsonResult = (JsonResult)result;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(jsonResult.Value, Is.EqualTo(tourDto));
            Assert.That(jsonResult.ContentType, Is.EqualTo("application/json"));
            Assert.That(jsonResult.StatusCode, Is.EqualTo(200));
        }
    }

    [Test]
    public void ExportTourToJson_UnhappyPath_TourNotFound()
    {
        var tourId = TestData.NonexistentGuid;
        _mockFileService
            .Setup(s => s.ExportTourToJson(tourId))
            .Throws(new KeyNotFoundException("Tour not found"));

        Assert.Throws<KeyNotFoundException>(() => _controller.ExportTourToJson(tourId));
    }

    [Test]
    public async Task ImportTourFromJsonAsync_HappyPath_ReturnsOkResult()
    {
        var json = TestData.SampleTourJson();

        var result = await _controller.ImportTourFromJsonAsync(json);

        Assert.That(result, Is.TypeOf<OkObjectResult>());
        var okResult = (OkObjectResult)result;
        Assert.That(okResult.Value, Is.EqualTo("Tour imported successfully"));
    }

    [Test]
    public Task ImportTourFromJsonAsync_UnhappyPath_InvalidJsonFormat()
    {
        const string invalidJson = "{invalid_json}";
        _mockFileService
            .Setup(s => s.ImportTourFromJsonAsync(invalidJson))
            .ThrowsAsync(new JsonException("Invalid JSON format"));

        Assert.ThrowsAsync<JsonException>(() => _controller.ImportTourFromJsonAsync(invalidJson));
        return Task.CompletedTask;
    }

    [Test]
    public Task ImportTourFromJsonAsync_UnhappyPath_DuplicateTourData()
    {
        var json = TestData.SampleTourJson();
        _mockFileService
            .Setup(s => s.ImportTourFromJsonAsync(json))
            .ThrowsAsync(new InvalidOperationException("Tour with the same ID already exists"));

        Assert.ThrowsAsync<InvalidOperationException>(() => _controller.ImportTourFromJsonAsync(json));
        return Task.CompletedTask;
    }

    [Test]
    public void GetSummaryReport_UnhappyPath_PdfGenerationFails()
    {
        var tours = TestData.SampleTourDomainList();
        _mockTourService.Setup(s => s.GetAllTours()).Returns(tours);
        _mockFileService
            .Setup(s => s.GenerateSummaryReport(tours))
            .Throws(new InvalidOperationException("PDF generation failed"));

        Assert.Throws<InvalidOperationException>(() => _controller.GetSummaryReport());
    }
}