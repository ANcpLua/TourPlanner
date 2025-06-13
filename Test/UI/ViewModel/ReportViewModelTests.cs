using System.Text;
using System.Text.Json;
using BlazorDownloadFile;
using Microsoft.AspNetCore.Components.Forms;
using Moq;
using Serilog;
using UI.Model;
using UI.Service;
using UI.Service.Interface;
using UI.ViewModel;

namespace Test.UI.ViewModel;

[TestFixture]
public sealed class ReportViewModelTests
{
    [SetUp]
    public void Setup()
    {
        _http = TestData.MockHttpService();
        _toast = TestData.MockToastService();
        _file = TestData.MockBlazorDownloadFileService();
        _helper = new ViewModelHelperService();
        _log = TestData.MockLogger();


        _tourVm = new Mock<TourViewModel>(
                TestData.MockHttpService().Object,
                TestData.MockToastService().Object,
                TestData.MockConfiguration().Object,
                TestData.MockJsRuntime().Object,
                TestData.MockRouteApiService().Object,
                TestData.MockLogger().Object,
                TestData.MockMapViewModel().Object,
                _helper)
            { CallBase = true };

        _vm = new ReportViewModel(
            _http.Object,
            _toast.Object,
            _log.Object,
            _file.Object,
            _tourVm.Object,
            _helper);
    }

    private Mock<IHttpService> _http = null!;
    private Mock<IToastServiceWrapper> _toast = null!;
    private Mock<IBlazorDownloadFileService> _file = null!;
    private IViewModelHelperService _helper = null!;
    private Mock<TourViewModel> _tourVm = null!;
    private Mock<ILogger> _log = null!;
    private ReportViewModel _vm = null!;


    [Test]
    public void CurrentReportUrl_Change_RaisesPropertyChanged()
    {
        var raised = new List<string?>();
        _vm.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        _vm.CurrentReportUrl = "url";
        Assert.That(_vm.CurrentReportUrl, Is.EqualTo("url"));
        Assert.That(raised, Contains.Item(nameof(ReportViewModel.CurrentReportUrl)));
    }

    [Test]
    public void SelectedDetailedTourId_Change_RaisesPropertyChanged()
    {
        var raised = new List<string?>();
        _vm.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        var id = Guid.NewGuid();
        _vm.SelectedDetailedTourId = id;
        Assert.That(_vm.SelectedDetailedTourId, Is.EqualTo(id));
        Assert.That(raised, Contains.Item(nameof(ReportViewModel.SelectedDetailedTourId)));
    }


    [Test]
    public async Task InitializeAsync_JustRuns()
    {
        await _vm.InitializeAsync();
    }


    [Test]
    public void ResetCurrentReportUrl_ClearsFieldAndNotifies()
    {
        _vm.CurrentReportUrl = "dirty";
        var raised = new List<string?>();
        _vm.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        _vm.ResetCurrentReportUrl();

        Assert.That(_vm.CurrentReportUrl, Is.Empty);
        Assert.That(raised, Contains.Item(nameof(ReportViewModel.CurrentReportUrl)));
    }

    [Test]
    public void ClearCurrentReport_SameAsReset()
    {
        _vm.CurrentReportUrl = "dirty";
        var raised = new List<string?>();
        _vm.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        _vm.ClearCurrentReport();

        Assert.That(_vm.CurrentReportUrl, Is.Empty);
        Assert.That(raised, Contains.Item(nameof(ReportViewModel.CurrentReportUrl)));
    }


    [Test]
    public async Task GenerateAndDownloadReport_NonNullBytes_SetsUrlAndSuccess()
    {
        var bytes = new byte[] { 1, 2, 3 };
        _http.Setup(h => h.GetByteArrayAsync("uri")).ReturnsAsync(bytes);

        await _vm.GenerateAndDownloadReport("uri", "Rpt");

        _file.Verify(f => f.DownloadFile(
            It.IsRegex(@"Rpt_\d{8}_\d{6}\.pdf"), bytes, "application/pdf"), Times.Once);

        Assert.That(_vm.CurrentReportUrl, Does.StartWith("data:application/pdf;base64,"));
        _toast.Verify(t => t.ShowSuccess("Rpt generated successfully."), Times.Once);
    }

    [Test]
    public async Task GenerateAndDownloadReport_NullBytes_DoesNotTouchUrl()
    {
        _http.Setup(h => h.GetByteArrayAsync("void")).ReturnsAsync((byte[]?)null);
        _vm.CurrentReportUrl = "stay";

        await _vm.GenerateAndDownloadReport("void", "Void");

        _file.Verify(f => f.DownloadFile(It.IsAny<string>(), It.IsAny<byte[]>(), "application/pdf"), Times.Once);
        Assert.That(_vm.CurrentReportUrl, Is.EqualTo("stay"));
        _toast.Verify(t => t.ShowSuccess("Void generated successfully."), Times.Once);
    }

