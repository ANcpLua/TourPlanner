using UI.Model;
using UI.Service.Interface;
using UI.View.TourComponents;
using UI.ViewModel;

namespace Test.UI.View;

[TestFixture]
[Parallelizable(ParallelScope.All)]
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public sealed class TourListComponentTests : BunitTestBase
{
    protected override void OnSetup()
    {
        Services.ViewModel<TourViewModel>().Tours = [..TestData.SampleTourList(2)];
    }

    [Test]
    public void RenderAllTours_DisplaysCorrectNumberOfCards()
    {
        var cut = RenderComponent<TourListComponent>(p => p
            .Add(x => x.ViewModel, Services.ViewModel<TourViewModel>())
            .Add(x => x.ReportViewModel, Services.ViewModel<ReportViewModel>()));

        var tourCards = cut.FindAll("div.tour-card");
        Assert.That(tourCards, Has.Count.EqualTo(2));
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task DeleteTour_HandlesUserConfirmation(bool userConfirms)
    {
        var tour = Services.ViewModel<TourViewModel>().Tours.First();
        JSInterop.Setup<bool>("confirm", _ => true).SetResult(userConfirms);

        if (userConfirms)
            Services.Mock<IHttpService>().Setup(s => s.DeleteAsync($"api/tour/{tour.Id}"))
                .Returns(Task.CompletedTask);

        var cut = RenderComponent<TourListComponent>(p => p
            .Add(x => x.ViewModel, Services.ViewModel<TourViewModel>())
            .Add(x => x.ReportViewModel, Services.ViewModel<ReportViewModel>()));

        await cut.Find("button.btn-danger").ClickAsync(new MouseEventArgs());

        Services.Mock<IHttpService>().Verify(
            s => s.DeleteAsync($"api/tour/{tour.Id}"),
            userConfirms ? Times.Once : Times.Never);
    }

    [Test]
    public async Task ShowTourDetails_LoadsAndDisplaysModal()
    {
        var tour = Services.ViewModel<TourViewModel>().Tours.First();
        Services.Mock<IHttpService>().Setup(s => s.GetAsync<Tour>($"api/tour/{tour.Id}"))
            .ReturnsAsync(tour);

        JSInterop.SetupVoid("showModal");

        var cut = RenderComponent<TourListComponent>(p => p
            .Add(x => x.ViewModel, Services.ViewModel<TourViewModel>())
            .Add(x => x.ReportViewModel, Services.ViewModel<ReportViewModel>()));

        var detailsButton = cut.FindAll("button").First(b => b.TextContent.Trim() == "Details");
        await detailsButton.ClickAsync(new MouseEventArgs());

        Services.Mock<IHttpService>().Verify(s => s.GetAsync<Tour>($"api/tour/{tour.Id}"), Times.Once);
        JSInterop.VerifyInvoke("showModal", 1);
    }

    [Test]
    public void EmptyTourList_DisplaysNoToursMessage()
    {
        Services.ViewModel<TourViewModel>().Tours.Clear();

        var cut = RenderComponent<TourListComponent>(p => p
            .Add(x => x.ViewModel, Services.ViewModel<TourViewModel>())
            .Add(x => x.ReportViewModel, Services.ViewModel<ReportViewModel>()));

        Assert.That(cut.Markup, Does.Contain("No tours available"));
    }
}