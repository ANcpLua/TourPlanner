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
    public void RendersInitialStructureCorrectly()
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
        var logs = TestData.SampleTourLogList(3, tourId);

        Services.Mock<IHttpService>().Setup(x => x.GetListAsync<TourLog>($"api/tourlog/bytour/{tourId}"))
            .ReturnsAsync(logs);

        var cut = RenderComponent<TourLogPage>();
        cut.Find("select").Change(tourId.ToString());

        cut.WaitForAssertion(() =>
            Assert.That(Services.ViewModel<TourLogViewModel>().TourLogs, Has.Count.EqualTo(3)));
    }

    [TestCase("", 0, false)]
    [TestCase("Valid", 3, true)]
    public void ValidatesFormCorrectly(string comment, int difficulty, bool expectedValid)
    {
        var log = TestData.SampleTourLog(difficulty: difficulty);
        log.Comment = comment;
        Services.ViewModel<TourLogViewModel>().SelectedTourLog = log;

        Assert.That(Services.ViewModel<TourLogViewModel>().IsFormValid, Is.EqualTo(expectedValid));
    }

    [Test]
    public async Task TourViewModel_ToursPropertyChanged_InvokesStateHasChanged()
    {
        var cut = RenderComponent<TourLogPage>();
        var tourViewModel = Services.ViewModel<TourViewModel>();
        var initialRenderCount = cut.RenderCount;

        await cut.InvokeAsync(() =>
        {
            tourViewModel.Tours = new ObservableCollection<Tour>(TestData.SampleTourList());
            tourViewModel.OnPropertyChanged(nameof(TourViewModel.Tours));
        });

        Assert.That(cut.RenderCount, Is.GreaterThan(initialRenderCount));
    }

    [Test]
    public async Task TourViewModel_OtherPropertyChanged_DoesNotInvokeStateHasChanged()
    {
        var cut = RenderComponent<TourLogPage>();
        var tourViewModel = Services.ViewModel<TourViewModel>();
        var initialRenderCount = cut.RenderCount;

        await cut.InvokeAsync(() =>
        {
            tourViewModel.IsMapVisible = !tourViewModel.IsMapVisible;
            tourViewModel.OnPropertyChanged(nameof(TourViewModel.IsMapVisible));
        });

        Assert.That(cut.RenderCount, Is.EqualTo(initialRenderCount));
    }
}