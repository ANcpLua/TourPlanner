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

    private IRenderedComponent<TourListComponent> Render()
    {
        return RenderComponent<TourListComponent>(p => p
            .Add(x => x.ViewModel, Services.ViewModel<TourViewModel>())
            .Add(x => x.ReportViewModel, Services.ViewModel<ReportViewModel>()));
    }

    [Test]
    public void RendersTourCardPerTour()
    {
        Assert.That(Render().FindAll("div.tour-card"), Has.Count.EqualTo(2));
    }

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
        var tour = Services.ViewModel<TourViewModel>().Tours.First();
        JSInterop.Setup<bool>("confirm", _ => true).SetResult(confirms);
        if (confirms)
            Services.Mock<IHttpService>().Setup(s => s.DeleteAsync($"api/tour/{tour.Id}"))
                .Returns(Task.CompletedTask);

        await Render().Find("button.btn-danger").ClickAsync(new MouseEventArgs());

        Services.Mock<IHttpService>().Verify(
            s => s.DeleteAsync($"api/tour/{tour.Id}"),
            confirms ? Times.Once : Times.Never);
    }

    [Test]
    public void EditButton_WhenEditingThisTour_ShowsHideText()
    {
        var tour = Services.ViewModel<TourViewModel>().Tours.First();
        Services.ViewModel<TourViewModel>().IsFormVisible = true;
        Services.ViewModel<TourViewModel>().SelectedTour = tour;

        var editBtn = Render().FindAll("button").First(b => b.TextContent.Contains("Hide Edit Form"));
        Assert.That(editBtn, Is.Not.Null);
    }

    [Test]
    public void EditButton_WhenNotEditing_ShowsEditText()
    {
        Services.ViewModel<TourViewModel>().IsFormVisible = false;

        var editBtn = Render().FindAll("button").First(b => b.TextContent.Trim() == "Edit");
        Assert.That(editBtn, Is.Not.Null);
    }

    [TestCase(true, "Exporting...")]
    [TestCase(false, "Export")]
    public void ExportButton_ReflectsProcessingState(bool processing, string expected)
    {
        Services.ViewModel<ReportViewModel>().IsProcessing = processing;

        var btn = Render().FindAll("button").First(b => b.TextContent.Contains(expected));
        Assert.That(btn, Is.Not.Null);
    }
}