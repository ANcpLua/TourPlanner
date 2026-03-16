using UI.View.TourComponents;
using UI.ViewModel;

namespace Test.UI.View;

[TestFixture]
[Parallelizable(ParallelScope.All)]
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public sealed class TourDetailsModalTests : BunitTestBase
{
    protected override void OnSetup() => Services.WithModalTour();

    [Test]
    public void DisplaysTourDetails()
    {
        var body = RenderComponent<TourDetailsModal>(p =>
            p.Add(x => x.TourViewModel, Services.ViewModel<TourViewModel>())).Find(".modal-body");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(body.TextContent, Does.Contain("Test Tour"));
            Assert.That(body.TextContent, Does.Contain("City1"));
            Assert.That(body.TextContent, Does.Contain("City2"));
        }
    }

    [Test]
    public void HandlesNullGracefully()
    {
        Services.WithMinimalModalTour();
        Assert.DoesNotThrow(() => RenderComponent<TourDetailsModal>(p =>
            p.Add(x => x.TourViewModel, Services.ViewModel<TourViewModel>())).Find(".modal-body"));
    }
}
