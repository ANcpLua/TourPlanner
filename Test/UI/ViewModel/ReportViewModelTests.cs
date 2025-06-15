using UI.Model;
using UI.Service.Interface;
using UI.ViewModel;

namespace Test.UI.ViewModel;

[TestFixture]
public sealed class ReportViewModelTests
{
    private Mock<IHttpService> _mockHttpService = null!;
    private Mock<IToastServiceWrapper> _mockToastService = null!;
    private Mock<IBlazorDownloadFileService> _mockDownloadFileService = null!;
    private Mock<TourViewModel> _mockTourViewModel = null!;
    private Mock<ILogger> _mockLogger = null!;
    private ReportViewModel _reportViewModel = null!;

    [SetUp]
    public void Setup()
    {
        _mockHttpService = TestData.MockHttpService();
        _mockToastService = TestData.MockToastService();
        _mockDownloadFileService = TestData.MockBlazorDownloadFileService();
        _mockLogger = TestData.MockLogger();

        _mockTourViewModel = new Mock<TourViewModel>(
                TestData.MockHttpService().Object,
                TestData.MockToastService().Object,
                TestData.MockConfiguration().Object,
                TestData.MockJsRuntime().Object,
                TestData.MockRouteApiService().Object,
                TestData.MockLogger().Object,
                TestData.MockMapViewModel().Object)
            { CallBase = true };

        _reportViewModel = new ReportViewModel(
            _mockHttpService.Object,
            _mockToastService.Object,
            _mockLogger.Object,
            _mockDownloadFileService.Object,
            _mockTourViewModel.Object);
    }

