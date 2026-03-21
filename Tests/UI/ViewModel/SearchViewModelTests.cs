using UI.Model;
using UI.Service.Interface;
using UI.ViewModel;

namespace Tests.UI.ViewModel;

[TestFixture]
public class SearchViewModelTests
{
    [SetUp]
    public void Setup()
    {
        var (client, handler) = TestData.MockedHttpClient();
        _httpClient = client;
        _mockHandler = handler;
        _mockToastService = TestData.MockToastService();
        _mockNavigationManager = new TestNavigationManager();
        _viewModel = new SearchViewModel(
            _httpClient, _mockToastService.Object,
            TestData.MockTryCatchToastWrapper(), _mockNavigationManager);
    }

    private HttpClient _httpClient = null!;
    private Mock<HttpMessageHandler> _mockHandler = null!;
    private Mock<IToastServiceWrapper> _mockToastService = null!;
    private TestNavigationManager _mockNavigationManager = null!;
    private SearchViewModel _viewModel = null!;

    private sealed class TestNavigationManager : NavigationManager
    {
        public string? LastUri;

        public TestNavigationManager()
        {
            Initialize("http://localhost/", "http://localhost/");
        }

        protected override void NavigateToCore(string uri, bool forceLoad)
        {
            LastUri = uri;
        }
    }

    [Test]
    public void Constructor_DefaultValues()
    {
        Assert.That(_viewModel.SearchResults, Is.Empty);
    }

    [Test]
    public async Task SearchToursAsync_EmptyText_ClearsResults()
    {
        _viewModel.SearchResults = new ObservableCollection<Tour>(TestData.SampleTourList());
        _viewModel.SearchText = "   ";
        await _viewModel.SearchToursAsync();

        Assert.That(_viewModel.SearchResults, Is.Empty);
    }

    [Test]
    public async Task SearchToursAsync_NoHits_ShowsToast()
    {
        _viewModel.SearchText = "xyz";
        TestData.SetupHandler(_mockHandler, HttpMethod.Get, "api/tour/search/xyz", "[]");

        await _viewModel.SearchToursAsync();

        _mockToastService.Verify(static t => t.ShowSuccess("No tours found matching your search criteria."), Times.Once);
    }

    [Test]
    public async Task SearchToursAsync_Hits_FillsResults()
    {
        var tours = TestData.SampleTourList();
        TestData.SetupHandler(_mockHandler, HttpMethod.Get, "api/tour/search/a", tours);

        _viewModel.SearchText = "a";
        await _viewModel.SearchToursAsync();

        Assert.That(_viewModel.SearchResults, Has.Count.EqualTo(tours.Count));
    }

    [Test]
    public void ClearSearch_ResetsEverything()
    {
        _viewModel.SearchText = "abc";
        _viewModel.SearchResults = new ObservableCollection<Tour>(TestData.SampleTourList());

        _viewModel.ClearSearch();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_viewModel.SearchText, Is.Empty);
            Assert.That(_viewModel.SearchResults, Is.Empty);
        }
    }

    [Test]
    public void NavigateToTour_NavigatesWithQuery()
    {
        var id = Guid.NewGuid();
        _viewModel.NavigateToTour(id);

        Assert.That(_mockNavigationManager.LastUri, Is.EqualTo($"/?tourId={id}"));
    }

    [Test]
    public async Task HandleKeyPress_Enter_PerformsSearch()
    {
        _viewModel.SearchText = "foo";
        TestData.SetupHandler(_mockHandler, HttpMethod.Get, "api/tour/search/foo", "[]");

        await _viewModel.HandleKeyPress(new KeyboardEventArgs { Key = "Enter" });

        TestData.VerifyHandler(_mockHandler, HttpMethod.Get, "api/tour/search/foo", Times.Once());
    }

    [Test]
    public async Task HandleKeyPress_OtherKey_NoSearch()
    {
        await _viewModel.HandleKeyPress(new KeyboardEventArgs { Key = "X" });
        TestData.VerifyHandler(_mockHandler, HttpMethod.Get, "api/tour/search/", Times.Never());
    }
}
