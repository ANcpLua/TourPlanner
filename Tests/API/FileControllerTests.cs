using API.Endpoints;
using BL.Interface;
using Contracts.Tours;
using MapsterMapper;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Tests.API;

[TestFixture]
public class ReportEndpointsTests
{
    [SetUp]
    public void Setup()
    {
        _mockFileService = new Mock<IFileService>();
        _mockTourService = new Mock<ITourService>();
        _mockMapper = new Mock<IMapper>();
    }

    private Mock<IFileService> _mockFileService = null!;
    private Mock<ITourService> _mockTourService = null!;
    private Mock<IMapper> _mockMapper = null!;

    [Test]
    public void MapReportEndpoints_RegistersEndpoints()
    {
        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();
        var result = app.MapReportEndpoints();

        Assert.That(result, Is.Not.Null);
        var dataSource = app as IEndpointRouteBuilder;
        Assert.That(dataSource.DataSources, Is.Not.Empty);
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

        var result = ReportEndpoints.GetSummaryReport(_mockFileService.Object, _mockTourService.Object);

        Assert.That(result, Is.TypeOf<FileContentHttpResult>());
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.FileContents.ToArray(), Is.EqualTo(pdfBytes));
            Assert.That(result.ContentType, Is.EqualTo("application/pdf"));
            Assert.That(result.FileDownloadName, Is.EqualTo("SummaryReport.pdf"));
        }
    }

    [Test]
    public void GetSummaryReport_UnhappyPath_ThrowsException()
    {
        _mockTourService
            .Setup(s => s.GetAllTours())
            .Throws(new Exception("Database connection error"));

        Assert.Throws<Exception>(() =>
            ReportEndpoints.GetSummaryReport(_mockFileService.Object, _mockTourService.Object));
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

        var result = ReportEndpoints.GetTourReport(tourId, _mockFileService.Object);

        Assert.That(result, Is.TypeOf<FileContentHttpResult>());
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.FileContents.ToArray(), Is.EqualTo(pdfBytes));
            Assert.That(result.ContentType, Is.EqualTo("application/pdf"));
            Assert.That(result.FileDownloadName, Is.EqualTo($"TourReport_{tourId}.pdf"));
        }
    }

    [Test]
    public void GetTourReport_UnhappyPath_ReportGenerationFails()
    {
        var tourId = TestData.NonexistentGuid;
        _mockFileService
            .Setup(s => s.GenerateTourReport(tourId))
            .Throws(new InvalidOperationException("Report generation failed"));

        Assert.Throws<InvalidOperationException>(() =>
            ReportEndpoints.GetTourReport(tourId, _mockFileService.Object));
    }

    [Test]
    public void ExportTourToJson_HappyPath_ReturnsJsonResult()
    {
        var tourId = Guid.NewGuid();
        var tourDomain = TestData.SampleTourDomain();
        var tourDto = TestData.SampleTourDto();
        _mockFileService.Setup(s => s.ExportTourToJson(tourId)).Returns(tourDomain);
        _mockMapper.Setup(m => m.Map<TourDto>(tourDomain)).Returns(tourDto);

        var result = ReportEndpoints.ExportTourToJson(tourId, _mockFileService.Object, _mockMapper.Object);

        Assert.That(result, Is.TypeOf<JsonHttpResult<TourDto>>());
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Value, Is.EqualTo(tourDto));
            Assert.That(result.StatusCode, Is.Null.Or.EqualTo(200));
        }
    }

    [Test]
    public void ExportTourToJson_UnhappyPath_TourNotFound()
    {
        var tourId = TestData.NonexistentGuid;
        _mockFileService
            .Setup(s => s.ExportTourToJson(tourId))
            .Throws(new KeyNotFoundException("Tour not found"));

        Assert.Throws<KeyNotFoundException>(() =>
            ReportEndpoints.ExportTourToJson(tourId, _mockFileService.Object, _mockMapper.Object));
    }

    [Test]
    public async Task ImportTourFromJsonAsync_HappyPath_ReturnsOkResult()
    {
        var json = TestData.SampleTourJson();

        var result = await ReportEndpoints.ImportTourFromJsonAsync(json, _mockFileService.Object, CancellationToken.None);

        Assert.That(result, Is.TypeOf<Ok<string>>());
        Assert.That(result.Value, Is.EqualTo("Tour imported successfully"));
    }

    [Test]
    public Task ImportTourFromJsonAsync_UnhappyPath_InvalidJsonFormat()
    {
        const string invalidJson = "{invalid_json}";
        _mockFileService
            .Setup(s => s.ImportTourFromJsonAsync(invalidJson))
            .ThrowsAsync(new JsonException("Invalid JSON format"));

        Assert.ThrowsAsync<JsonException>(() =>
            ReportEndpoints.ImportTourFromJsonAsync(invalidJson, _mockFileService.Object, CancellationToken.None));
        return Task.CompletedTask;
    }

    [Test]
    public Task ImportTourFromJsonAsync_UnhappyPath_DuplicateTourData()
    {
        var json = TestData.SampleTourJson();
        _mockFileService
            .Setup(s => s.ImportTourFromJsonAsync(json))
            .ThrowsAsync(new InvalidOperationException("Tour with the same ID already exists"));

        Assert.ThrowsAsync<InvalidOperationException>(() =>
            ReportEndpoints.ImportTourFromJsonAsync(json, _mockFileService.Object, CancellationToken.None));
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

        Assert.Throws<InvalidOperationException>(() =>
            ReportEndpoints.GetSummaryReport(_mockFileService.Object, _mockTourService.Object));
    }
}
