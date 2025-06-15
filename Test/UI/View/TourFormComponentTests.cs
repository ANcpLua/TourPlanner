using UI.Model;
using UI.Service.Interface;
using UI.View.TourComponents;
using UI.ViewModel;

namespace Test.UI.View;

[TestFixture]
[Parallelizable(ParallelScope.All)]
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public sealed class TourFormComponentTests : BunitTestBase
{
    protected override void OnSetup()
    {
        var tour = TestData.SampleTour();
        tour.Name = "Valid Tour";
        tour.From = "Vienna";
        tour.To = "Paris";
        tour.TransportType = "Car";
        Services.ViewModel<TourViewModel>().SelectedTour = tour;
    }

    [Test]
    public void RendersAllFields()
    {
        var cut = RenderComponent<TourFormComponent>(p => p
            .Add(x => x.ViewModel, Services.ViewModel<TourViewModel>())
            .Add(x => x.MapViewModel, Services.ViewModel<MapViewModel>()));
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
        Services.ViewModel<TourViewModel>().SelectedTour = new Tour { Name = "" };

        var cut = RenderComponent<TourFormComponent>(p => p
            .Add(x => x.ViewModel, Services.ViewModel<TourViewModel>())
            .Add(x => x.MapViewModel, Services.ViewModel<MapViewModel>()));

        Assert.That(cut.Find(".btn-primary").HasAttribute("disabled"), Is.True);
    }

    [TestCase(true, "Saving...")]
    [TestCase(false, "Save Tour")]
    public void SaveButtonShowsProcessingState(bool isProcessing, string expected)
    {
        Services.ViewModel<TourViewModel>().IsProcessing = isProcessing;

        var cut = RenderComponent<TourFormComponent>(p => p
            .Add(x => x.ViewModel, Services.ViewModel<TourViewModel>())
            .Add(x => x.MapViewModel, Services.ViewModel<MapViewModel>()));

        Assert.That(cut.Find(".btn-primary").TextContent, Is.EqualTo(expected));
    }

    [Test]
    public async Task SaveTourSuccessfully()
    {
        Services.ViewModel<TourViewModel>().SelectedTour.Id = Guid.Empty;
        Services.Mock<IRouteApiService>().Setup(s => s.FetchRouteDataAsync(
                It.IsAny<(double, double)>(), It.IsAny<(double, double)>(), It.IsAny<string>()))
            .ReturnsAsync((100.5, 60.5));
        Services.Mock<IHttpService>().Setup(s => s.PostAsync<Tour>("api/tour", It.IsAny<Tour>()))
            .ReturnsAsync(TestData.SampleTour());

        var cut = RenderComponent<TourFormComponent>(p => p
            .Add(x => x.ViewModel, Services.ViewModel<TourViewModel>())
            .Add(x => x.MapViewModel, Services.ViewModel<MapViewModel>()));

        await cut.Find(".btn-primary").ClickAsync(new MouseEventArgs());

        Services.Mock<IHttpService>().Verify(s => s.PostAsync<Tour>("api/tour", It.IsAny<Tour>()), Times.Once);
    }
}