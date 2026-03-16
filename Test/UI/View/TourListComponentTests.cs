using UI.Service.Interface;
using UI.View.TourComponents;
using UI.ViewModel;

namespace Test.UI.View;

[TestFixture]
[Parallelizable(ParallelScope.All)]
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public sealed class TourListComponentTests : BunitTestBase
{
    protected override void OnSetup() => Services.WithTours(2);

    private IRenderedComponent<TourListComponent> Render() =>
        RenderComponent<TourListComponent>(p => p
            .Add(x => x.ViewModel, Services.ViewModel<TourViewModel>())
            .Add(x => x.ReportViewModel, Services.ViewModel<ReportViewModel>()));

    [Test]
    public void RendersTourCardPerTour() =>
        Assert.That(Render().FindAll("div.tour-card"), Has.Count.EqualTo(2));

    [Test]
    public void EmptyTourList_ShowsMessage()
    {
        Services.ViewModel<TourViewModel>().Tours.Clear();
        Assert.That(Render().Markup, Does.Contain("No tours available"));
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task DeleteTour_RespectsConfirmation(bool confirms)
    {
        var id = Services.FirstTourId();
        JSInterop.Setup<bool>("confirm", _ => true).SetResult(confirms);
        if (confirms) Services.SetupMockDeleteTour(id);
        await Render().Find("button.btn-danger").ClickAsync(new MouseEventArgs());
        Services.VerifyMockDeleteTour(id, confirms ? Times.Once() : Times.Never());
    }

    [Test]
    public void EditButton_WhenEditingThisTour_ShowsHideText()
    {
        Services.ViewModel<TourViewModel>().IsFormVisible = true;
        Services.ViewModel<TourViewModel>().SelectedTour = Services.ViewModel<TourViewModel>().Tours.First();
        Assert.That(Render().FindAll("button").First(b => b.TextContent.Contains("Hide Edit Form")), Is.Not.Null);
    }

    [Test]
    public void EditButton_WhenNotEditing_ShowsEditText()
    {
        Services.ViewModel<TourViewModel>().IsFormVisible = false;
        Assert.That(Render().FindAll("button").First(b => b.TextContent.Trim() == "Edit"), Is.Not.Null);
    }

    [TestCase(true, "Exporting...")]
    [TestCase(false, "Export")]
    public void ExportButton_ReflectsProcessingState(bool processing, string expected)
    {
        Services.ViewModel<ReportViewModel>().IsProcessing = processing;
        Assert.That(Render().FindAll("button").First(b => b.TextContent.Contains(expected)), Is.Not.Null);
    }
}
