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

    [TestCase(1d, 4d, "Yes")]
    [TestCase(2d, 3d, "Yes")]
    [TestCase(2d, 4d, "Yes")]
    [TestCase(3d, 4d, "No")]
    [TestCase(1d, 2d, "No")]
    public void TourWithLog_HasDifficultyAndRating_DisplaysChildFriendlyStatus(double difficulty, double rating,
        string expectedStatus)
    {
        var tour = TestData.SampleTour();
        tour.TourLogs.Clear();
        tour.TourLogs.Add(new TourLog
        {
            TourId = tour.Id, Difficulty = difficulty, Rating = rating, DateTime = DateTime.Now, Comment = "Test log",
            TotalDistance = 10, TotalTime = 60
        });
        Services.ViewModel<SearchViewModel>().SearchResults = [tour];

        var cut = RenderComponent<SearchComponent>();

        Assert.That(cut.Find("div.tour-search").TextContent, Does.Contain($"Child Friendly: {expectedStatus}"));
    }

    [Test]
    public void TourWithoutLogs_HasEmptyCollection_DisplaysNotChildFriendly()
    {
        var tour = TestData.SampleTour();
        tour.TourLogs.Clear();
        Services.ViewModel<SearchViewModel>().SearchResults = [tour];

        var cut = RenderComponent<SearchComponent>();

        Assert.That(cut.Find("div.tour-search").TextContent, Does.Contain("Child Friendly: No"));
    }

    [TestCase("Beautiful scenic route", "Beautiful scenic route")]
    [TestCase("", "N/A")]
    public void TourDescription_HasValue_DisplaysCorrectly(string description, string expectedText)
    {
        var tour = TestData.SampleTour();
        tour.Description = description;
        Services.ViewModel<SearchViewModel>().SearchResults = [tour];

        var cut = RenderComponent<SearchComponent>();

        Assert.That(cut.Find("div.tour-search").TextContent, Does.Contain($"Description: {expectedText}"));
    }

    [Test]
    public void TourDescription_IsNull_DisplaysNA()
    {
        var tour = TestData.SampleTour();
        tour.Description = null;
        Services.ViewModel<SearchViewModel>().SearchResults = [tour];

        var cut = RenderComponent<SearchComponent>();

        Assert.That(cut.Find("div.tour-search").TextContent, Does.Contain("Description: N/A"));
    }

    [TestCase("/images/tour.jpg", "Available")]
    [TestCase("", "N/A")]
    public void TourImagePath_HasValue_DisplaysCorrectly(string imagePath, string expectedText)
    {
        var tour = TestData.SampleTour();
        tour.ImagePath = imagePath;
        Services.ViewModel<SearchViewModel>().SearchResults = [tour];

        var cut = RenderComponent<SearchComponent>();

        Assert.That(cut.Find("div.tour-search").TextContent, Does.Contain($"Image: {expectedText}"));
    }

    [Test]
    public void TourImagePath_IsNull_DisplaysNA()
    {
        var tour = TestData.SampleTour();
        tour.ImagePath = null;
        Services.ViewModel<SearchViewModel>().SearchResults = [tour];

        var cut = RenderComponent<SearchComponent>();

        Assert.That(cut.Find("div.tour-search").TextContent, Does.Contain("Image: N/A"));
    }

    [Test]
    public void TourWithMultipleLogs_HasLatestFirst_DisplaysLatestLogInfo()
    {
        var tour = TestData.SampleTour();
        tour.TourLogs.Clear();

        var olderLog = TestData.SampleTourLog(3, 1, tour.Id);
        olderLog.DateTime = DateTime.Now.AddDays(-5);

        var newerLog = TestData.SampleTourLog(5, 1, tour.Id);
        newerLog.DateTime = DateTime.Now.AddDays(-1);

        tour.TourLogs.Add(olderLog);
        tour.TourLogs.Add(newerLog);

        Services.ViewModel<SearchViewModel>().SearchResults = [tour];

        var cut = RenderComponent<SearchComponent>();

        var tourCard = cut.Find("div.tour-search");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(tourCard.TextContent, Does.Contain("Latest Log:"));
            Assert.That(tourCard.TextContent, Does.Contain(newerLog.DateTime.ToShortDateString()));
            Assert.That(tourCard.TextContent, Does.Contain("Log Rating: 5"));
        }
    }

    [Test]
    public void TourWithoutLogs_HasEmptyCollection_HidesLogInfo()
    {
        var tour = TestData.SampleTour();
        tour.TourLogs.Clear();
        Services.ViewModel<SearchViewModel>().SearchResults = [tour];

        var cut = RenderComponent<SearchComponent>();

        var tourCard = cut.Find("div.tour-search");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(tourCard.TextContent, Does.Not.Contain("Latest Log:"));
            Assert.That(tourCard.TextContent, Does.Not.Contain("Log Rating:"));
        }
    }
}