using UI.Model;
using UI.Service.Interface;
using UI.View.Pages;
using UI.ViewModel;

namespace Test.UI.View;

[TestFixture]
[Parallelizable(ParallelScope.All)]
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public sealed class ReportPageTests : BunitTestBase
{
    protected override void OnSetup()
    {
        var tours = TestData.SampleTourList(2);
        Services.Mock<IHttpService>().Setup(s => s.GetListAsync<Tour>("api/tour")).ReturnsAsync(tours);
        Services.ViewModel<TourViewModel>().Tours = new ObservableCollection<Tour>(tours);
    }

    [Test]
    public void TourDropdown_DisplaysAllTours()
    {
        var cut = RenderComponent<ReportPage>();
        var options = cut.FindAll("select.form-select option");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(options, Has.Count.EqualTo(3));
            Assert.That(options[0].TextContent, Is.EqualTo("Select a Tour"));
            Assert.That(options[1].TextContent, Does.Contain("Tour 1"));
            Assert.That(options[2].TextContent, Does.Contain("Tour 2"));
        }
    }

    [Test]
    public async Task GenerateDetailedReport_CallsViewModel()
    {
        var reportVm = Services.ViewModel<ReportViewModel>();
        var tourId = Services.ViewModel<TourViewModel>().Tours.First().Id;
        reportVm.SelectedDetailedTourId = tourId;

        Services.Mock<IHttpService>().Setup(s => s.GetByteArrayAsync($"api/reports/tour/{tourId}"))
            .ReturnsAsync([1, 2, 3]);
        Services.Mock<IBlazorDownloadFileService>().Setup(b => b.DownloadFileAsync(
                It.IsAny<string>(), It.IsAny<byte[]>(), "application/pdf"))
            .Returns(new ValueTask<bool>(true));

        var cut = RenderComponent<ReportPage>();
        var button = cut.FindAll("button").First(b => b.TextContent.Contains("Generate Detailed Report"));
        await button.ClickAsync(new MouseEventArgs());

        Services.Mock<IHttpService>().Verify(s => s.GetByteArrayAsync($"api/reports/tour/{tourId}"), Times.Once);
        Services.Mock<IToastServiceWrapper>()
            .Verify(t => t.ShowSuccess("DetailedReport generated successfully."), Times.Once);
    }

    [Test]
    public void GenerateDetailedReport_DisabledWhenNoTourSelected()
    {
        Services.ViewModel<ReportViewModel>().SelectedDetailedTourId = Guid.Empty;
        var cut = RenderComponent<ReportPage>();

        var button = cut.FindAll("button").First(b => b.TextContent.Contains("Generate Detailed Report"));
        Assert.That(button.HasAttribute("disabled"), Is.True);
    }

    [Test]
    public async Task GenerateSummaryReport_CallsViewModel()
    {
        Services.Mock<IHttpService>().Setup(s => s.GetByteArrayAsync("api/reports/summary"))
            .ReturnsAsync([1, 2, 3]);
        Services.Mock<IBlazorDownloadFileService>().Setup(b => b.DownloadFileAsync(
                It.IsAny<string>(), It.IsAny<byte[]>(), "application/pdf"))
            .Returns(new ValueTask<bool>(true));

        var cut = RenderComponent<ReportPage>();
        var button = cut.FindAll("button").First(b => b.TextContent.Contains("Generate Summary"));
        await button.ClickAsync(new MouseEventArgs());

        Services.Mock<IHttpService>().Verify(s => s.GetByteArrayAsync("api/reports/summary"), Times.Once);
        Services.Mock<IToastServiceWrapper>()
            .Verify(t => t.ShowSuccess("SummaryReport generated successfully."), Times.Once);
    }

    [Test]
    public void IFrame_RendersWhenReportUrlExists()
    {
        const string reportUrl = "data:application/pdf;base64,AAAA";
        Services.ViewModel<ReportViewModel>().CurrentReportUrl = reportUrl;

        var cut = RenderComponent<ReportPage>();
        var iframe = cut.Find("iframe");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(iframe.GetAttribute("src"), Is.EqualTo(reportUrl));
            Assert.That(iframe.GetAttribute("title"), Is.EqualTo("Generated Report"));
        }
    }
    
    [TestCase(true, "Generating...", true)]
    [TestCase(false, "Generate Summary", false)]
    public void GenerateSummaryButton_ProcessingState_DisplaysCorrectly(bool isProcessing, string expectedText, bool expectedDisabled)
    {
        Services.ViewModel<ReportViewModel>().IsProcessing = isProcessing;
    
        var component = RenderComponent<ReportPage>();
    
        var generateButton = component.FindAll("button")
            .First(b => b.TextContent.Contains(expectedText));
        Assert.That(generateButton.HasAttribute("disabled"), Is.EqualTo(expectedDisabled));
    }
}