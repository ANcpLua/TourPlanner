using System.Net;
using UI.Model;
using UI.Service.Interface;
using UI.ViewModel;

namespace Tests.UI.ViewModel;

[TestFixture]
public class TourViewModelTests
{
    [SetUp]
    public void Setup()
    {
        var (client, handler) = TestData.MockedHttpClient();
        _httpClient = client;
        _mockHandler = handler;
        _mockToastService = TestData.MockToastService();
        _mockConfiguration = TestData.MockConfiguration();
        _mockJsRuntime = TestData.MockJsRuntime();
        _mockRouteApiService = TestData.MockRouteApiService();
        _mockMapViewModel = TestData.MockMapViewModel();

        _viewModel = new TourViewModel(
            _httpClient,
            _mockToastService.Object,
            TestData.MockTryCatchToastWrapper(_mockToastService.Object),
            _mockConfiguration.Object,
            _mockJsRuntime.Object,
            _mockRouteApiService.Object,
            _mockMapViewModel.Object
        );
    }

    private HttpClient _httpClient = null!;
    private Mock<HttpMessageHandler> _mockHandler = null!;
    private Mock<IToastServiceWrapper> _mockToastService = null!;
    private Mock<IConfiguration> _mockConfiguration = null!;
    private Mock<IJSRuntime> _mockJsRuntime = null!;
    private Mock<IRouteApiService> _mockRouteApiService = null!;
    private Mock<MapViewModel> _mockMapViewModel = null!;
    private TourViewModel _viewModel = null!;

    [TestCase(true, "Hide Map")]
    [TestCase(false, "Show Map")]
    public void MapToggleButtonText_ReflectsVisibility(bool visible, string expected)
    {
        _viewModel.IsMapVisible = visible;
        Assert.That(_viewModel.MapToggleButtonText, Is.EqualTo(expected));
    }

    [TestCase(true, "Hide Form")]
    [TestCase(false, "Add Tour")]
    public void FormToggleButtonText_ReflectsVisibility(bool visible, string expected)
    {
        _viewModel.IsFormVisible = visible;
        Assert.That(_viewModel.FormToggleButtonText, Is.EqualTo(expected));
    }

    [TestCase(true, "Saving...")]
    [TestCase(false, "Save Tour")]
    public void SaveButtonText_ReflectsProcessing(bool processing, string expected)
    {
        _viewModel.IsProcessing = processing;
        Assert.That(_viewModel.SaveButtonText, Is.EqualTo(expected));
    }

    [Test]
    public void EditButtonText_WhenEditingThisTour_ShowsHideText()
    {
        var id = Guid.NewGuid();
        _viewModel.IsFormVisible = true;
        _viewModel.SelectedTour = TestData.SampleTour(id: id);
        Assert.That(_viewModel.EditButtonText(id), Is.EqualTo("Hide Edit Form"));
    }

    [Test]
    public void EditButtonText_WhenNotEditing_ShowsEditText()
    {
        _viewModel.IsFormVisible = false;
        Assert.That(_viewModel.EditButtonText(Guid.NewGuid()), Is.EqualTo("Edit"));
    }

