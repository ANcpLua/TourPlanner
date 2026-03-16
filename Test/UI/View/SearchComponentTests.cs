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
    public void SearchInput_TypesText_UpdatesViewModel()
    {
        var cut = RenderComponent<SearchComponent>();
        cut.Find("input.search-input").Input("test");

        Assert.That(Services.ViewModel<SearchViewModel>().SearchText, Is.EqualTo("test"));
    }

    [Test]
    public async Task SearchInput_PressesEnter_TriggersSearch()
    {
        Services.Mock<IHttpService>().Setup(h => h.GetListAsync<Tour>("api/tour/search/test"))
            .ReturnsAsync(TestData.SampleTourList(0));
        Services.ViewModel<SearchViewModel>().SearchText = "test";
        var cut = RenderComponent<SearchComponent>();

        await cut.Find("input.search-input").KeyUpAsync(new KeyboardEventArgs { Key = "Enter" });

        Services.Mock<IHttpService>().Verify(h => h.GetListAsync<Tour>("api/tour/search/test"));
    }

    [Test]
    public async Task TourCard_Clicks_NavigatesToTour()
    {
        var tour = TestData.SampleTour();
        Services.ViewModel<SearchViewModel>().SearchResults = [tour];
        var cut = RenderComponent<SearchComponent>();

        await cut.Find("div.tour-search").ClickAsync(new MouseEventArgs());

        var nav = Services.GetRequiredService<NavigationManager>();
        Assert.That(nav.Uri, Does.EndWith($"/?tourId={tour.Id}"));
    }

    [Test]
    public async Task SearchButton_NoResults_ShowsNotification()
    {
        Services.Mock<IHttpService>().Setup(h => h.GetListAsync<Tour>("api/tour/search/test"))
            .ReturnsAsync(new List<Tour>());
        Services.ViewModel<SearchViewModel>().SearchText = "test";
        var cut = RenderComponent<SearchComponent>();

        await cut.Find("button.search-btn").ClickAsync(new MouseEventArgs());

        Services.Mock<IToastServiceWrapper>()
            .Verify(t => t.ShowSuccess("No tours found matching your search criteria."));
    }

    [Test]
    public void SearchResults_WhenEmpty_HidesResultsList()
    {
        Services.ViewModel<SearchViewModel>().SearchResults = [];
        var cut = RenderComponent<SearchComponent>();

        Assert.Throws<ElementNotFoundException>(() => cut.Find("div.tour-search"));
    }

    [Test]
    public void SearchResults_WhenPopulated_RendersCorrectNumberOfCards()
    {
        Services.ViewModel<SearchViewModel>().SearchResults = [..TestData.SampleTourList(3)];
        var cut = RenderComponent<SearchComponent>();

        Assert.That(cut.FindAll("div.tour-search"), Has.Count.EqualTo(3));
    }

    [Test]
    public void SearchResults_DisplaysTourName()
    {
        var tour = TestData.SampleTour("My Tour");
        Services.ViewModel<SearchViewModel>().SearchResults = [tour];
        var cut = RenderComponent<SearchComponent>();

        Assert.That(cut.Find("p.tour-name").TextContent, Is.EqualTo("My Tour"));
    }

    [Test]
    public void ClearButton_ClearsSearchTextAndResults()
    {
        Services.ViewModel<SearchViewModel>().SearchText = "test";
        Services.ViewModel<SearchViewModel>().SearchResults = [TestData.SampleTour()];
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
        var tour = TestData.SampleTour();
        tour.TourLogs = [TestData.SampleTourLog(tourId: tour.Id)];
        Services.ViewModel<SearchViewModel>().SearchResults = [tour];

        var cut = RenderComponent<SearchComponent>();

        Assert.That(cut.Find("div.tour-search").TextContent, Does.Contain("Latest Log:"));
    }

    [Test]
    public void TourWithoutLogs_HidesLatestLogSection()
    {
        var tour = TestData.SampleTour();
        tour.TourLogs.Clear();
        Services.ViewModel<SearchViewModel>().SearchResults = [tour];

        var cut = RenderComponent<SearchComponent>();

        Assert.That(cut.Find("div.tour-search").TextContent, Does.Not.Contain("Latest Log:"));
    }

    [Test]
    public void NoResultsText_ShownWhenSearchTextExistsButNoResults()
    {
        Services.ViewModel<SearchViewModel>().SearchText = "nonexistent";
        Services.ViewModel<SearchViewModel>().SearchResults = [];

        var cut = RenderComponent<SearchComponent>();

        Assert.That(cut.Find("p.text-center").TextContent, Is.EqualTo("No results found."));
    }

    [Test]
    public void NoResultsText_HiddenWhenSearchTextEmpty()
    {
        Services.ViewModel<SearchViewModel>().SearchText = "";
        Services.ViewModel<SearchViewModel>().SearchResults = [];

        var cut = RenderComponent<SearchComponent>();

        Assert.Throws<ElementNotFoundException>(() => cut.Find("p.text-center"));
    }
}