using UI.Model;
using UI.Service.Interface;
using UI.ViewModel;

namespace Test.UI.View;

[TestFixture]
[Parallelizable(ParallelScope.All)]
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public sealed class SearchComponentTests : BunitTestBase
{
    [Test]
    public void BindsSearchText()
    {
        var cut = RenderComponent<SearchComponent>();
        cut.Find("input.search-input").Input("test");

        Assert.That(Services.ViewModel<SearchViewModel>().SearchText, Is.EqualTo("test"));
    }

    [Test]
    public async Task EnterKeyTriggersSearch()
    {
        Services.Mock<IHttpService>().Setup(h => h.GetListAsync<Tour>("api/tour/search/test"))
            .ReturnsAsync(TestData.SampleTourList(0));

        var cut = RenderComponent<SearchComponent>();
        Services.ViewModel<SearchViewModel>().SearchText = "test";

        await cut.Find("input.search-input").KeyUpAsync(new KeyboardEventArgs { Key = "Enter" });

        Services.Mock<IHttpService>().Verify(h => h.GetListAsync<Tour>("api/tour/search/test"));
    }

    [Test]
    public async Task SearchResultsNavigateOnClick()
    {
        var tour = TestData.SampleTour();
        Services.ViewModel<SearchViewModel>().SearchResults = [tour];

        var cut = RenderComponent<SearchComponent>();
        await cut.Find("div.tour-search").ClickAsync(new MouseEventArgs());

        var nav = Services.GetRequiredService<NavigationManager>();
        Assert.That(nav.Uri, Does.EndWith($"/?tourId={tour.Id}"));
    }

    [Test]
    public async Task EmptySearchShowsNotification()
    {
        Services.Mock<IHttpService>().Setup(h => h.GetListAsync<Tour>("api/tour/search/test"))
            .ReturnsAsync(new List<Tour>());

        Services.ViewModel<SearchViewModel>().SearchText = "test";
        var cut = RenderComponent<SearchComponent>();

        await cut.Find("button.search-btn").ClickAsync(new MouseEventArgs());

        Services.Mock<IToastServiceWrapper>()
            .Verify(t => t.ShowSuccess("No tours found matching your search criteria."));
    }
}