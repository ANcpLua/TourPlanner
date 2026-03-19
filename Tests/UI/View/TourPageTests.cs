using UI.Service.Interface;
using UI.View.Pages;
using UI.View.TourComponents;
using UI.ViewModel;

namespace Test.UI.View;

[TestFixture]
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
[Parallelizable(ParallelScope.All)]
public sealed class TourPageTests : BunitTestBase
{
    [Test]
    public void TourList_WhenToursExist_RendersListComponent()
    {
        var cut = RenderComponent<TourPage>();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(cut.FindComponents<TourListComponent>(), Has.Count.EqualTo(1));
            Assert.Throws<ElementNotFoundException>(() =>
                cut.Find("div.tour-list-container-tour-card p.text-center"));
        }
    }

    [Test]
    public void TourList_WhenNoTours_ShowsEmptyMessage()
    {
        Services.WithEmptyTours();
        Assert.That(RenderComponent<TourPage>().Find("div.tour-list-container-tour-card p.text-center").TextContent,
            Is.EqualTo("No tours available. Please add a tour."));
    }

    [Test]
    public void MapComponent_WhenMapVisible_RendersMap()
    {
        Services.ViewModel<TourViewModel>().IsMapVisible = true;
        Assert.That(RenderComponent<TourPage>().FindComponents<MapComponent>(), Has.Count.EqualTo(1));
    }

    [Test]
    public async Task MapToggleButton_TogglesVisibility()
    {
        Services.ViewModel<TourViewModel>().IsMapVisible = false;
        var cut = RenderComponent<TourPage>();
        await cut.Find("button.show-map-btn").ClickAsync(new MouseEventArgs());
        Assert.That(Services.ViewModel<TourViewModel>().IsMapVisible, Is.True);
    }

    [TestCase(true, "Hide Map")]
    [TestCase(false, "Show Map")]
    public void MapToggleButton_ShowsCorrectText(bool isVisible, string expectedText)
    {
        Services.ViewModel<TourViewModel>().IsMapVisible = isVisible;
        Assert.That(RenderComponent<TourPage>().Find("button.show-map-btn").TextContent.Trim(), Is.EqualTo(expectedText));
    }

    [Test]
    public async Task FileImport_WithValidJson_ImportsTour()
    {
        Services.SetupMockPostTour();
        var cut = RenderComponent<TourPage>();
        var fileInput = cut.FindComponent<CustomFileInput>();
        await fileInput.InvokeAsync(() =>
            fileInput.Instance.OnChange.InvokeAsync(
                new InputFileChangeEventArgs([TestData.MockBrowserFile(TestData.SampleTourJson()).Object])));
        Services.VerifyMockPostTour(Times.Once());
    }

    [Test]
    public async Task FileImport_WithInvalidJson_ShowsError()
    {
        var cut = RenderComponent<TourPage>();
        var fileInput = cut.FindComponent<CustomFileInput>();
        await fileInput.InvokeAsync(() =>
            fileInput.Instance.OnChange.InvokeAsync(
                new InputFileChangeEventArgs([TestData.MockBrowserFile("invalid json").Object])));
        Services.Mock<IToastServiceWrapper>().Verify(t => t.ShowError(It.IsAny<string>()), Times.AtLeastOnce);
    }

    [Test]
    public void PageStructure_ContainsAllRequiredElements()
    {
        var cut = RenderComponent<TourPage>();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(cut.Find("div.tour-planner-app"), Is.Not.Null);
            Assert.That(cut.Find("div.tour-planner-container"), Is.Not.Null);
            Assert.That(cut.FindAll("button"), Has.Count.GreaterThanOrEqualTo(2));
            Assert.That(cut.FindComponents<CustomFileInput>(), Has.Count.EqualTo(1));
            Assert.That(cut.FindComponents<TourDetailsModal>(), Has.Count.EqualTo(1));
        }
    }

    [Test]
    public async Task ReportViewModel_PropertyChanged_InvokesStateHasChanged()
    {
        var cut = RenderComponent<TourPage>();
        var initial = cut.RenderCount;
        await cut.InvokeAsync(() => Services.ViewModel<ReportViewModel>().CurrentReportUrl = "test-report-url");
        Assert.That(cut.RenderCount, Is.GreaterThan(initial));
    }

    [Test]
    public async Task MapViewModel_PropertyChanged_InvokesStateHasChanged()
    {
        var cut = RenderComponent<TourPage>();
        var initial = cut.RenderCount;
        await cut.InvokeAsync(() => Services.ViewModel<MapViewModel>().FromCity = "Vienna");
        Assert.That(cut.RenderCount, Is.GreaterThan(initial));
    }
}