    [Test]
    public void CurrentReportUrl_Change_RaisesPropertyChanged()
    {
        var raised = new List<string?>();
        _reportViewModel.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        _reportViewModel.CurrentReportUrl = "url";
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_reportViewModel.CurrentReportUrl, Is.EqualTo("url"));
            Assert.That(raised, Contains.Item(nameof(ReportViewModel.CurrentReportUrl)));
        }
    }

    [Test]
    public void SelectedDetailedTourId_Change_RaisesPropertyChanged()
    {
        var raised = new List<string?>();
        _reportViewModel.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        var id = Guid.NewGuid();
        _reportViewModel.SelectedDetailedTourId = id;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_reportViewModel.SelectedDetailedTourId, Is.EqualTo(id));
            Assert.That(raised, Contains.Item(nameof(ReportViewModel.SelectedDetailedTourId)));
        }
    }

    [Test]
    public async Task InitializeAsync_JustRuns()
    {
        await _reportViewModel.InitializeAsync();
    }

    [Test]
    public void ResetCurrentReportUrl_ClearsFieldAndNotifies()
    {
        _reportViewModel.CurrentReportUrl = "dirty";
        var raised = new List<string?>();
        _reportViewModel.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        _reportViewModel.ResetCurrentReportUrl();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_reportViewModel.CurrentReportUrl, Is.Empty);
            Assert.That(raised, Contains.Item(nameof(ReportViewModel.CurrentReportUrl)));
        }
    }

    [Test]
    public void ClearCurrentReport_SameAsReset()
    {
        _reportViewModel.CurrentReportUrl = "dirty";
        var raised = new List<string?>();
        _reportViewModel.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        _reportViewModel.ClearCurrentReport();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_reportViewModel.CurrentReportUrl, Is.Empty);
            Assert.That(raised, Contains.Item(nameof(ReportViewModel.CurrentReportUrl)));
        }
    }

    [Test]
    public async Task GenerateAndDownloadReport_NonNullBytes_DownloadsSuccessfully()
    {
        var bytes = new byte[] { 1, 2, 3 };
        _mockHttpService.Setup(h => h.GetByteArrayAsync("uri")).ReturnsAsync(bytes);

        await _reportViewModel.GenerateAndDownloadReport("uri", "Rpt");

        _mockDownloadFileService.Verify(f => f.DownloadFileAsync(
            It.IsRegex(@"Rpt_\d{8}_\d{6}\.pdf"),
            bytes,
            "application/pdf"), Times.Once);
        _mockToastService.Verify(t => t.ShowSuccess("Rpt generated successfully."), Times.Once);
    }

    [Test]
    public async Task GenerateAndDownloadReport_NullBytes_ShowsError()
    {
        _mockHttpService.Setup(h => h.GetByteArrayAsync("uri")).ReturnsAsync((byte[])null!);

        await _reportViewModel.GenerateAndDownloadReport("uri", "Rpt");

        _mockDownloadFileService.VerifyNoOtherCalls();
        _mockToastService.Verify(t => t.ShowError("Error generating Rpt: No data received."), Times.Once);
    }

    [Test]
    public Task GenerateAndDownloadReport_HttpServiceThrows_PropagatesException()
    {
        var expectedException = new HttpRequestException("Network error");
        _mockHttpService.Setup(h => h.GetByteArrayAsync("failing-api")).ThrowsAsync(expectedException);

        var actualException =
            Assert.ThrowsAsync<HttpRequestException>(() =>
                _reportViewModel.GenerateAndDownloadReport("failing-api", "FailingReport"));

        Assert.That(actualException, Is.SameAs(expectedException));
        _mockDownloadFileService.VerifyNoOtherCalls();
        _mockToastService.VerifyNoOtherCalls();
        return Task.CompletedTask;
    }

    [Test]
    public Task GenerateAndDownloadReport_DownloadFileThrows_PropagatesException()
    {
        var bytes = new byte[] { 1, 2, 3 };
        _mockHttpService.Setup(h => h.GetByteArrayAsync("api/test")).ReturnsAsync(bytes);

        var expectedException = new InvalidOperationException("Download failed");
        _mockDownloadFileService
            .Setup(f => f.DownloadFileAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<string>()))
            .ThrowsAsync(expectedException);

        var actualException =
            Assert.ThrowsAsync<InvalidOperationException>(() =>
                _reportViewModel.GenerateAndDownloadReport("api/test", "TestReport"));

        Assert.That(actualException, Is.SameAs(expectedException));
        return Task.CompletedTask;
    }

    [Test]
    [TestCase("api/reports/summary", "SummaryReport")]
    [TestCase("api/reports/detailed", "DetailedReport")]
    [TestCase("api/reports/custom", "CustomReport")]
    [TestCase("", "EmptyUriReport")]
    public async Task GenerateAndDownloadReport_VariousUrisAndReportTypes_Success(string uri, string reportType)
    {
        var bytes = new byte[] { 1, 2, 3, 4, 5 };
        _mockHttpService.Setup(h => h.GetByteArrayAsync(uri)).ReturnsAsync(bytes);

        await _reportViewModel.GenerateAndDownloadReport(uri, reportType);

        _mockDownloadFileService.Verify(f => f.DownloadFileAsync(
            It.IsRegex($@"{reportType}_\d{{8}}_\d{{6}}\.pdf"),
            bytes,
            "application/pdf"), Times.Once);

        _mockToastService.Verify(t => t.ShowSuccess($"{reportType} generated successfully."), Times.Once);
    }

    [Test]
    public async Task GenerateSummaryReportAsync_CompleteFlow()
    {
        var bytes = new byte[] { 4, 5, 6 };
        _mockHttpService.Setup(h => h.GetByteArrayAsync("api/reports/summary")).ReturnsAsync(bytes);

        await _reportViewModel.GenerateSummaryReportAsync();

        _mockDownloadFileService.Verify(f => f.DownloadFileAsync(
            It.IsRegex(@"SummaryReport_\d{8}_\d{6}\.pdf"), bytes, "application/pdf"), Times.Once);
        _mockToastService.Verify(t => t.ShowSuccess("SummaryReport generated successfully."), Times.Once);
    }

    [Test]
    public async Task GenerateDetailedReportAsync_NoId_NothingHappens()
    {
        _reportViewModel.SelectedDetailedTourId = Guid.Empty;
        await _reportViewModel.GenerateDetailedReportAsync();

        _mockDownloadFileService.VerifyNoOtherCalls();
        _mockToastService.VerifyNoOtherCalls();
    }

    [Test]
    public async Task GenerateDetailedReportAsync_WithImagePath_UpdatesPathAndDownloads()
    {
        var tour = TestData.SampleTour();
        tour.ImagePath = Path.Combine("sub", "img.png");
        _mockTourViewModel.Object.Tours.Add(tour);
        _reportViewModel.SelectedDetailedTourId = tour.Id;

        var bytes = new byte[] { 7, 7, 7 };
        _mockHttpService.Setup(h => h.GetByteArrayAsync($"api/reports/tour/{tour.Id}")).ReturnsAsync(bytes);

        await _reportViewModel.GenerateDetailedReportAsync();

        var expectedPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "sub", "img.png");
        var actualPath = Path.GetFullPath(tour.ImagePath);
        var normalizedExpected = Path.GetFullPath(expectedPath);

        Assert.That(actualPath, Is.EqualTo(normalizedExpected));

        _mockDownloadFileService.Verify(f => f.DownloadFileAsync(
            It.IsRegex(@"DetailedReport_\d{8}_\d{6}\.pdf"), bytes, "application/pdf"), Times.Once);
        _mockToastService.Verify(t => t.ShowSuccess("DetailedReport generated successfully."), Times.Once);
    }

    [Test]
    public async Task GenerateDetailedReportAsync_EmptyImagePath_DownloadsButKeepsEmpty()
    {
        var tour = TestData.SampleTour();
        tour.ImagePath = "";
        _mockTourViewModel.Object.Tours.Add(tour);
        _reportViewModel.SelectedDetailedTourId = tour.Id;

        var bytes = new byte[] { 8, 8, 8 };
        _mockHttpService.Setup(h => h.GetByteArrayAsync($"api/reports/tour/{tour.Id}")).ReturnsAsync(bytes);

        await _reportViewModel.GenerateDetailedReportAsync();

        Assert.That(tour.ImagePath, Is.Empty);
        _mockDownloadFileService.Verify(f => f.DownloadFileAsync(It.IsAny<string>(), bytes, "application/pdf"),
            Times.Once);
        _mockToastService.Verify(t => t.ShowSuccess("DetailedReport generated successfully."), Times.Once);
    }

    [Test]
    public async Task GenerateDetailedReportAsync_TourNotFound_SkipsImageLogicAndStillDownloads()
    {
        var missingId = Guid.NewGuid();
        _reportViewModel.SelectedDetailedTourId = missingId;

        var fake = new byte[] { 9, 8, 7 };
        _mockHttpService
            .Setup(h => h.GetByteArrayAsync($"api/reports/tour/{missingId}"))
            .ReturnsAsync(fake);

        await _reportViewModel.GenerateDetailedReportAsync();

        _mockDownloadFileService.Verify(f => f.DownloadFileAsync(
            It.IsRegex(@"DetailedReport_\d{8}_\d{6}\.pdf"),
            fake,
            "application/pdf"
        ), Times.Once);

        _mockToastService.Verify(t => t.ShowSuccess("DetailedReport generated successfully."), Times.Once);
    }

    [Test]
    public async Task GenerateDetailedReportAsync_NullImagePath_DoesNotModifyPath()
    {
        var tour = TestData.SampleTour();
        tour.ImagePath = null!;
        _mockTourViewModel.Object.Tours.Add(tour);
        _reportViewModel.SelectedDetailedTourId = tour.Id;

        var bytes = new byte[] { 1, 2, 3 };
        _mockHttpService.Setup(h => h.GetByteArrayAsync($"api/reports/tour/{tour.Id}")).ReturnsAsync(bytes);

        await _reportViewModel.GenerateDetailedReportAsync();

        Assert.That(tour.ImagePath, Is.Null);
        _mockDownloadFileService.Verify(f => f.DownloadFileAsync(It.IsAny<string>(), bytes, "application/pdf"),
            Times.Once);
    }

    [Test]
    public async Task ExportTourToJsonAsync_Valid_Exports()
    {
        var id = Guid.NewGuid();
        var json = TestData.SampleTourJson();
        _mockHttpService.Setup(h => h.GetStringAsync($"api/reports/export/{id}")).ReturnsAsync(json);

        await _reportViewModel.ExportTourToJsonAsync(id);

        _mockDownloadFileService.Verify(f => f.DownloadFileAsync(
            It.IsRegex($@"Tour_{id}_\d{{8}}_\d{{6}}\.json"),
            It.Is<byte[]>(b => Encoding.UTF8.GetString(b) == json),
            "application/json"), Times.Once);
        _mockToastService.Verify(t => t.ShowSuccess("Tour exported successfully."), Times.Once);
    }

    [Test]
    public async Task ExportTourToJsonAsync_Invalid_ShowsError()
    {
        var id = Guid.NewGuid();
        _mockHttpService.Setup(h => h.GetStringAsync($"api/reports/export/{id}")).ReturnsAsync("");

        await _reportViewModel.ExportTourToJsonAsync(id);

        _mockToastService.Verify(t => t.ShowError("Error exporting tour: Invalid tour data."), Times.Once);
        _mockDownloadFileService.Verify(
            f => f.DownloadFileAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task ImportTourFromJsonAsync_InvalidJson_ShowsError()
    {
        await _reportViewModel.ImportTourFromJsonAsync(TestData.MakeFile("{bad json"));

        _mockToastService.Verify(t => t.ShowError(It.Is<string>(s => s.StartsWith("Error importing tour"))),
            Times.Once);
        _mockHttpService.Verify(h => h.PostAsync(It.IsAny<string>(), It.IsAny<Tour>()), Times.Never);
    }

    [Test]
    public async Task ImportTourFromJsonAsync_Duplicate_ShowsError()
    {
        var duplicate = TestData.SampleTour();
        _mockTourViewModel.Object.Tours.Add(duplicate);

        await _reportViewModel.ImportTourFromJsonAsync(TestData.MakeFile(JsonSerializer.Serialize(duplicate)));

        _mockToastService.Verify(t => t.ShowError(It.Is<string>(s => s.Contains("already exists"))), Times.Once);
        _mockHttpService.Verify(h => h.PostAsync(It.IsAny<string>(), It.IsAny<Tour>()), Times.Never);
    }

    [Test]
    public async Task ImportTourFromJsonAsync_NewTour_Imports()
    {
        var newTour = TestData.SampleTour();
        var json = JsonSerializer.Serialize(newTour);

        _mockHttpService
            .Setup(h => h.PostAsync("api/tour", It.IsAny<Tour>()))
            .Returns(Task.FromResult(newTour));

        await _reportViewModel.ImportTourFromJsonAsync(TestData.MakeFile(json));

        _mockHttpService.Verify(h => h.PostAsync("api/tour", It.IsAny<Tour>()), Times.Once);
        _mockToastService.Verify(t => t.ShowSuccess("Tour imported successfully."), Times.Once);
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task ImportTourFromJsonAsync_DifferentJsonSerializationOptions_HandlesCorrectly(bool camelCase)
    {
        var tour = TestData.SampleTour();
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = camelCase ? JsonNamingPolicy.CamelCase : null
        };
        var json = JsonSerializer.Serialize(tour, options);
        var args = TestData.MakeFile(json);

        _mockTourViewModel.Object.Tours.Clear();

        _mockHttpService
            .Setup(h => h.PostAsync("api/tour", It.IsAny<Tour>()))
            .Returns(Task.FromResult(tour));

        await _reportViewModel.ImportTourFromJsonAsync(args);

        _mockToastService.Verify(t => t.ShowSuccess("Tour imported successfully."), Times.Once);
    }

    [Test]
    public async Task ImportTourFromJsonAsync_NullTourAfterDeserialization_ShowsError()
    {
        await _reportViewModel.ImportTourFromJsonAsync(TestData.MakeFile("null"));

        _mockToastService.Verify(t => t.ShowError("Error importing tour: Invalid tour data."), Times.Once);
        _mockHttpService.Verify(h => h.PostAsync(It.IsAny<string>(), It.IsAny<Tour>()), Times.Never);
    }
}