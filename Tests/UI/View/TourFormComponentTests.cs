using UI.Service.Interface;
using UI.View.TourComponents;
using UI.ViewModel;

namespace Tests.UI.View;

[TestFixture]
[Parallelizable(ParallelScope.All)]
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public sealed class TourFormComponentTests : BunitTestBase
{
    protected override void OnSetup() => Services.WithValidTourForm();

    private IRenderedComponent<TourFormComponent> Render() =>
        RenderComponent<TourFormComponent>(p => p
            .Add(x => x.ViewModel, Services.ViewModel<TourViewModel>())
            .Add(x => x.MapViewModel, Services.ViewModel<MapViewModel>()));

    [Test]
    public void RendersAllFields()
    {
        var cut = Render();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(cut.Find("#name"), Is.Not.Null);
            Assert.That(cut.Find("#description"), Is.Not.Null);
            Assert.That(cut.Find("#from"), Is.Not.Null);
            Assert.That(cut.Find("#to"), Is.Not.Null);
            Assert.That(cut.Find("#transportType"), Is.Not.Null);
        }
    }

    [Test]
    public void DisablesSaveWhenInvalid()
    {
        Services.WithEmptyTourForm();
        Assert.That(Render().Find(".btn-primary").HasAttribute("disabled"), Is.True);
    }

    [TestCase(true, "Saving...")]
    [TestCase(false, "Save Tour")]
    public void SaveButtonShowsProcessingState(bool isProcessing, string expected)
    {
        Services.ViewModel<TourViewModel>().IsProcessing = isProcessing;
        Assert.That(Render().Find(".btn-primary").TextContent, Is.EqualTo(expected));
    }

    [Test]
    public async Task SaveTourSuccessfully()
    {
        Services.ViewModel<TourViewModel>().SelectedTour.Id = Guid.Empty;
        Services.SetupMockRouteData();
        Services.SetupMockPostTour();
        await Render().Find(".btn-primary").ClickAsync(new MouseEventArgs());
        Services.VerifyMockPostTour(Times.Once());
    }
}
