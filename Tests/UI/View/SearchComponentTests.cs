using UI.Service.Interface;
using UI.ViewModel;

namespace Test.UI.View;

[TestFixture]
[Parallelizable(ParallelScope.All)]
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public sealed class SearchComponentTests : BunitTestBase
{
    [Test]
    public void SearchInput_TypesText_UpdatesViewModel()
    {
        var cut = RenderComponent<SearchComponent>();
        cut.Find("input.search-input").Input("test");
        Assert.That(Services.ViewModel<SearchViewModel>().SearchText, Is.EqualTo("test"));
    }

    [Test]
    public async Task SearchInput_PressesEnter_ExecutesSearch()
    {
        Services.ViewModel<SearchViewModel>().SearchText = "test";
        var cut = RenderComponent<SearchComponent>();
        await cut.Find("input.search-input").KeyUpAsync(new KeyboardEventArgs { Key = "Enter" });
        Assert.That(Services.ViewModel<SearchViewModel>().SearchResults, Is.Not.Null);
    }

    [Test]
    public async Task TourCard_Clicks_NavigatesToTour()
    {
        Services.WithSearchResults();
        var id = Services.FirstSearchResultId();
        var cut = RenderComponent<SearchComponent>();
        await cut.Find("div.tour-search").ClickAsync(new MouseEventArgs());
        Assert.That(Services.GetRequiredService<NavigationManager>().Uri, Does.EndWith($"/?tourId={id}"));
    }

    [Test]
    public void SearchResults_WhenPopulated_RendersCards()
    {
        Services.WithSearchResults(3);
        Assert.That(RenderComponent<SearchComponent>().FindAll("div.tour-search"), Has.Count.EqualTo(3));
    }

    [Test]
    public void SearchResults_WhenEmpty_HidesCards()
    {
        Assert.Throws<ElementNotFoundException>(() => RenderComponent<SearchComponent>().Find("div.tour-search"));
    }

    [Test]
    public void ClearButton_ResetsState()
    {
        Services.ViewModel<SearchViewModel>().SearchText = "test";
        Services.WithSearchResults();
        var cut = RenderComponent<SearchComponent>();
        cut.Find("button.clear-btn").Click();
        var vm = Services.ViewModel<SearchViewModel>();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(vm.SearchText, Is.Empty);
            Assert.That(vm.SearchResults, Is.Empty);
        }
    }

    [Test]
    public void TourWithLogs_ShowsLatestLogSection()
    {
        Services.WithSearchResultWithLogs();
        Assert.That(RenderComponent<SearchComponent>().Find("div.tour-search").TextContent, Does.Contain("Latest Log:"));
    }

    [Test]
    public void TourWithoutLogs_HidesLatestLogSection()
    {
        Services.WithSearchResultWithoutLogs();
        Assert.That(RenderComponent<SearchComponent>().Find("div.tour-search").TextContent, Does.Not.Contain("Latest Log:"));
    }
}
