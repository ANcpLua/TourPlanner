using UI.Service.Interface;
using UI.View.Pages;
using UI.ViewModel;

namespace Tests.UI.View;

[TestFixture]
[Parallelizable(ParallelScope.All)]
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public sealed class ReportPageTests : BunitTestBase
{
    protected override void OnSetup() => Services.WithTours(2);

    [Test]
    public void TourDropdown_DisplaysAllTours()
    {
        var options = RenderComponent<ReportPage>().FindAll("select.form-select option");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(options, Has.Count.EqualTo(3));
            Assert.That(options[0].TextContent, Is.EqualTo("Select a Tour"));
        }
    }

    [Test]
    public async Task GenerateDetailedReport_CallsViewModel()
    {
        var tourId = Services.FirstTourId();
        Services.ViewModel<ReportViewModel>().SelectedDetailedTourId = tourId;
        Services.SetupMockReportBytes($"api/reports/tour/{tourId}");
        Services.SetupMockDownloadFile();
        var cut = RenderComponent<ReportPage>();
        await cut.FindAll("button").First(static b => b.TextContent.Contains("Generate Detailed Report"))
            .ClickAsync(new MouseEventArgs());
        Services.Mock<IToastServiceWrapper>()
            .Verify(static t => t.ShowSuccess("DetailedReport generated successfully."), Times.Once);
    }

    [Test]
    public void GenerateDetailedReport_DisabledWhenNoTourSelected()
    {
        Services.ViewModel<ReportViewModel>().SelectedDetailedTourId = Guid.Empty;
        var btn = RenderComponent<ReportPage>().FindAll("button")
            .First(static b => b.TextContent.Contains("Generate Detailed Report"));
        Assert.That(btn.HasAttribute("disabled"), Is.True);
    }

    [Test]
    public async Task GenerateSummaryReport_CallsViewModel()
    {
        Services.SetupMockReportBytes("api/reports/summary");
        Services.SetupMockDownloadFile();
        var cut = RenderComponent<ReportPage>();
        await cut.FindAll("button").First(static b => b.TextContent.Contains("Generate Summary"))
            .ClickAsync(new MouseEventArgs());
        Services.Mock<IToastServiceWrapper>()
            .Verify(static t => t.ShowSuccess("SummaryReport generated successfully."), Times.Once);
    }

    [Test]
    public void IFrame_RendersWhenReportUrlExists()
    {
        const string url = "data:application/pdf;base64,AAAA";
        Services.ViewModel<ReportViewModel>().CurrentReportUrl = url;
        var iframe = RenderComponent<ReportPage>().Find("iframe");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(iframe.GetAttribute("src"), Is.EqualTo(url));
            Assert.That(iframe.GetAttribute("title"), Is.EqualTo("Generated Report"));
        }
    }

    [TestCase(true, "Generating...", true)]
    [TestCase(false, "Generate Summary", false)]
    public void GenerateSummaryButton_ProcessingState(bool isProcessing, string expectedText, bool expectedDisabled)
    {
        Services.ViewModel<ReportViewModel>().IsProcessing = isProcessing;
        var btn = RenderComponent<ReportPage>().FindAll("button").First(b => b.TextContent.Contains(expectedText));
        Assert.That(btn.HasAttribute("disabled"), Is.EqualTo(expectedDisabled));
    }
}
