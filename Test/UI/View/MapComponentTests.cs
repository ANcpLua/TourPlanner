using UI.Service.Interface;
using UI.ViewModel;

namespace Test.UI.View;

[TestFixture]
[Parallelizable(ParallelScope.All)]
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public sealed class MapComponentTests : BunitTestBase
{
    private IRenderedComponent<MapComponent> Render() =>
        RenderComponent<MapComponent>(p => p.Add(x => x.ViewModel, Services.ViewModel<MapViewModel>()));

    [Test]
    public void RendersAllCityOptions()
    {
        var cut = Render();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(cut.FindAll("#fromCity option"), Has.Count.EqualTo(6));
            Assert.That(cut.FindAll("#toCity option"), Has.Count.EqualTo(6));
        }
    }

    [Test]
    public void FiltersDestinationWhenOriginSelected()
    {
        Services.ViewModel<MapViewModel>().FromCity = "Vienna";
        Assert.That(Render().FindAll("#toCity option").Select(o => o.TextContent), Does.Not.Contain("Vienna"));
    }

    [Test]
    public async Task UpdateMapRequiresBothCities()
    {
        await Services.ViewModel<MapViewModel>().InitializeMapAsync(new ElementReference());
        await Render().Find("button.btn-primary").ClickAsync(new MouseEventArgs());
        Services.Mock<IToastServiceWrapper>().Verify(t => t.ShowError("Please select both cities."), Times.Once);
    }

    [Test]
    public async Task ResetClearsSelections()
    {
        var vm = Services.ViewModel<MapViewModel>();
        vm.FromCity = "Vienna";
        vm.ToCity = "Paris";
        await Render().Find("button.btn-secondary").ClickAsync(new MouseEventArgs());
        using (Assert.EnterMultipleScope())
        {
            Assert.That(vm.FromCity, Is.Empty);
            Assert.That(vm.ToCity, Is.Empty);
        }
    }
}
