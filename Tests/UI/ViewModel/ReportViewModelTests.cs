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
        var (client, handler) = TestData.MockedHttpClient();
        _httpClient = client;
        _mockHandler = handler;
        _mockToastService = TestData.MockToastService();
        _mockDownloadFileService = TestData.MockBlazorDownloadFileService();

        _reportViewModel = new ReportViewModel(
            _httpClient,
            _mockToastService.Object,
            TestData.MockTryCatchToastWrapper(),
            _mockDownloadFileService.Object);
    }

    private HttpClient _httpClient = null!;
    private Mock<HttpMessageHandler> _mockHandler = null!;
    private Mock<IToastServiceWrapper> _mockToastService = null!;
    private Mock<IBlazorDownloadFileService> _mockDownloadFileService = null!;
    private ReportViewModel _reportViewModel = null!;

    [TestCase(true, "Generating...")]
    [TestCase(false, "Generate Summary")]
    public void SummaryButtonText_ReflectsProcessing(bool processing, string expected)
    {
        _reportViewModel.IsProcessing = processing;
        Assert.That(_reportViewModel.SummaryButtonText, Is.EqualTo(expected));
    }

    [TestCase(true, "Exporting...")]
    [TestCase(false, "Export")]
    public void ExportButtonText_ReflectsProcessing(bool processing, string expected)
    {
        _reportViewModel.IsProcessing = processing;
        Assert.That(_reportViewModel.ExportButtonText, Is.EqualTo(expected));
    }

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
        TestData.SetupHandler(_mockHandler, HttpMethod.Get, "api/tour",
            JsonSerializer.Serialize(new List<Tour> { TestData.SampleTour() }));

        await _reportViewModel.InitializeAsync();

        Assert.That(_reportViewModel.Tours, Has.Count.EqualTo(1));
    }

    [Test]
    [TestCase("api/reports/summary", "SummaryReport")]
    [TestCase("api/reports/detailed", "DetailedReport")]
    public async Task GenerateAndDownloadReport_Success(string uri, string reportType)
    {
        byte[] bytes = [1, 2, 3];
        TestData.SetupHandlerBytes(_mockHandler, uri, bytes);

        await _reportViewModel.GenerateAndDownloadReport(uri, reportType);

        _mockDownloadFileService.Verify(f => f.DownloadFileAsync(
            It.IsRegex($@"{reportType}_\d{{8}}_\d{{6}}\.pdf"), bytes, "application/pdf"), Times.Once);
        _mockToastService.Verify(t => t.ShowSuccess($"{reportType} generated successfully."), Times.Once);
    }

    [Test]
    public async Task GenerateAndDownloadReport_EmptyBytes_ShowsError()
    {
        TestData.SetupHandlerBytes(_mockHandler, "uri", []);

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
        TestData.SetupHandlerBytes(_mockHandler, $"api/reports/tour/{tourId}", [7, 7, 7]);

        await _reportViewModel.GenerateDetailedReportAsync();

        _mockDownloadFileService.Verify(static f => f.DownloadFileAsync(
            It.IsRegex(@"DetailedReport_\d{8}_\d{6}\.pdf"), It.IsAny<byte[]>(), "application/pdf"), Times.Once);
    }

    [Test]
    public async Task ExportTourToJsonAsync_Valid_Exports()
    {
        var id = Guid.NewGuid();
        var json = TestData.SampleTourJson();
        TestData.SetupHandler(_mockHandler, HttpMethod.Get, $"api/reports/export/{id}", json);

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
        TestData.SetupHandler(_mockHandler, HttpMethod.Get, $"api/reports/export/{id}", "");

        await _reportViewModel.ExportTourToJsonAsync(id);

        _mockToastService.Verify(static t => t.ShowError("Error exporting tour: Invalid tour data."), Times.Once);
    }

    [Test]
    public async Task ImportTourFromJsonAsync_NewTour_Imports()
    {
        var newTour = TestData.SampleTour();
        TestData.SetupHandler(_mockHandler, HttpMethod.Post, "api/tour", "{}");
        TestData.SetupHandler(_mockHandler, HttpMethod.Get, "api/tour", "[]");

        var json = JsonSerializer.Serialize(newTour);
        await _reportViewModel.ImportTourFromJsonAsync(TestData.MakeFile(json));

        TestData.VerifyHandler(_mockHandler, HttpMethod.Post, "api/tour", Times.Once());
        _mockToastService.Verify(static t => t.ShowSuccess("Tour imported successfully."), Times.Once);
    }

    [Test]
    public async Task ImportTourFromJsonAsync_Duplicate_ShowsError()
    {
        var duplicate = TestData.SampleTour();
        TestData.SetupHandler(_mockHandler, HttpMethod.Get, "api/tour",
            JsonSerializer.Serialize(new List<Tour> { duplicate }));
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