    [Test]
    public Task GenerateAndDownloadReport_HttpServiceThrows_PropagatesException()
    {
        var expectedException = new HttpRequestException("Network error");
        _http.Setup(h => h.GetByteArrayAsync("failing-api")).ThrowsAsync(expectedException);

        var actualException =
            Assert.ThrowsAsync<HttpRequestException>(() =>
                _vm.GenerateAndDownloadReport("failing-api", "FailingReport"));

        Assert.That(actualException, Is.SameAs(expectedException));
        _file.VerifyNoOtherCalls();
        _toast.VerifyNoOtherCalls();
        return Task.CompletedTask;
    }

    [Test]
    public Task GenerateAndDownloadReport_DownloadFileThrows_PropagatesException()
    {
        var bytes = new byte[] { 1, 2, 3 };
        _http.Setup(h => h.GetByteArrayAsync("api/test")).ReturnsAsync(bytes);

        var expectedException = new InvalidOperationException("Download failed");
        _file.Setup(f => f.DownloadFile(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<string>()))
            .ThrowsAsync(expectedException);

        var actualException =
            Assert.ThrowsAsync<InvalidOperationException>(() =>
                _vm.GenerateAndDownloadReport("api/test", "TestReport"));

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
        _http.Setup(h => h.GetByteArrayAsync(uri)).ReturnsAsync(bytes);

        await _vm.GenerateAndDownloadReport(uri, reportType);

        _file.Verify(f => f.DownloadFile(
            It.IsRegex($@"{reportType}_\d{{8}}_\d{{6}}\.pdf"),
            bytes,
            "application/pdf"), Times.Once);

        _toast.Verify(t => t.ShowSuccess($"{reportType} generated successfully."), Times.Once);
    }


    [Test]
    public async Task GenerateSummaryReportAsync_CompleteFlow()
    {
        var bytes = new byte[] { 4, 5, 6 };
        _http.Setup(h => h.GetByteArrayAsync("api/reports/summary")).ReturnsAsync(bytes);

        await _vm.GenerateSummaryReportAsync();

        _file.Verify(f => f.DownloadFile(
            It.IsRegex(@"SummaryReport_\d{8}_\d{6}\.pdf"), bytes, "application/pdf"), Times.Once);
        _toast.Verify(t => t.ShowSuccess("SummaryReport generated successfully."), Times.Once);
    }


    [Test]
    public async Task GenerateDetailedReportAsync_NoId_NothingHappens()
    {
        _vm.SelectedDetailedTourId = Guid.Empty;
        await _vm.GenerateDetailedReportAsync();

        _file.VerifyNoOtherCalls();
        _toast.VerifyNoOtherCalls();
    }

    [Test]
    public async Task GenerateDetailedReportAsync_WithImagePath_UpdatesPathAndDownloads()
    {
        var tour = TestData.SampleTour();
        tour.ImagePath = Path.Combine("sub", "img.png");
        _tourVm.Object.Tours.Add(tour);
        _vm.SelectedDetailedTourId = tour.Id;

        var bytes = new byte[] { 7, 7, 7 };
        _http.Setup(h => h.GetByteArrayAsync($"api/reports/tour/{tour.Id}")).ReturnsAsync(bytes);

        await _vm.GenerateDetailedReportAsync();

        var expectedPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "sub", "img.png");
        var actualPath = Path.GetFullPath(tour.ImagePath);
        var normalizedExpected = Path.GetFullPath(expectedPath);

        Assert.That(actualPath, Is.EqualTo(normalizedExpected));

