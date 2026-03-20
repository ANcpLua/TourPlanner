using UI.Model;
using UI.Service.Interface;
using UI.ViewModel;

namespace Tests.UI.ViewModel;

[TestFixture]
public sealed class ReportViewModelTests
{
    [SetUp]
    public void Setup()
    {
        _mockHttpService = TestData.MockHttpService();
        _mockToastService = TestData.MockToastService();
        _mockDownloadFileService = TestData.MockBlazorDownloadFileService();
        _mockLogger = TestData.MockLogger();

        _reportViewModel = new ReportViewModel(
            _mockHttpService.Object,
            _mockToastService.Object,
            _mockLogger.Object,
            _mockDownloadFileService.Object);
    }

    private Mock<IHttpService> _mockHttpService = null!;
    private Mock<IToastServiceWrapper> _mockToastService = null!;
    private Mock<IBlazorDownloadFileService> _mockDownloadFileService = null!;
    private Mock<ILogger> _mockLogger = null!;
    private ReportViewModel _reportViewModel = null!;

    [Test]
    public void CurrentReportUrl_Change_RaisesPropertyChanged()
    {
        List<string?> raised = [];
        _reportViewModel.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        _reportViewModel.CurrentReportUrl = "url";
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_reportViewModel.CurrentReportUrl, Is.EqualTo("url"));
            Assert.That(raised, Contains.Item(nameof(ReportViewModel.CurrentReportUrl)));
        }
    }

    [Test]
    public void ClearCurrentReport_ClearsFieldAndNotifies()
    {
        _reportViewModel.CurrentReportUrl = "dirty";
        List<string?> raised = [];
        _reportViewModel.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        _reportViewModel.ClearCurrentReport();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_reportViewModel.CurrentReportUrl, Is.Empty);
            Assert.That(raised, Contains.Item(nameof(ReportViewModel.CurrentReportUrl)));
        }
    }

    [Test]
    public async Task InitializeAsync_LoadsTours()
    {
        _mockHttpService.Setup(static h => h.GetListAsync<Tour>("api/tour"))
            .ReturnsAsync(new List<Tour> { TestData.SampleTour() });

        await _reportViewModel.InitializeAsync();

        Assert.That(_reportViewModel.Tours, Has.Count.EqualTo(1));
    }

    [Test]
    [TestCase("api/reports/summary", "SummaryReport")]
    [TestCase("api/reports/detailed", "DetailedReport")]
    public async Task GenerateAndDownloadReport_Success(string uri, string reportType)
    {
        byte[] bytes = [1, 2, 3];
        _mockHttpService.Setup(h => h.GetByteArrayAsync(uri)).ReturnsAsync(bytes);

        await _reportViewModel.GenerateAndDownloadReport(uri, reportType);

        _mockDownloadFileService.Verify(f => f.DownloadFileAsync(
            It.IsRegex($@"{reportType}_\d{{8}}_\d{{6}}\.pdf"), bytes, "application/pdf"), Times.Once);
        _mockToastService.Verify(t => t.ShowSuccess($"{reportType} generated successfully."), Times.Once);
    }

    [Test]
    public async Task GenerateAndDownloadReport_NullBytes_ShowsError()
    {
        _mockHttpService.Setup(static h => h.GetByteArrayAsync("uri")).ReturnsAsync((byte[])null!);

        await _reportViewModel.GenerateAndDownloadReport("uri", "Rpt");

        _mockDownloadFileService.VerifyNoOtherCalls();
        _mockToastService.Verify(static t => t.ShowError("Error generating Rpt: No data received."), Times.Once);
    }

    [Test]
    public async Task GenerateDetailedReportAsync_NoId_NothingHappens()
    {
        _reportViewModel.SelectedDetailedTourId = Guid.Empty;
        await _reportViewModel.GenerateDetailedReportAsync();

        _mockDownloadFileService.VerifyNoOtherCalls();
    }

    [Test]
    public async Task GenerateDetailedReportAsync_WithId_Downloads()
    {
        var tourId = Guid.NewGuid();
        _reportViewModel.SelectedDetailedTourId = tourId;
        _mockHttpService.Setup(h => h.GetByteArrayAsync($"api/reports/tour/{tourId}"))
            .ReturnsAsync(new byte[] { 7, 7, 7 });

        await _reportViewModel.GenerateDetailedReportAsync();

        _mockDownloadFileService.Verify(f => f.DownloadFileAsync(
            It.IsRegex(@"DetailedReport_\d{8}_\d{6}\.pdf"), It.IsAny<byte[]>(), "application/pdf"), Times.Once);
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
    }

    [Test]
    public async Task ExportTourToJsonAsync_Empty_ShowsError()
    {
        var id = Guid.NewGuid();
        _mockHttpService.Setup(h => h.GetStringAsync($"api/reports/export/{id}")).ReturnsAsync("");

        await _reportViewModel.ExportTourToJsonAsync(id);

        _mockToastService.Verify(static t => t.ShowError("Error exporting tour: Invalid tour data."), Times.Once);
    }

    [Test]
    public async Task ImportTourFromJsonAsync_NewTour_Imports()
    {
        var newTour = TestData.SampleTour();
        _mockHttpService.Setup(static h => h.PostAsync("api/tour", It.IsAny<Tour>()))
            .Returns(Task.FromResult(newTour));
        _mockHttpService.Setup(static h => h.GetListAsync<Tour>("api/tour"))
            .ReturnsAsync(new List<Tour>());

        await _reportViewModel.ImportTourFromJsonAsync(TestData.MakeFile(JsonSerializer.Serialize(newTour)));

        _mockHttpService.Verify(static h => h.PostAsync("api/tour", It.IsAny<Tour>()), Times.Once);
        _mockToastService.Verify(static t => t.ShowSuccess("Tour imported successfully."), Times.Once);
    }

    [Test]
    public async Task ImportTourFromJsonAsync_Duplicate_ShowsError()
    {
        var duplicate = TestData.SampleTour();
        _mockHttpService.Setup(static h => h.GetListAsync<Tour>("api/tour"))
            .ReturnsAsync(new List<Tour> { duplicate });
        await _reportViewModel.InitializeAsync();

        await _reportViewModel.ImportTourFromJsonAsync(TestData.MakeFile(JsonSerializer.Serialize(duplicate)));

        _mockToastService.Verify(static t => t.ShowError(It.Is<string>(static s => s.Contains("already exists"))), Times.Once);
    }

    [Test]
    public async Task ImportTourFromJsonAsync_NullJson_ShowsError()
    {
        await _reportViewModel.ImportTourFromJsonAsync(TestData.MakeFile("null"));

        _mockToastService.Verify(static t => t.ShowError("Error importing tour: Invalid tour data."), Times.Once);
    }
}