    [Test]
    public void Constructor_ShouldInitializePropertiesCorrectly()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_viewModel.Tours, Is.Not.Null);
            Assert.That(_viewModel.Tours, Is.Empty);
            Assert.That(_viewModel.IsFormVisible, Is.False);
            Assert.That(_viewModel.IsMapVisible, Is.False);
            Assert.That(_viewModel.SelectedTour, Is.Not.Null);
            Assert.That(_viewModel.ModalTour, Is.Not.Null);
        }
    }

    [Test]
    public void IsFormVisible_WhenSet_ShouldUpdateProperty()
    {
        _viewModel.IsFormVisible = true;

        Assert.That(_viewModel.IsFormVisible, Is.True);
    }

    [Test]
    public void SelectedTour_WhenSet_ShouldUpdateMapViewModel()
    {
        var tour = TestData.SampleTour();
        tour.From = "Vienna";
        tour.To = "Berlin";

        _viewModel.SelectedTour = tour;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_viewModel.SelectedTour, Is.EqualTo(tour));

            Assert.That(_mockMapViewModel.Object.FromCity, Is.EqualTo("Vienna"));
            Assert.That(_mockMapViewModel.Object.ToCity, Is.EqualTo("Berlin"));
        }
    }

    [Test]
    public void IsMapVisible_WhenSet_ShouldUpdateProperty()
    {
        _viewModel.IsMapVisible = true;

        Assert.That(_viewModel.IsMapVisible, Is.True);
    }

    [TestCase("", "Description", "From", "To", "Car", false)]
    [TestCase("Name", "", "From", "To", "Car", false)]
    [TestCase("Name", "Description", "", "To", "Car", false)]
    [TestCase("Name", "Description", "From", "", "Car", false)]
    [TestCase("Name", "Description", "From", "To", "", false)]
    [TestCase("Name", "Description", "From", "To", "Car", true)]
    public void IsFormValid_WithVariousInputs_ShouldValidateCorrectly(string name, string description, string from,
        string to, string transportType, bool expected)
    {
        _viewModel.SelectedTour = new Tour
        {
            Name = name,
            Description = description,
            From = from,
            To = to,
            TransportType = transportType
        };

        Assert.That(_viewModel.IsFormValid, Is.EqualTo(expected));
    }

    [Test]
    public void FilteredToCities_ShouldExcludeSelectedFromCity()
    {
        _viewModel.SelectedTour = new Tour { Name = "", Description = "", From = "Vienna", To = "", TransportType = "" };

        var filteredCities = _viewModel.FilteredToCities.ToList();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(filteredCities, Does.Not.Contain("Vienna"));
            Assert.That(filteredCities, Contains.Item("Berlin"));
            Assert.That(filteredCities, Contains.Item("Paris"));
            Assert.That(filteredCities, Contains.Item("Budapest"));
            Assert.That(filteredCities, Contains.Item("Warsaw"));
            Assert.That(filteredCities, Has.Count.EqualTo(4));
        }
    }

    [Test]
    public void ShowAddTourForm_WhenFormNotVisible_ShouldShowForm()
    {
        _viewModel.IsFormVisible = false;

        _viewModel.ShowAddTourForm();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_viewModel.IsFormVisible, Is.True);
            Assert.That(_viewModel.SelectedTour, Is.Not.Null);
            Assert.That(_viewModel.SelectedTour.Id, Is.EqualTo(Guid.Empty));
            Assert.That(_mockMapViewModel.Object.FromCity, Is.Empty);
            Assert.That(_mockMapViewModel.Object.ToCity, Is.Empty);
        }
    }

    [Test]
    public void ResetForm_ShouldResetAllProperties()
    {
        _viewModel.IsFormVisible = true;
        _viewModel.SelectedTour = TestData.SampleTour("Existing Tour");
        _mockMapViewModel.Object.FromCity = "Vienna";
        _mockMapViewModel.Object.ToCity = "Paris";

        _viewModel.ResetForm();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_viewModel.IsFormVisible, Is.False);
            Assert.That(_viewModel.SelectedTour, Is.Not.Null);
            Assert.That(_viewModel.SelectedTour.Id, Is.EqualTo(Guid.Empty));
            Assert.That(_viewModel.SelectedTour.Name, Is.Empty);
            Assert.That(_mockMapViewModel.Object.FromCity, Is.Empty);
            Assert.That(_mockMapViewModel.Object.ToCity, Is.Empty);
        }
    }

    [Test]
    public void ShowAddTourForm_WhenFormAlreadyVisible_ShouldResetForm()
    {
        _viewModel.IsFormVisible = true;
        _viewModel.SelectedTour = TestData.SampleTour("Old Tour");

        _viewModel.ShowAddTourForm();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_viewModel.IsFormVisible, Is.False);
            Assert.That(_viewModel.SelectedTour.Name, Is.Empty);
        }
    }

    [Test]
    public void ToggleMap_ShouldToggleVisibility()
    {
        _viewModel.IsMapVisible = false;
        _viewModel.ToggleMap();
        Assert.That(_viewModel.IsMapVisible, Is.True);

        _viewModel.ToggleMap();
        Assert.That(_viewModel.IsMapVisible, Is.False);
    }

    [Test]
    public async Task LoadToursAsync_ShouldLoadToursSuccessfully()
    {
        var tours = TestData.SampleTourList(5);
        TestData.SetupHandler(_mockHandler, HttpMethod.Get, "api/tour", JsonSerializer.Serialize(tours));

        await _viewModel.LoadToursAsync();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_viewModel.Tours, Is.Not.Empty);
            Assert.That(_viewModel.Tours, Has.Count.EqualTo(5));
        }
    }

    [Test]
    public async Task LoadToursAsync_OnException_ShouldShowErrorMessage()
    {
        TestData.SetupHttpMessageHandlerError(_mockHandler, HttpStatusCode.InternalServerError, "error");

        await _viewModel.LoadToursAsync();

        _mockToastService.Verify(
            static t => t.ShowError(It.Is<string>(static msg => msg.Contains("Error loading tours"))),
            Times.Once
        );
    }

    [Test]
    public async Task SaveTourAsync_WithValidNewTour_ShouldSaveSuccessfully()
    {
        var tour = TestData.SampleTour();
        tour.Id = Guid.Empty;
        _viewModel.SelectedTour = tour;

        var fromCoords = TestData.TestCoordinates;
        var toCoords = (52.5200, 13.4050);

        _mockMapViewModel.Setup(m => m.GetCoordinates(tour.From)).Returns(fromCoords);
        _mockMapViewModel.Setup(m => m.GetCoordinates(tour.To)).Returns(toCoords);
        _mockRouteApiService.Setup(r => r.FetchRouteDataAsync(fromCoords, toCoords, tour.TransportType))
            .ReturnsAsync((523.4, 480.0));
        TestData.SetupHandler(_mockHandler, HttpMethod.Post, "api/tour", "{}");
        TestData.SetupHandler(_mockHandler, HttpMethod.Get, "api/tour", JsonSerializer.Serialize(TestData.SampleTourList()));

        _mockJsRuntime.Setup(static j => j.InvokeAsync<IJSVoidResult>(
                "TourPlannerMap.setRoute",
                It.IsAny<object[]>()))
            .ReturnsAsync(Mock.Of<IJSVoidResult>());

        var result = await _viewModel.SaveTourAsync();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.True);
            Assert.That(tour.Distance, Is.EqualTo(523.4));
            Assert.That(tour.EstimatedTime, Is.EqualTo(480.0));
            Assert.That(tour.ImagePath, Is.Not.Empty);
            Assert.That(tour.RouteInformation, Is.Not.Empty);
        }

        TestData.VerifyHandler(_mockHandler, HttpMethod.Post, "api/tour", Times.Once());
        _mockToastService.Verify(static t => t.ShowSuccess("Tour saved successfully."), Times.Once);
    }

    [Test]
    public async Task SaveTourAsync_WithValidExistingTour_ShouldUpdateSuccessfully()
    {
        var tour = TestData.SampleTour();
        _viewModel.SelectedTour = tour;

        var fromCoords = TestData.TestCoordinates;
        var toCoords = TestData.TestCoordinates;

        _mockMapViewModel.Setup(m => m.GetCoordinates(tour.From)).Returns(fromCoords);
        _mockMapViewModel.Setup(m => m.GetCoordinates(tour.To)).Returns(toCoords);
        _mockRouteApiService.Setup(r => r.FetchRouteDataAsync(fromCoords, toCoords, tour.TransportType))
            .ReturnsAsync((100.5, 60.0));
        TestData.SetupHandler(_mockHandler, HttpMethod.Put, $"api/tour/{tour.Id}", "{}");
        TestData.SetupHandler(_mockHandler, HttpMethod.Get, "api/tour", JsonSerializer.Serialize(TestData.SampleTourList()));

        _mockJsRuntime.Setup(static j => j.InvokeAsync<IJSVoidResult>(
                "TourPlannerMap.setRoute",
                It.IsAny<object[]>()))
            .ReturnsAsync(Mock.Of<IJSVoidResult>());

        var result = await _viewModel.SaveTourAsync();

        Assert.That(result, Is.True);
        TestData.VerifyHandler(_mockHandler, HttpMethod.Put, $"api/tour/{tour.Id}", Times.Once());
        _mockToastService.Verify(static t => t.ShowSuccess("Tour updated successfully."), Times.Once);
    }

    [Test]
    public async Task SaveTourAsync_OnException_ShouldShowErrorMessage()
    {
        var testTour = TestData.SampleTour();
        _viewModel.SelectedTour = testTour;

        _mockMapViewModel.Setup(static m => m.GetCoordinates(It.IsAny<string>()))
            .Returns(TestData.TestCoordinates);
        _mockRouteApiService
            .Setup(static r => r.FetchRouteDataAsync(It.IsAny<(double, double)>(), It.IsAny<(double, double)>(),
                It.IsAny<string>()))
            .ThrowsAsync(new HttpRequestException("Network error"));

        var result = await _viewModel.SaveTourAsync();

        Assert.That(result, Is.False);

        _mockToastService.Verify(static t => t.ShowError(It.Is<string>(static s => s.Contains("Error saving tour"))), Times.Once);
    }

    [Test]
    public async Task ShowTourDetailsAsync_ShouldLoadTourSuccessfully()
    {
        var testTour = TestData.SampleTour();
        TestData.SetupHandler(_mockHandler, HttpMethod.Get, $"api/tour/{testTour.Id}", JsonSerializer.Serialize(testTour));

        await _viewModel.ShowTourDetailsAsync(testTour.Id);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_viewModel.ModalTour.Id, Is.EqualTo(testTour.Id));
            Assert.That(_viewModel.ModalTour.Name, Is.EqualTo(testTour.Name));
            Assert.That(_viewModel.ModalTour.Description, Is.EqualTo(testTour.Description));
            TestData.VerifyHandler(_mockHandler, HttpMethod.Get, $"api/tour/{testTour.Id}", Times.Once());
        }
    }

    [Test]
    public async Task ShowTourDetailsAsync_OnException_ShouldShowErrorMessage()
    {
        TestData.SetupHttpMessageHandlerError(_mockHandler, HttpStatusCode.InternalServerError, "error");

        await _viewModel.ShowTourDetailsAsync(TestData.TestGuid);

        _mockToastService.Verify(
            static t => t.ShowError(It.Is<string>(static msg => msg.Contains("Error loading tour details"))),
            Times.Once
        );
    }

    [Test]
    public async Task EditTourAsync_ShouldLoadTourSuccessfully()
    {
        var testTour = TestData.SampleTour();
        TestData.SetupHandler(_mockHandler, HttpMethod.Get, $"api/tour/{testTour.Id}", JsonSerializer.Serialize(testTour));

        await _viewModel.EditTourAsync(testTour.Id);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_viewModel.SelectedTour.Id, Is.EqualTo(testTour.Id));
            Assert.That(_viewModel.SelectedTour.Name, Is.EqualTo(testTour.Name));
            Assert.That(_viewModel.SelectedTour.Description, Is.EqualTo(testTour.Description));
            Assert.That(_viewModel.IsFormVisible, Is.True);
            TestData.VerifyHandler(_mockHandler, HttpMethod.Get, $"api/tour/{testTour.Id}", Times.Once());
        }
    }

    [Test]
    public async Task EditTourAsync_WithSameTourWhenEditing_ShouldResetForm()
    {
        var tourId = Guid.NewGuid();
        var tour = TestData.SampleTour();
        tour.Id = tourId;

        TestData.SetupHandler(_mockHandler, HttpMethod.Get, $"api/tour/{tourId}", JsonSerializer.Serialize(tour));

        await _viewModel.EditTourAsync(tourId);
        _viewModel.IsFormVisible = true;
        _viewModel.SelectedTour = tour;

        await _viewModel.EditTourAsync(tourId);

        Assert.That(_viewModel.IsFormVisible, Is.False);
    }

    [Test]
    public async Task EditTourAsync_OnException_ShouldShowErrorMessage()
    {
        TestData.SetupHttpMessageHandlerError(_mockHandler, HttpStatusCode.InternalServerError, "error");

        await _viewModel.EditTourAsync(Guid.Empty);

        _mockToastService.Verify(
            static t => t.ShowError(It.Is<string>(static msg => msg.Contains("Error handling tour edit action"))),
            Times.Once
        );
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task DeleteTourAsync_WithUserResponse_ShouldHandleCorrectly(bool userConfirms)
    {
        var testTour = TestData.SampleTour();
        _mockJsRuntime
            .Setup(static js => js.InvokeAsync<bool>("confirm", It.IsAny<object[]>()))
            .ReturnsAsync(userConfirms);

        if (userConfirms)
        {
            _viewModel.Tours.Add(testTour);
            TestData.SetupHandler(_mockHandler, HttpMethod.Delete, $"api/tour/{testTour.Id}", "{}");
            TestData.SetupHandler(_mockHandler, HttpMethod.Get, "api/tour", JsonSerializer.Serialize(TestData.SampleTourList()));
        }

        await _viewModel.DeleteTourAsync(testTour.Id);

        if (userConfirms)
            using (Assert.EnterMultipleScope())
            {
                TestData.VerifyHandler(_mockHandler, HttpMethod.Delete, $"api/tour/{testTour.Id}", Times.Once());
                _mockToastService.Verify(static t => t.ShowSuccess("Tour deleted successfully."), Times.Once);
            }
        else
            TestData.VerifyHandler(_mockHandler, HttpMethod.Delete, $"api/tour/{testTour.Id}", Times.Never());
    }
}
