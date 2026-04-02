using BL.DomainModel;
using BL.Interface;
using BL.Service;

namespace Tests.BL;

[TestFixture]
public sealed class FileServiceTests
{
    private Mock<ITourService> _tourService = null!;
    private Mock<IPdfReportService> _pdfReportService = null!;
    private FileService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _tourService = new Mock<ITourService>();
        _pdfReportService = new Mock<IPdfReportService>();
        _sut = new FileService(_tourService.Object, _pdfReportService.Object);
    }

    [Test]
    public void GenerateTourReport_WhenTourExists_UsesPdfService()
    {
        var tour = TourTestData.SampleTourDomain();
        var reportBytes = new byte[] { 1, 2, 3 };

        _tourService.Setup(service => service.GetTourById(TestConstants.TestGuid)).Returns(tour);
        _pdfReportService.Setup(service => service.GenerateTourReport(tour)).Returns(reportBytes);

        var report = _sut.GenerateTourReport(TestConstants.TestGuid);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(report, Is.EqualTo(reportBytes));
            _pdfReportService.Verify(service => service.GenerateTourReport(tour), Times.Once);
        }
    }

    [Test]
    public void GenerateTourReport_WhenTourDoesNotExist_ReturnsNullWithoutCallingPdfService()
    {
        _tourService.Setup(static service => service.GetTourById(TestConstants.NonexistentGuid)).Returns((TourDomain?)null);

        var report = _sut.GenerateTourReport(TestConstants.NonexistentGuid);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(report, Is.Null);
            _pdfReportService.Verify(static service => service.GenerateTourReport(It.IsAny<TourDomain>()), Times.Never);
        }
    }

    [Test]
    public void GenerateSummaryReport_DelegatesToPdfService()
    {
        var tours = TourTestData.SampleTourDomainList();
        var reportBytes = new byte[] { 4, 5, 6 };

        _pdfReportService.Setup(service => service.GenerateSummaryReport(tours)).Returns(reportBytes);

        var report = _sut.GenerateSummaryReport(tours);

        Assert.That(report, Is.EqualTo(reportBytes));
    }

    [Test]
    public void ExportTourToJson_ReturnsTourFromTourService()
    {
        var tour = TourTestData.SampleTourDomain();
        _tourService.Setup(static service => service.GetTourById(TestConstants.TestGuid)).Returns(tour);

        var exported = _sut.ExportTourToJson(TestConstants.TestGuid);

        Assert.That(exported, Is.SameAs(tour));
    }

    [Test]
    public async Task ImportTourFromJsonAsync_ValidPayload_CreatesTourAndReturnsTrue()
    {
        var tour = TourTestData.SampleTourDomain("Imported Tour");
        var json = JsonSerializer.Serialize(tour);
        var cancellationToken = new CancellationTokenSource().Token;

        _tourService.Setup(service => service.CreateTourAsync(
                It.Is<TourDomain>(candidate => candidate.Name == "Imported Tour" && candidate.From == tour.From),
                cancellationToken))
            .ReturnsAsync(tour);

        var result = await _sut.ImportTourFromJsonAsync(json, cancellationToken);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.True);
            _tourService.Verify(service => service.CreateTourAsync(
                It.Is<TourDomain>(candidate => candidate.Name == "Imported Tour" && candidate.From == tour.From),
                cancellationToken), Times.Once);
        }
    }

    [Test]
    public async Task ImportTourFromJsonAsync_InvalidJson_ReturnsFalseWithoutCallingTourService()
    {
        var result = await _sut.ImportTourFromJsonAsync("{invalid json}");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.False);
            _tourService.Verify(static service => service.CreateTourAsync(It.IsAny<TourDomain>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }

    [Test]
    public async Task ImportTourFromJsonAsync_NullPayload_ReturnsFalseWithoutCallingTourService()
    {
        var result = await _sut.ImportTourFromJsonAsync("null");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.False);
            _tourService.Verify(static service => service.CreateTourAsync(It.IsAny<TourDomain>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }

    [Test]
    public void ImportTourFromJsonAsync_TourServiceFailure_PropagatesException()
    {
        var json = JsonSerializer.Serialize(TourTestData.SampleTourDomain());
        _tourService.Setup(service => service.CreateTourAsync(It.IsAny<TourDomain>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("create failed"));

        Assert.That(() => _sut.ImportTourFromJsonAsync(json),
            Throws.TypeOf<InvalidOperationException>().With.Message.EqualTo("create failed"));
    }
}
