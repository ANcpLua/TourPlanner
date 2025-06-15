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

    [TestCase(100.5, "100[.,]50")]
    [TestCase(null, "N/A")]
    public void TourCard_Distance_DisplaysCorrectValue(double? distance, string expectedPattern)
    {
        var tour = Services.ViewModel<TourViewModel>().Tours.First();
        tour.Distance = distance;

        var cut = RenderComponent<TourListComponent>(p => p
            .Add(x => x.ViewModel, Services.ViewModel<TourViewModel>())
            .Add(x => x.ReportViewModel, Services.ViewModel<ReportViewModel>()));

        var tourCard = cut.Find("div.tour-card");
        if (distance.HasValue)
        {
            Assert.That(tourCard.TextContent, Does.Match($@"Distance: {expectedPattern} meters"));
        }
        else
        {
            Assert.That(tourCard.TextContent, Does.Contain($"Distance: {expectedPattern} meters"));
        }
    }

    [TestCase(60.0, "60")]
    [TestCase(null, "N/A")]
    public void TourCard_EstimatedTime_DisplaysCorrectValue(double? estimatedTime, string expectedText)
    {
        var tour = Services.ViewModel<TourViewModel>().Tours.First();
        tour.EstimatedTime = estimatedTime;

        var cut = RenderComponent<TourListComponent>(p => p
            .Add(x => x.ViewModel, Services.ViewModel<TourViewModel>())
            .Add(x => x.ReportViewModel, Services.ViewModel<ReportViewModel>()));

        var tourCard = cut.Find("div.tour-card");
        Assert.That(tourCard.TextContent, Does.Contain($"Estimated Time: {expectedText} minutes"));
    }

    [TestCase(true, "Yes")]
    [TestCase(false, "No")]
    public void TourCard_ChildFriendly_DisplaysCorrectValue(bool isChildFriendly, string expectedText)
    {
        var tour = Services.ViewModel<TourViewModel>().Tours.First();
        tour.TourLogs.Clear();
        if (isChildFriendly)
        {
            tour.TourLogs.Add(new TourLog { Difficulty = 2, Rating = 3 });
        }

        var cut = RenderComponent<TourListComponent>(p => p
            .Add(x => x.ViewModel, Services.ViewModel<TourViewModel>())
            .Add(x => x.ReportViewModel, Services.ViewModel<ReportViewModel>()));

        var tourCard = cut.Find("div.tour-card");
        Assert.That(tourCard.TextContent, Does.Contain($"Child Friendly: {expectedText}"));
    }

    [Test]
    public void EditButton_FormVisibleForSelectedTour_ShowsHideEditForm()
    {
        var tour = Services.ViewModel<TourViewModel>().Tours.First();
        Services.ViewModel<TourViewModel>().IsFormVisible = true;
        Services.ViewModel<TourViewModel>().SelectedTour = tour;

        var cut = RenderComponent<TourListComponent>(p => p
            .Add(x => x.ViewModel, Services.ViewModel<TourViewModel>())
            .Add(x => x.ReportViewModel, Services.ViewModel<ReportViewModel>()));

        var editButton = cut.FindAll("button").First(b => b.TextContent.Contains("Hide Edit Form"));
        Assert.That(editButton, Is.Not.Null);
    }

    [Test]
    public void EditButton_FormNotVisibleOrDifferentTour_ShowsEdit()
    {
        Services.ViewModel<TourViewModel>().IsFormVisible = false;

        var cut = RenderComponent<TourListComponent>(p => p
            .Add(x => x.ViewModel, Services.ViewModel<TourViewModel>())
            .Add(x => x.ReportViewModel, Services.ViewModel<ReportViewModel>()));

        var editButton = cut.FindAll("button").First(b => b.TextContent.Trim() == "Edit");
        Assert.That(editButton, Is.Not.Null);
    }

    [TestCase(true, "Exporting...")]
    [TestCase(false, "Export")]
    public void ExportButton_ProcessingState_DisplaysCorrectText(bool isProcessing, string expectedText)
    {
        Services.ViewModel<ReportViewModel>().IsProcessing = isProcessing;

        var cut = RenderComponent<TourListComponent>(p => p
            .Add(x => x.ViewModel, Services.ViewModel<TourViewModel>())
            .Add(x => x.ReportViewModel, Services.ViewModel<ReportViewModel>()));

        var exportButton = cut.FindAll("button").First(b => b.TextContent.Contains(expectedText));
        Assert.That(exportButton, Is.Not.Null);
    }
}