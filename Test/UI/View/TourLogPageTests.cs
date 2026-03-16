using UI.Model;
using UI.Service.Interface;
using UI.View.Pages;
using UI.ViewModel;

namespace Test.UI.View;

[TestFixture]
[Parallelizable(ParallelScope.All)]
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public sealed class TourLogPageTests : BunitTestBase
{
    protected override void OnSetup()
    {
        var tours = TestData.SampleTourList(2);
        Services.Mock<IHttpService>().Setup(s => s.GetListAsync<Tour>("api/tour")).ReturnsAsync(tours);
        Services.ViewModel<TourViewModel>().Tours = new ObservableCollection<Tour>(tours);
    }

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
        var tourId = Services.ViewModel<TourViewModel>().Tours.First().Id;
        Services.Mock<IHttpService>().Setup(x => x.GetListAsync<TourLog>($"api/tourlog/bytour/{tourId}"))
            .ReturnsAsync(TestData.SampleTourLogList(3, tourId));

        var cut = RenderComponent<TourLogPage>();
        cut.Find("select").Change(tourId.ToString());

        cut.WaitForAssertion(() =>
            Assert.That(Services.ViewModel<TourLogViewModel>().TourLogs, Has.Count.EqualTo(3)));
    }

    [Test]
    public void FormHeader_NewLog_ShowsAddText()
    {
        var tourId = Services.ViewModel<TourViewModel>().Tours.First().Id;
        Services.ViewModel<TourLogViewModel>().SelectedTourId = tourId;
        Services.ViewModel<TourLogViewModel>().IsLogFormVisible = true;
        Services.ViewModel<TourLogViewModel>().SelectedTourLog = new TourLog { Id = Guid.Empty };

        var cut = RenderComponent<TourLogPage>();

        Assert.That(cut.Find(".tour-log-form-section h5").TextContent, Is.EqualTo("Add New Log"));
    }

    [Test]
    public void FormHeader_ExistingLog_ShowsEditText()
    {
        var tourId = Services.ViewModel<TourViewModel>().Tours.First().Id;
        Services.ViewModel<TourLogViewModel>().SelectedTourId = tourId;
        Services.ViewModel<TourLogViewModel>().IsLogFormVisible = true;
        Services.ViewModel<TourLogViewModel>().SelectedTourLog = new TourLog { Id = Guid.NewGuid() };

        var cut = RenderComponent<TourLogPage>();

        Assert.That(cut.Find(".tour-log-form-section h5").TextContent, Is.EqualTo("Edit Log"));
    }

    [Test]
    public async Task TourViewModel_ToursChanged_ReRendersPage()
    {
        var cut = RenderComponent<TourLogPage>();
        var initial = cut.RenderCount;

        await cut.InvokeAsync(() =>
        {
            var vm = Services.ViewModel<TourViewModel>();
            vm.Tours = new ObservableCollection<Tour>(TestData.SampleTourList());
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