        _file.Verify(f => f.DownloadFile(
            It.IsRegex(@"DetailedReport_\d{8}_\d{6}\.pdf"), bytes, "application/pdf"), Times.Once);
        _toast.Verify(t => t.ShowSuccess("DetailedReport generated successfully."), Times.Once);
    }

    [Test]
    public async Task GenerateDetailedReportAsync_EmptyImagePath_DownloadsButKeepsEmpty()
    {
        var tour = TestData.SampleTour();
        tour.ImagePath = "";
        _tourVm.Object.Tours.Add(tour);
        _vm.SelectedDetailedTourId = tour.Id;

        var bytes = new byte[] { 8, 8, 8 };
        _http.Setup(h => h.GetByteArrayAsync($"api/reports/tour/{tour.Id}")).ReturnsAsync(bytes);

        await _vm.GenerateDetailedReportAsync();

        Assert.That(tour.ImagePath, Is.Empty);
        _file.Verify(f => f.DownloadFile(It.IsAny<string>(), bytes, "application/pdf"), Times.Once);
        _toast.Verify(t => t.ShowSuccess("DetailedReport generated successfully."), Times.Once);
    }

    [Test]
    public async Task GenerateDetailedReportAsync_TourNotFound_SkipsImageLogicAndStillDownloads()
    {
        var missingId = Guid.NewGuid();
        _vm.SelectedDetailedTourId = missingId;


        var fake = new byte[] { 9, 8, 7 };
        _http
            .Setup(h => h.GetByteArrayAsync($"api/reports/tour/{missingId}"))
            .ReturnsAsync(fake);


        await _vm.GenerateDetailedReportAsync();


        _file.Verify(f => f.DownloadFile(
            It.IsRegex(@"DetailedReport_\d{8}_\d{6}\.pdf"),
            fake,
            "application/pdf"
        ), Times.Once);


        _toast.Verify(t => t.ShowSuccess("DetailedReport generated successfully."), Times.Once);
    }

    [Test]
    public async Task GenerateDetailedReportAsync_NullImagePath_DoesNotModifyPath()
    {
        var tour = TestData.SampleTour();
        tour.ImagePath = null!;
        _tourVm.Object.Tours.Add(tour);
        _vm.SelectedDetailedTourId = tour.Id;

        var bytes = new byte[] { 1, 2, 3 };
        _http.Setup(h => h.GetByteArrayAsync($"api/reports/tour/{tour.Id}")).ReturnsAsync(bytes);

        await _vm.GenerateDetailedReportAsync();

        Assert.That(tour.ImagePath, Is.Null);
        _file.Verify(f => f.DownloadFile(It.IsAny<string>(), bytes, "application/pdf"), Times.Once);
    }

    [Test]
    public async Task ExportTourToJsonAsync_Valid_Exports()
    {
        var id = Guid.NewGuid();
        var json = TestData.SampleTourJson();
        _http.Setup(h => h.GetStringAsync($"api/reports/export/{id}")).ReturnsAsync(json);

        await _vm.ExportTourToJsonAsync(id);

        _file.Verify(f => f.DownloadFile(
            It.IsRegex($@"Tour_{id}_\d{{8}}_\d{{6}}\.json"),
            It.Is<byte[]>(b => Encoding.UTF8.GetString(b) == json),
            "application/json"), Times.Once);
        _toast.Verify(t => t.ShowSuccess("Tour exported successfully."), Times.Once);
    }

    [Test]
    public async Task ExportTourToJsonAsync_Invalid_ShowsError()
    {
        var id = Guid.NewGuid();
        _http.Setup(h => h.GetStringAsync($"api/reports/export/{id}")).ReturnsAsync("");

        await _vm.ExportTourToJsonAsync(id);

        _toast.Verify(t => t.ShowError("Error exporting tour: Invalid tour data."), Times.Once);
        _file.Verify(f => f.DownloadFile(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<string>()), Times.Never);
    }


    private static InputFileChangeEventArgs MakeFile(string content)
    {
        var fileMock = TestData.MockBrowserFile(content);
        return new InputFileChangeEventArgs([fileMock.Object]);
    }

    [Test]
    public async Task ImportTourFromJsonAsync_InvalidJson_ShowsError()
    {
        await _vm.ImportTourFromJsonAsync(MakeFile("{bad json"));

        _toast.Verify(t => t.ShowError(It.Is<string>(s => s.StartsWith("Error importing tour"))), Times.Once);
        _http.Verify(h => h.PostAsync(It.IsAny<string>(), It.IsAny<Tour>()), Times.Never);
    }

    [Test]
    public async Task ImportTourFromJsonAsync_Duplicate_ShowsError()
    {
        var duplicate = TestData.SampleTour();
        _tourVm.Object.Tours.Add(duplicate);

        await _vm.ImportTourFromJsonAsync(MakeFile(JsonSerializer.Serialize(duplicate)));

        _toast.Verify(t => t.ShowError(It.Is<string>(s => s.Contains("already exists"))), Times.Once);
        _http.Verify(h => h.PostAsync(It.IsAny<string>(), It.IsAny<Tour>()), Times.Never);
    }

    [Test]
    public async Task ImportTourFromJsonAsync_NewTour_Imports()
    {
        var newTour = TestData.SampleTour();
        var json = JsonSerializer.Serialize(newTour);

        _http
            .Setup(h => h.PostAsync("api/tour", It.IsAny<Tour>()))
            .Returns(Task.FromResult(newTour));

        await _vm.ImportTourFromJsonAsync(MakeFile(json));

        _http.Verify(h => h.PostAsync("api/tour", It.IsAny<Tour>()), Times.Once);
        _toast.Verify(t => t.ShowSuccess("Tour imported successfully."), Times.Once);
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
        var args = MakeFile(json);


        _tourVm.Object.Tours.Clear();


        _http
            .Setup(h => h.PostAsync("api/tour", It.IsAny<Tour>()))
            .Returns(Task.FromResult(tour));

        await _vm.ImportTourFromJsonAsync(args);


        _toast.Verify(t => t.ShowSuccess("Tour imported successfully."), Times.Once);
    }

    [Test]
    public async Task ImportTourFromJsonAsync_NullTourAfterDeserialization_ShowsError()
    {
        await _vm.ImportTourFromJsonAsync(MakeFile("null"));

        _toast.Verify(t => t.ShowError("Error importing tour: Invalid tour data."), Times.Once);
        _http.Verify(h => h.PostAsync(It.IsAny<string>(), It.IsAny<Tour>()), Times.Never);
    }
}