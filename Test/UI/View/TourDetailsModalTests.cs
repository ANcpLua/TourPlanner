using UI.Model;
using UI.View.TourComponents;
using UI.ViewModel;

namespace Test.UI.View;

[TestFixture]
[Parallelizable(ParallelScope.All)]
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public sealed class TourDetailsModalTests : BunitTestBase
{
    protected override void OnSetup()
    {
        Services.ViewModel<TourViewModel>().ModalTour = TestData.SampleTour("Test Tour");
    }

    [Test]
    public void DisplaysTourDetails()
    {
        var cut = RenderComponent<TourDetailsModal>(p =>
            p.Add(x => x.TourViewModel, Services.ViewModel<TourViewModel>()));

        var body = cut.Find(".modal-body");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(body.TextContent, Does.Contain("Test Tour"));
            Assert.That(body.TextContent, Does.Contain("City1"));
            Assert.That(body.TextContent, Does.Contain("City2"));
            Assert.That(body.TextContent, Does.Match(@"100[.,]5 meters"));
        }
    }

    [Test]
    public void HandlesNullGracefully()
    {
        Services.ViewModel<TourViewModel>().ModalTour = new Tour
        {
            Name = "Tour",
            From = "A",
            To = "B",
            TransportType = "Walk"
        };

        var cut = RenderComponent<TourDetailsModal>(p =>
            p.Add(x => x.TourViewModel, Services.ViewModel<TourViewModel>()));

        Assert.DoesNotThrow(() => cut.Find(".modal-body"));
    }
}