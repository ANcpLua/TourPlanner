using UI.Service.Interface;
using UI.ViewModel;

namespace Test.UI.ViewModel;

[TestFixture]
public class MapViewModelTests
{
    private Mock<IJSRuntime> _mockJsRuntime = null!;
    private Mock<IHttpService> _mockHttpService = null!;
    private Mock<IToastServiceWrapper> _mockToastService = null!;
    private Mock<ILogger> _mockLogger = null!;
    private MapViewModel _viewModel = null!;

    [SetUp]
    public void Setup()
    {
        _mockJsRuntime = TestData.MockJsRuntime();
        _mockHttpService = TestData.MockHttpService();
        _mockToastService = TestData.MockToastService();
        _mockLogger = TestData.MockLogger();

        _viewModel = new MapViewModel(
            _mockJsRuntime.Object,
            _mockHttpService.Object,
            _mockToastService.Object,
            _mockLogger.Object
        );
    }

    [Test]
    public void Constructor_ShouldInitializePropertiesCorrectly()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_viewModel.CityNames, Is.Not.Null);
            Assert.That(_viewModel.CityNames, Has.Count.EqualTo(MapViewModel.Coordinates.Count));
            Assert.That(_viewModel.FromCity, Is.Empty);
            Assert.That(_viewModel.ToCity, Is.Empty);
        }
    }

    [TestCase("Vienna")]
    [TestCase("Berlin")]
    [TestCase("Paris")]
    public void FromCity_WhenSet_ShouldUpdateFilteredToCities(string selectedCity)
    {
        _viewModel.FromCity = selectedCity;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_viewModel.FromCity, Is.EqualTo(selectedCity));
            Assert.That(_viewModel.FilteredToCities, Does.Not.Contain(selectedCity));
            Assert.That(_viewModel.FilteredToCities.Count(), Is.EqualTo(_viewModel.CityNames.Count - 1));
        }
    }

    [Test]
    public void FromCity_WhenSetSameAsToCity_ShouldClearToCity()
    {
        const string city = "Vienna";
        _viewModel.ToCity = city;

        _viewModel.FromCity = city;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_viewModel.FromCity, Is.EqualTo(city));
            Assert.That(_viewModel.ToCity, Is.Empty);
        }
    }

    [Test]
    public void ToCity_WhenSet_ShouldUpdateProperty()
    {
        const string city = "Berlin";

        _viewModel.ToCity = city;

        Assert.That(_viewModel.ToCity, Is.EqualTo(city));
    }

    [Test]
    public void FilteredToCities_ShouldExcludeFromCity()
    {
        _viewModel.FromCity = "Vienna";

        var filteredCities = _viewModel.FilteredToCities.ToList();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(filteredCities, Does.Not.Contain("Vienna"));
            Assert.That(filteredCities, Contains.Item("Berlin"));
            Assert.That(filteredCities, Contains.Item("Paris"));
        }
    }

    [TestCase("Vienna", 48.2082, 16.3738)]
    [TestCase("Berlin", 52.5200, 13.4050)]
    [TestCase("Paris", 48.8566, 2.3522)]
    public void GetCoordinates_WithValidCity_ShouldReturnCorrectCoordinates(string city, double expectedLat,
        double expectedLng)
    {
        var result = _viewModel.GetCoordinates(city);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Value.Latitude, Is.EqualTo(expectedLat));
            Assert.That(result.Value.Longitude, Is.EqualTo(expectedLng));
        }
    }

    [TestCase("InvalidCity")]
    [TestCase("")]
    public void GetCoordinates_WithInvalidCity_ShouldReturnNull(string? city)
    {
        var result = _viewModel.GetCoordinates(city!);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task ShowMapAsync_WhenMapNotInitialized_ShouldShowError()
    {
        _viewModel.FromCity = "Vienna";
        _viewModel.ToCity = "Berlin";

        await _viewModel.ShowMapAsync();

        _mockToastService.Verify(
            t => t.ShowError("Map is not initialized yet."),
            Times.Once
        );
    }

    [TestCase("", "Berlin", "Please select both cities.")]
    [TestCase("Vienna", "", "Please select both cities.")]
    [TestCase("   ", "Berlin", "Please select both cities.")]
    [TestCase("Vienna", "   ", "Please select both cities.")]
    public async Task ShowMapAsync_WithInvalidCitySelection_ShouldShowError(string fromCity, string toCity,
        string expectedError)
    {
        await _viewModel.InitializeMapAsync(new ElementReference());
        _viewModel.FromCity = fromCity;
        _viewModel.ToCity = toCity;

        await _viewModel.ShowMapAsync();

        _mockToastService.Verify(
            t => t.ShowError(expectedError),
            Times.Once
        );
    }

    [Test]
    public async Task ShowMapAsync_WithValidCities_ShouldDisplayRoute()
    {
        _mockJsRuntime
            .Setup(js => js.InvokeAsync<IJSVoidResult>(
                "TourPlannerMap.setRoute",
                It.IsAny<object[]>()))
            .ReturnsAsync(Mock.Of<IJSVoidResult>());

        await _viewModel.InitializeMapAsync(new ElementReference());
        _viewModel.FromCity = "Vienna";
        _viewModel.ToCity = "Berlin";

        await _viewModel.ShowMapAsync();

        _mockJsRuntime.Verify(
            js => js.InvokeAsync<IJSVoidResult>(
                "TourPlannerMap.setRoute",
                It.IsAny<object[]>()),
            Times.Once);
    }

    [Test]
    public async Task ClearMapAsync_ShouldClearPropertiesAndCallJavaScript()
    {
        var invokedMethod = string.Empty;
        var fromCityChanged = false;
        var toCityChanged = false;

        _mockJsRuntime
            .Setup(js => js.InvokeAsync<IJSVoidResult>(
                It.IsAny<string>(),
                It.IsAny<object[]>()))
            .Callback<string, object[]>((method, _) => invokedMethod = method)
            .ReturnsAsync(Mock.Of<IJSVoidResult>());

        _viewModel.FromCity = "Vienna";
        _viewModel.ToCity = "Berlin";

        ((INotifyPropertyChanged)_viewModel).PropertyChanged += (_, e) =>
        {
            switch (e.PropertyName)
            {
                case nameof(_viewModel.FromCity):
                    fromCityChanged = true;
                    break;
                case nameof(_viewModel.ToCity):
                    toCityChanged = true;
                    break;
            }
        };

        await _viewModel.ClearMapAsync();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(invokedMethod, Is.EqualTo("TourPlannerMap.clearMap"));
            Assert.That(_viewModel.FromCity, Is.Empty);
            Assert.That(_viewModel.ToCity, Is.Empty);
            Assert.That(fromCityChanged, Is.True);
            Assert.That(toCityChanged, Is.True);
        }
    }
}