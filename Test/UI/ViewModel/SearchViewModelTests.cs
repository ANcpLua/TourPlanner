using System.Collections.ObjectModel;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moq;
using Serilog;
using UI.Model;
using UI.Service.Interface;
using UI.ViewModel;

namespace Test.UI.ViewModel;

[TestFixture]
public class SearchViewModelTests
{
    [SetUp]
    public void Setup()
    {
        _mockHttpService = TestData.MockHttpService();
        _mockToastService = TestData.MockToastService();
        _mockLogger = TestData.MockLogger();
        _mockNavigationManager = new TestNavigationManager();
        _viewModel = new SearchViewModel(_mockHttpService.Object, _mockToastService.Object, _mockLogger.Object,
            _mockNavigationManager);
    }

    private class TestNavigationManager : NavigationManager
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

    private Mock<IHttpService> _mockHttpService = null!;
    private Mock<IToastServiceWrapper> _mockToastService = null!;
    private Mock<ILogger> _mockLogger = null!;
    private TestNavigationManager _mockNavigationManager = null!;
    private SearchViewModel _viewModel = null!;

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
        _mockHttpService.Setup(h => h.GetListAsync<Tour>(It.IsAny<string>()))
            .ReturnsAsync((IEnumerable<Tour>?)null);

        await _viewModel.SearchToursAsync();

        _mockToastService.Verify(t => t.ShowSuccess("No tours found matching your search criteria."), Times.Once);
    }

    [Test]
    public async Task SearchToursAsync_Hits_FillsResults()
    {
        var tours = TestData.SampleTourList();
        _mockHttpService.Setup(h => h.GetListAsync<Tour>("api/tour/search/a")).ReturnsAsync(tours);

        _viewModel.SearchText = "a";
        await _viewModel.SearchToursAsync();

        Assert.That(_viewModel.SearchResults, Is.EquivalentTo(tours));
    }

    [Test]
    public void ClearSearch_ResetsEverything()
    {
        _viewModel.SearchText = "abc";
        _viewModel.SearchResults = new ObservableCollection<Tour>(TestData.SampleTourList());

        _viewModel.ClearSearch();

        Assert.Multiple(() =>
        {
            Assert.That(_viewModel.SearchText, Is.Empty);
            Assert.That(_viewModel.SearchResults, Is.Empty);
        });
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
        await _viewModel.HandleKeyPress(new KeyboardEventArgs { Key = "Enter" });

        _mockHttpService.Verify(h => h.GetListAsync<Tour>("api/tour/search/foo"), Times.Once);
    }

    [Test]
    public async Task HandleKeyPress_OtherKey_NoSearch()
    {
        await _viewModel.HandleKeyPress(new KeyboardEventArgs { Key = "X" });
        _mockHttpService.VerifyNoOtherCalls();
    }
}