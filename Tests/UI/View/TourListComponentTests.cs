using UI.View.TourComponents;
using UI.ViewModel;

namespace Tests.UI.View;

[TestFixture]
[Parallelizable(ParallelScope.All)]
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public sealed class TourListComponentTests : BunitTestBase
{
    protected override void OnSetup() => Services.WithTours();

    private IRenderedComponent<TourListComponent> Render() =>
        RenderComponent<TourListComponent>(p => p
            .Add(static x => x.ViewModel, Services.ViewModel<TourViewModel>())
            .Add(static x => x.ReportViewModel, Services.ViewModel<ReportViewModel>()));

    [Test]
    public void RendersTourCardPerTour() =>
        Assert.That(Render().FindAll("div.tour-card"), Has.Count.EqualTo(2));

    [Test]
    public void EmptyTourList_ShowsMessage()
    {
        Services.ViewModel<TourViewModel>().Tours = [];
        Assert.That(Render().Markup, Does.Contain("No tours available"));
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task DeleteTour_RespectsConfirmation(bool confirms)
    {
        var id = Services.FirstTourId();
        JsInterop.Setup<bool>("confirm", static _ => true).SetResult(confirms);
        if (confirms) Services.SetupMockDeleteTour(id);
        await Render().Find("button.btn-danger").ClickAsync(new MouseEventArgs());
        Services.VerifyMockDeleteTour(id, confirms ? Times.Once() : Times.Never());
    }

    [Test]
    public void EditButton_WhenEditingThisTour_ShowsHideText()
    {
        var vm = Services.ViewModel<TourViewModel>();
        vm.IsFormVisible = true;
        vm.SelectedTour = vm.Tours.First();
        Assert.That(Render().FindAll("button").First(static b => b.TextContent.Contains("Hide Edit Form")), Is.Not.Null);
    }

    [Test]
    public void EditButton_WhenNotEditing_ShowsEditText()
    {
        Services.ViewModel<TourViewModel>().IsFormVisible = false;
        Assert.That(Render().FindAll("button").First(static b => b.TextContent.Trim() == "Edit"), Is.Not.Null);
    }

    [TestCase(true, "Exporting...")]
    [TestCase(false, "Export")]
    public void ExportButton_ReflectsProcessingState(bool processing, string expected)
    {
        Services.ViewModel<ReportViewModel>().IsProcessing = processing;
        Assert.That(Render().FindAll("button").First(b => b.TextContent.Contains(expected)), Is.Not.Null);
    }

    [Test]
    public void TourWithNullValues_ShowsNAForMissingFields()
    {
        Services.ViewModel<TourViewModel>().Tours = [TestData.SampleTour(
            name: "Null Tour", description: "", from: "A", to: "B",
            distance: null, estimatedTime: null)];

        var markup = Render().Markup;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(markup, Does.Contain("N/A") & Does.Contain("meters"));
            Assert.That(markup, Does.Contain("N/A") & Does.Contain("minutes"));
        }
    }

    [TestCase(true, "Yes")]
    [TestCase(false, "No")]
    public void ChildFriendly_DisplaysCorrectText(bool isChildFriendly, string expected)
    {
        Services.ViewModel<TourViewModel>().Tours = [TestData.SampleTour(tourLogs: isChildFriendly
            ? [TestData.SampleTourLog(rating: 5, difficulty: 1)]
            : [TestData.SampleTourLog(rating: 1, difficulty: 5)])];

        Assert.That(Render().Markup, Does.Contain("Child Friendly:") & Does.Contain(expected));
    }

    [Test]
    public void TourWithNullAverageRating_ShowsNA()
    {
        Services.ViewModel<TourViewModel>().Tours = [TestData.SampleTour()];
        Assert.That(Render().Markup, Does.Contain("Average Rating:") & Does.Contain("N/A"));
    }

    [Test]
    public void TourWithRating_ShowsFormattedValue()
    {
        Services.ViewModel<TourViewModel>().Tours = [TestData.SampleTour(
            tourLogs: [TestData.SampleTourLog(rating: 4.5)])];

        Assert.That(Render().Markup, Does.Contain("4.5"));
    }

    [Test]
    public void RenderWithoutReportViewModel_OmitsExportButton()
    {
        var cut = RenderComponent<TourListComponent>(p => p
            .Add(static x => x.ViewModel, Services.ViewModel<TourViewModel>()));

        Assert.That(cut.FindAll("button.btn-export"), Has.Count.EqualTo(0));
    }
}
