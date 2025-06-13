using Microsoft.Extensions.Configuration;
using Microsoft.JSInterop;
using Moq;
using Serilog;
using UI.Decorator;
using UI.Model;
using UI.Service.Interface;
using UI.ViewModel;

namespace Test.UI.ViewModel;

[TestFixture]
public class TourViewModelTests
{
    [SetUp]
    public void Setup()
    {
        _mockHttpService = TestData.MockHttpService();
        _mockToastService = TestData.MockToastService();
        _mockConfiguration = TestData.MockConfiguration();
        _mockJsRuntime = TestData.MockJsRuntime();
        _mockRouteApiService = TestData.MockRouteApiService();
        _mockLogger = TestData.MockLogger();
        _mockMapViewModel = TestData.MockMapViewModel();
        _mockViewModelHelperService = TestData.MockViewModelHelperService();

        _viewModel = new TourViewModel(
            _mockHttpService.Object,
            _mockToastService.Object,
            _mockConfiguration.Object,
            _mockJsRuntime.Object,
            _mockRouteApiService.Object,
            _mockLogger.Object,
            _mockMapViewModel.Object,
            _mockViewModelHelperService.Object
        );
    }

    private Mock<IHttpService> _mockHttpService = null!;
    private Mock<IToastServiceWrapper> _mockToastService = null!;
    private Mock<IConfiguration> _mockConfiguration = null!;
    private Mock<IJSRuntime> _mockJsRuntime = null!;
    private Mock<IRouteApiService> _mockRouteApiService = null!;
    private Mock<ILogger> _mockLogger = null!;
    private Mock<IViewModelHelperService> _mockViewModelHelperService = null!;
    private Mock<MapViewModel> _mockMapViewModel = null!;
    private TourViewModel _viewModel = null!;

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
    public void IsFormValid_WithVariousInputs_ShouldValidateCorrectly(
        string name, string description, string from, string to, string transportType, bool expected)
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
        _viewModel.SelectedTour = new Tour { From = "Vienna" };


