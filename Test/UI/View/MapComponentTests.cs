using UI.Service.Interface;
using UI.ViewModel;

namespace Test.UI.View;

[TestFixture]
[Parallelizable(ParallelScope.All)]
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public sealed class MapComponentTests : BunitTestBase
{
    [Test]
    public void RendersAllCityOptions()
    {
        var vm = Services.ViewModel<MapViewModel>();
        var cut = RenderComponent<MapComponent>(p => p.Add(x => x.ViewModel, vm));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(cut.FindAll("#fromCity option"), Has.Count.EqualTo(6));
            Assert.That(cut.FindAll("#toCity option"), Has.Count.EqualTo(6));
        }
    }

    [Test]
    public void FiltersDestinationWhenOriginSelected()
    {
        var vm = Services.ViewModel<MapViewModel>();
        vm.FromCity = "Vienna";

        var cut = RenderComponent<MapComponent>(p => p.Add(x => x.ViewModel, vm));
        var toOptions = cut.FindAll("#toCity option").Select(o => o.TextContent);

        Assert.That(toOptions, Does.Not.Contain("Vienna"));
    }

    [Test]
    public async Task UpdateMapRequiresBothCities()
    {
        var vm = Services.ViewModel<MapViewModel>();
        Services.Mock<IToastServiceWrapper>().Setup(t => t.ShowError("Please select both cities."));

        await vm.InitializeMapAsync(new ElementReference());
        var cut = RenderComponent<MapComponent>(p => p.Add(x => x.ViewModel, vm));

        await cut.Find("button.btn-primary").ClickAsync(new MouseEventArgs());

        Services.Mock<IToastServiceWrapper>().Verify(t => t.ShowError("Please select both cities."), Times.Once);
    }

    [Test]
    public async Task ResetClearsSelections()
    {
        var vm = Services.ViewModel<MapViewModel>();
        vm.FromCity = "Vienna";
        vm.ToCity = "Paris";

        var cut = RenderComponent<MapComponent>(p => p.Add(x => x.ViewModel, vm));
        await cut.Find("button.btn-secondary").ClickAsync(new MouseEventArgs());
        using (Assert.EnterMultipleScope())
        {
            Assert.That(vm.FromCity, Is.Empty);
            Assert.That(vm.ToCity, Is.Empty);
        }
    }
}