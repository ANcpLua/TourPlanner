using UI.Service.Interface;
using UI.ViewModel;

namespace Tests.UI.View;

[TestFixture]
[Parallelizable(ParallelScope.All)]
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public sealed class SearchComponentTests : BunitTestBase
{
    [Test]
    public void SearchInput_TypesText_UpdatesRenderedValue()
    {
        var cut = RenderComponent<SearchComponent>();
        cut.Find("input.search-input").Input("test");
        Assert.That(cut.Find("input.search-input").GetAttribute("value"), Is.EqualTo("test"));
    }

    [Test]
    public async Task SearchInput_PressesEnter_RendersResults()
    {
        Services.ViewModel<SearchViewModel>().SearchText = "test";
        var cut = RenderComponent<SearchComponent>();
        await cut.Find("input.search-input").KeyUpAsync(new KeyboardEventArgs { Key = "Enter" });
        cut.WaitForAssertion(() =>
            Assert.That(cut.FindAll("div.tour-search"), Has.Count.GreaterThan(0)));
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
    public void ClearButton_ResetsRenderedState()
    {
        Services.ViewModel<SearchViewModel>().SearchText = "test";
        Services.WithSearchResults();
        var cut = RenderComponent<SearchComponent>();
        cut.Find("button.clear-btn").Click();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(cut.Find("input.search-input").GetAttribute("value"), Is.Empty);
            Assert.Throws<ElementNotFoundException>(() => cut.Find("div.tour-search"));
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
