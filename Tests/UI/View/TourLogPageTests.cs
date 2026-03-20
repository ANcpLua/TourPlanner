using UI.Service.Interface;
using UI.View.Pages;
using UI.ViewModel;

namespace Tests.UI.View;

[TestFixture]
[Parallelizable(ParallelScope.All)]
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public sealed class TourLogPageTests : BunitTestBase
{
    protected override void OnSetup() => Services.WithTours(2);

    [Test]
    public void RendersInitialStructure()
    {
        var cut = RenderComponent<TourLogPage>();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(cut.FindAll(".form-select"), Has.Count.EqualTo(1));
            Assert.That(cut.FindAll(".add-log-btn"), Has.Count.EqualTo(1));
            Assert.That(cut.FindAll("form"), Is.Empty);
        }
    }

    [Test]
    public void TourSelection_LoadsTourLogs()
    {
        var tourId = Services.FirstTourId();
        Services.SetupMockGetTourLogs(tourId);
        var cut = RenderComponent<TourLogPage>();
        cut.Find("select").Change(tourId.ToString());
        cut.WaitForAssertion(() =>
            Assert.That(cut.FindAll(".tour-card"), Has.Count.EqualTo(3)));
    }

    [Test]
    public void FormHeader_NewLog_ShowsAddText()
    {
        Services.WithTourLogFormVisible(newLog: true);
        Assert.That(RenderComponent<TourLogPage>().Find(".tour-log-form-section h5").TextContent, Is.EqualTo("Add New Log"));
    }

    [Test]
    public void FormHeader_ExistingLog_ShowsEditText()
    {
        Services.WithTourLogFormVisible(newLog: false);
        Assert.That(RenderComponent<TourLogPage>().Find(".tour-log-form-section h5").TextContent, Is.EqualTo("Edit Log"));
    }

    [Test]
    public async Task TourViewModel_ToursChanged_ReRendersPage()
    {
        var cut = RenderComponent<TourLogPage>();
        var initial = cut.RenderCount;
        await cut.InvokeAsync(() =>
        {
            var vm = Services.ViewModel<TourViewModel>();
            Services.WithTours();
            vm.OnPropertyChanged(nameof(TourViewModel.Tours));
        });
        Assert.That(cut.RenderCount, Is.GreaterThan(initial));
    }

    [Test]
    public async Task TourViewModel_UnrelatedPropertyChanged_DoesNotReRender()
    {
        var cut = RenderComponent<TourLogPage>();
        var initial = cut.RenderCount;
        await cut.InvokeAsync(() =>
        {
            var vm = Services.ViewModel<TourViewModel>();
            vm.IsMapVisible = !vm.IsMapVisible;
            vm.OnPropertyChanged(nameof(TourViewModel.IsMapVisible));
        });
        Assert.That(cut.RenderCount, Is.EqualTo(initial));
    }
}