        var filteredCities = _viewModel.FilteredToCities.ToList();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(filteredCities, Does.Not.Contain("Vienna"));
            Assert.That(filteredCities, Contains.Item("Berlin"));
            Assert.That(filteredCities, Contains.Item("Paris"));
            Assert.That(filteredCities, Contains.Item("Budapest"));
            Assert.That(filteredCities, Contains.Item("Warsaw"));
            Assert.That(filteredCities.Count, Is.EqualTo(4));
        }
    }

    [Test]
    public void ToggleMap_ShouldToggleMapVisibility()
    {
        _viewModel.ToggleMap();


        _mockViewModelHelperService.Verify(
            v => v.ToggleVisibility(ref It.Ref<bool>.IsAny),
            Times.Once
        );
    }

    [Test]
    public void ShowAddTourForm_WhenFormNotVisible_ShouldShowForm()
    {
        _viewModel.IsFormVisible = false;


        _viewModel.ShowAddTourForm();
        using (Assert.EnterMultipleScope())
        {
            _mockViewModelHelperService.Verify(
                v => v.ShowForm(ref It.Ref<bool>.IsAny),
                Times.Once
            );

            Assert.That(_mockMapViewModel.Object.FromCity, Is.Empty);
            Assert.That(_mockMapViewModel.Object.ToCity, Is.Empty);
        }
    }

    [Test]
    public void ResetForm_ShouldExecuteLambdaAndResetProperties()
    {
        _viewModel.IsFormVisible = true;
        _viewModel.SelectedTour = TestData.SampleTour();


        _mockViewModelHelperService
            .Setup(v => v.ResetForm(ref It.Ref<Tour>.IsAny, It.IsAny<Func<Tour>>()))
            .Callback((ref Tour tour, Func<Tour> factory) =>
            {
                tour = factory();
                _viewModel.IsFormVisible = false;
            });


        _viewModel.ResetForm();


        Assert.That(_viewModel.IsFormVisible, Is.False);
        _mockViewModelHelperService.Verify(
            v => v.ResetForm(ref It.Ref<Tour>.IsAny, It.IsAny<Func<Tour>>()),
            Times.Once);
    }

    [Test]
    public void ShowAddTourForm_WhenFormAlreadyVisible_ShouldResetForm()
    {
        _viewModel.IsFormVisible = true;
        _viewModel.SelectedTour = TestData.SampleTour();


        _viewModel.ShowAddTourForm();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_viewModel.IsFormVisible, Is.False);

            Assert.That(_mockMapViewModel.Object.FromCity, Is.Empty);
            Assert.That(_mockMapViewModel.Object.ToCity, Is.Empty);
        }
    }

    [Test]
    public async Task LoadToursAsync_ShouldLoadToursSuccessfully()
    {
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
        _mockHttpService
            .Setup(s => s.GetListAsync<Tour>(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Network error"));


        await _viewModel.LoadToursAsync();


        _mockToastService.Verify(
            t => t.ShowError(It.Is<string>(msg => msg.Contains("Error loading tours"))),
            Times.Once
        );
    }

    [Test]
    public async Task SaveTourAsync_WithInvalidForm_ShouldShowErrorAndReturnFalse()
    {
        _viewModel.SelectedTour = new Tour();


        var result = await _viewModel.SaveTourAsync();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.False);
            _mockToastService.Verify(
                t => t.ShowError("Please fill in all required fields correctly."),
                Times.Once
            );
        }
    }

    [Test]
    public async Task SaveTourAsync_WithValidNewTour_ShouldSaveSuccessfully()
    {
        var tour = TestData.SampleTourWithVariousProperties();
        tour.Id = Guid.Empty;
        _viewModel.SelectedTour = tour;

        var fromCoords = (48.2082, 16.3738);
        var toCoords = (52.5200, 13.4050);

        TestData.SetupMapViewModel(_mockMapViewModel, fromCoords, toCoords);
        TestData.SetupRouteApiService(_mockRouteApiService, fromCoords, toCoords);


        var result = await _viewModel.SaveTourAsync();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.True);


            Assert.That(tour.Distance, Is.EqualTo(523.4));
            Assert.That(tour.EstimatedTime, Is.EqualTo(480.0));


            Assert.That(tour.ImagePath, Is.Not.Empty);


            Assert.That(tour.RouteInformation, Is.Not.Empty);


            _mockHttpService.Verify(
                s => s.PostAsync<Tour>("api/tour", It.IsAny<Tour>()),
                Times.Once
            );
            _mockToastService.Verify(
                t => t.ShowSuccess(It.Is<string>(msg => msg.Contains("Tour saved successfully"))),
                Times.Once
            );
        }
    }

    [Test]
    public async Task SaveTourAsync_WithValidExistingTour_ShouldUpdateSuccessfully()
    {
        var tour = TestData.SampleTourWithVariousProperties();
        _viewModel.SelectedTour = tour;

        var fromCoords = TestData.SampleCoordinates();
        var toCoords = TestData.SampleCoordinates();

        TestData.SetupMapViewModel(_mockMapViewModel, fromCoords, toCoords);
        TestData.SetupRouteApiService(_mockRouteApiService, fromCoords, toCoords);
        TestData.SetupHttpServicePut(_mockHttpService, tour);


        var result = await _viewModel.SaveTourAsync();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.True);
            _mockHttpService.Verify(
                s => s.PutAsync<Tour>($"api/tour/{tour.Id}", It.IsAny<Tour>()),
                Times.Once
            );
            _mockToastService.Verify(
                t => t.ShowSuccess(It.Is<string>(msg => msg.Contains("Tour updated successfully"))),
                Times.Once
            );
        }
    }

    [Test]
    public async Task SaveTourAsync_OnException_ShouldShowErrorMessage()
    {
        var testTour = TestData.SampleTourWithVariousProperties();

        _mockRouteApiService
            .Setup(r => r.FetchRouteDataAsync(It.IsAny<(double, double)>(), It.IsAny<(double, double)>(),
                It.IsAny<string>()))
            .ThrowsAsync(new Exception("Network error"));
        _viewModel.SelectedTour = testTour;


        var result = await _viewModel.SaveTourAsync();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.False);
            _mockToastService.Verify(
                t => t.ShowError(It.Is<string>(msg => msg.Contains("Error saving tour"))),
                Times.Once
            );
        }
    }

    [Test]
    public async Task ShowTourDetailsAsync_ShouldLoadTourSuccessfully()
    {
        var testTour = TestData.SampleTour();
        _mockHttpService.Setup(s => s.GetAsync<Tour>(It.IsAny<string>())).ReturnsAsync(testTour);


        await _viewModel.ShowTourDetailsAsync(testTour.Id);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_viewModel.ModalTour.Id, Is.EqualTo(testTour.Id));
            Assert.That(_viewModel.ModalTour.Name, Is.EqualTo(testTour.Name));
            Assert.That(_viewModel.ModalTour.Description, Is.EqualTo(testTour.Description));
            _mockHttpService.Verify(s => s.GetAsync<Tour>($"api/tour/{testTour.Id}"), Times.Once);
        }
    }

    [Test]
    public async Task ShowTourDetailsAsync_OnException_ShouldShowErrorMessage()
    {
        _mockHttpService
            .Setup(s => s.GetAsync<Tour>(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Network error"));


        await _viewModel.ShowTourDetailsAsync(TestData.TestGuid);


        _mockToastService.Verify(
            t => t.ShowError(It.Is<string>(msg => msg.Contains("Error loading tour details"))),
            Times.Once
        );
    }

    [Test]
    public async Task EditTourAsync_ShouldLoadTourSuccessfully()
    {
        var testTour = TestData.SampleTour();
        _mockHttpService.Setup(s => s.GetAsync<Tour>(It.IsAny<string>())).ReturnsAsync(testTour);


        await _viewModel.EditTourAsync(testTour.Id);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_viewModel.SelectedTour.Id, Is.EqualTo(testTour.Id));
            Assert.That(_viewModel.SelectedTour.Name, Is.EqualTo(testTour.Name));
            Assert.That(_viewModel.SelectedTour.Description, Is.EqualTo(testTour.Description));
            Assert.That(_viewModel.IsFormVisible, Is.True);
            _mockHttpService.Verify(s => s.GetAsync<Tour>($"api/tour/{testTour.Id}"), Times.Once);
        }
    }

    [Test]
    public async Task EditTourAsync_WithSameTourWhenEditing_ShouldResetForm()
    {
        var tourId = Guid.NewGuid();
        var tour = TestData.SampleTour();
        tour.Id = tourId;

        _mockHttpService.Setup(s => s.GetAsync<Tour>($"api/tour/{tourId}"))
            .ReturnsAsync(tour);


        await _viewModel.EditTourAsync(tourId);
        _viewModel.IsFormVisible = true;
        _viewModel.SelectedTour = tour;


        await _viewModel.EditTourAsync(tourId);


        Assert.That(_viewModel.IsFormVisible, Is.False);
    }

    [Test]
    public async Task EditTourAsync_OnException_ShouldShowErrorMessage()
    {
        _mockHttpService
            .Setup(s => s.GetAsync<Tour>(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Network error"));


        await _viewModel.EditTourAsync(Guid.Empty);


        _mockToastService.Verify(
            t => t.ShowError(It.Is<string>(msg => msg.Contains("Error handling tour edit action"))),
            Times.Once
        );
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task DeleteTourAsync_WithUserResponse_ShouldHandleCorrectly(bool userConfirms)
    {
        var testTour = TestData.SampleTour();
        _mockJsRuntime
            .Setup(js => js.InvokeAsync<bool>("confirm", It.IsAny<object[]>()))
            .ReturnsAsync(userConfirms);

        if (userConfirms) _viewModel.Tours.Add(testTour);


        await _viewModel.DeleteTourAsync(testTour.Id);


        if (userConfirms)
            using (Assert.EnterMultipleScope())
            {
                _mockHttpService.Verify(s => s.DeleteAsync($"api/tour/{testTour.Id}"), Times.Once);
                _mockToastService.Verify(t => t.ShowSuccess("Tour deleted successfully."), Times.Once);
            }
        else
            _mockHttpService.Verify(s => s.DeleteAsync(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public void UiMethodDecorator_OnException_ShouldHandleCorrectly()
    {
        var decorator = new UiMethodDecorator();
        var mockLogger = new Mock<ILogger>();
        var method = typeof(TourViewModel).GetMethod(nameof(TourViewModel.SaveTourAsync))!;
        var args = new object[] { "param1", 42, true };
        var innerException = new ArgumentNullException("innerParam");
        var exception = new InvalidOperationException("Outer exception with inner", innerException);


        Log.Logger = mockLogger.Object;

        decorator.Init(_viewModel, method, args);


        decorator.OnException(exception);
        using (Assert.EnterMultipleScope())
        {
            mockLogger.Verify(
                l => l.Error(
                    exception,
                    "Exception in {MethodName} with arguments: {@Arguments} after {Duration}ms",
                    It.Is<string>(s => s.Contains("TourViewModel.SaveTourAsync")),
                    args,
                    It.IsAny<long>()
                ),
                Times.Once
            );

            _mockToastService.Verify(
                t => t.ShowError(It.Is<string>(s => s.Contains("Outer exception with inner"))),
                Times.Once
            );
        }
    }
}