using UI.Model;
using UI.Service.Interface;
using UI.ViewModel;

namespace Tests.UI.ViewModel;

[TestFixture]
public class TourLogViewModelTests
{
    [SetUp]
    public void Setup()
    {
        var (client, handler) = TestData.MockedHttpClient();
        _httpClient = client;
        _mockHandler = handler;
        _mockToastService = TestData.MockToastService();
        _mockJsRuntime = TestData.MockJsRuntime();

        _viewModel = new TourLogViewModel(
            _httpClient,
            _mockToastService.Object,
            TestData.MockTryCatchToastWrapper(),
            _mockJsRuntime.Object
        );
    }

    private HttpClient _httpClient = null!;
    private Mock<HttpMessageHandler> _mockHandler = null!;
    private Mock<IToastServiceWrapper> _mockToastService = null!;
    private Mock<IJSRuntime> _mockJsRuntime = null!;
    private TourLogViewModel _viewModel = null!;

    [TestCase(true, "Hide Form")]
    [TestCase(false, "Add New Log")]
    public void LogFormToggleButtonText_ReflectsVisibility(bool visible, string expected)
    {
        _viewModel.IsLogFormVisible = visible;
        Assert.That(_viewModel.LogFormToggleButtonText, Is.EqualTo(expected));
    }

    [Test]
    public void LogFormTitle_NewLog_ShowsAddText()
    {
        _viewModel.SelectedTourLog = new TourLog { Id = Guid.Empty };
        Assert.That(_viewModel.LogFormTitle, Is.EqualTo("Add New Log"));
    }

    [Test]
    public void LogFormTitle_ExistingLog_ShowsEditText()
    {
        _viewModel.SelectedTourLog = TestData.SampleTourLog();
        Assert.That(_viewModel.LogFormTitle, Is.EqualTo("Edit Log"));
    }

    [Test]
    public void EditLogButtonText_WhenEditingThisLog_ShowsHideText()
    {
        var logId = Guid.NewGuid();
        _viewModel.IsEditing = true;
        _viewModel.IsLogFormVisible = true;
        _viewModel.SelectedTourLog = new TourLog { Id = logId };
        Assert.That(_viewModel.EditLogButtonText(logId), Is.EqualTo("Hide Edit Form"));
    }

    [Test]
    public void EditLogButtonText_WhenNotEditing_ShowsEditText()
    {
        _viewModel.IsEditing = false;
        Assert.That(_viewModel.EditLogButtonText(Guid.NewGuid()), Is.EqualTo("Edit"));
    }

    [Test]
    public void Constructor_ShouldInitializePropertiesCorrectly()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_viewModel.TourLogs, Is.Not.Null);
            Assert.That(_viewModel.TourLogs, Is.Empty);
            Assert.That(_viewModel.SelectedTourLog, Is.Not.Null);
            Assert.That(_viewModel.IsLogFormVisible, Is.False);
            Assert.That(_viewModel.IsEditing, Is.False);
            Assert.That(_viewModel.SelectedTourId, Is.Null);
        }
    }

    [Test]
    public void SelectedTourLog_WhenSet_ShouldRaisePropertyChangedEvent()
    {
        var newTourLog = TestData.SampleTourLog();
        var eventRaised = false;
        _viewModel.PropertyChanged += (_, _) => eventRaised = true;

        _viewModel.SelectedTourLog = newTourLog;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_viewModel.SelectedTourLog, Is.EqualTo(newTourLog));
            Assert.That(eventRaised, Is.True);
        }
    }

    [Test]
    public void IsLogFormVisible_WhenSet_ShouldUpdateProperty()
    {
        _viewModel.IsLogFormVisible = true;

        Assert.That(_viewModel.IsLogFormVisible, Is.True);
    }

    [Test]
    public void IsEditing_WhenSet_ShouldUpdateProperty()
    {
        _viewModel.IsEditing = true;

        Assert.That(_viewModel.IsEditing, Is.True);
    }

    [Test]
    public void SelectedTourId_WhenSetToValidGuid_ShouldLoadTourLogs()
    {
        var newTourId = TestData.SampleTour().Id;
        TestData.SetupHandler(_mockHandler, HttpMethod.Get, $"api/tourlog/bytour/{newTourId}",
            TestData.SampleTourLogList(tourId: newTourId));

        _viewModel.SelectedTourId = newTourId;

        Assert.That(_viewModel.SelectedTourId, Is.EqualTo(newTourId));
    }

    [Test]
    public void ClearTourData_ShouldClearTourLogs()
    {
        _viewModel.TourLogs.Add(TestData.SampleTourLog());

        _viewModel.ClearTourData();

        Assert.That(_viewModel.TourLogs, Is.Empty);
    }

    [Test]
    public async Task OnTourSelectionChanged_WithEmptyGuid_ShouldClearTourData()
    {
        _viewModel.TourLogs.Add(TestData.SampleTourLog());

        _viewModel.SelectedTourId = Guid.Empty;
        await _viewModel.OnTourSelectionChangedAsync();

        Assert.That(_viewModel.TourLogs, Is.Empty);
    }

    [Test]
    public async Task OnTourSelectionChanged_WithNull_ShouldClearTourData()
    {
        _viewModel.SelectedTourId = Guid.NewGuid();
        _viewModel.TourLogs.Add(TestData.SampleTourLog());

        _viewModel.SelectedTourId = null;
        await _viewModel.OnTourSelectionChangedAsync();

        Assert.That(_viewModel.TourLogs, Is.Empty);
    }

    [Test]
    public void IsFormValid_WithValidData_ShouldReturnTrue()
    {
        _viewModel.SelectedTourLog = new TourLog
        {
            Comment = "Valid comment",
            Difficulty = 3,
            TotalDistance = 10,
            TotalTime = 60,
            Rating = 4
        };

        Assert.That(_viewModel.IsFormValid, Is.True);
    }

    [TestCase("", 1, 1, 1, 1, false)]
    [TestCase("Valid", 0, 1, 1, 1, false)]
    [TestCase("Valid", 6, 1, 1, 1, false)]
    [TestCase("Valid", 3, 0, 1, 1, false)]
    [TestCase("Valid", 3, 1, 0, 1, false)]
    [TestCase("Valid", 3, 1, 1, 0, false)]
    [TestCase("Valid", 3, 1, 1, 6, false)]
    [TestCase("Valid", 3, 1, 1, 3, true)]
    public void IsFormValid_WithVariousInputs_ShouldValidateCorrectly(
        string comment, double difficulty, double distance, int time, double? rating, bool expected)
    {
        _viewModel.SelectedTourLog = new TourLog
        {
            Comment = comment,
            Difficulty = difficulty,
            TotalDistance = distance,
            TotalTime = time,
            Rating = rating
        };

        Assert.That(_viewModel.IsFormValid, Is.EqualTo(expected));
    }

    [Test]
    public void IsFormValid_WithNullRating_ShouldReturnFalse()
    {
        _viewModel.SelectedTourLog = new TourLog
        {
            Comment = "Valid",
            Difficulty = 3,
            TotalDistance = 1,
            TotalTime = 1,
            Rating = null
        };

        Assert.That(_viewModel.IsFormValid, Is.False);
    }

    [Test]
    public void ToggleLogForm_WithNullTourId_ShouldNotShowForm()
    {
        _viewModel.SelectedTourId = null;

        _viewModel.ToggleLogForm();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_viewModel.IsLogFormVisible, Is.False);
            Assert.That(_viewModel.IsEditing, Is.False);
        }
    }

    [Test]
    public void ToggleLogForm_WhenFormVisible_ShouldHideForm()
    {
        _viewModel.SelectedTourId = Guid.NewGuid();
        _viewModel.ToggleLogForm();
        _viewModel.IsLogFormVisible = true;

        _viewModel.ToggleLogForm();

        Assert.That(_viewModel.IsLogFormVisible, Is.False);
    }

    [Test]
    public async Task LoadTourLogsAsync_WithValidTourId_ShouldLoadLogs()
    {
        var tourId = Guid.NewGuid();
        _viewModel.SelectedTourId = tourId;
        var logs = TestData.SampleTourLogList(tourId: tourId);
        TestData.SetupHandler(_mockHandler, HttpMethod.Get, $"api/tourlog/bytour/{tourId}",
            logs);

        await _viewModel.LoadTourLogsAsync();

        Assert.That(_viewModel.TourLogs, Has.Count.EqualTo(logs.Count));
    }

    [Test]
    public async Task LoadTourLogsAsync_WithNullTourId_ShouldReturnEarly()
    {
        _viewModel.SelectedTourId = null;

        await _viewModel.LoadTourLogsAsync();

        TestData.VerifyHandler(_mockHandler, HttpMethod.Get, "api/tourlog", Times.Never());
    }

    [Test]
    public async Task SaveTourLogAsync_WithValidNewLog_ShouldSaveSuccessfully()
    {
        var newLog = TestData.SampleTourLog();
        newLog.Id = Guid.Empty;
        _viewModel.SelectedTourId = TestData.SampleTour().Id;
        _viewModel.SelectedTourLog = newLog;

        TestData.SetupHandler(_mockHandler, HttpMethod.Post, "api/tourlog", "{}");
        TestData.SetupHandler(_mockHandler, HttpMethod.Get, $"api/tourlog/bytour/{_viewModel.SelectedTourId}",
            TestData.SampleTourLogList());

        var result = await _viewModel.SaveTourLogAsync();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.True);
            TestData.VerifyHandler(_mockHandler, HttpMethod.Post, "api/tourlog", Times.Once());
            _mockToastService.Verify(static t => t.ShowSuccess("Tour log created successfully."), Times.Once);
        }
    }

    [Test]
    public async Task SaveTourLogAsync_WithValidExistingLog_ShouldUpdateSuccessfully()
    {
        var existingLog = TestData.SampleTourLog();
        _viewModel.SelectedTourId = TestData.SampleTour().Id;
        _viewModel.SelectedTourLog = existingLog;

        TestData.SetupHandler(_mockHandler, HttpMethod.Put, $"api/tourlog/{existingLog.Id}", "{}");
        TestData.SetupHandler(_mockHandler, HttpMethod.Get, $"api/tourlog/bytour/{_viewModel.SelectedTourId}",
            TestData.SampleTourLogList());

        var result = await _viewModel.SaveTourLogAsync();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.True);
            TestData.VerifyHandler(_mockHandler, HttpMethod.Put, $"api/tourlog/{existingLog.Id}", Times.Once());
            _mockToastService.Verify(static t => t.ShowSuccess("Tour log updated successfully."), Times.Once);
        }
    }

    [Test]
    public async Task SaveTourLogAsync_WithInvalidForm_ShouldReturnFalse()
    {
        _viewModel.SelectedTourId = TestData.SampleTour().Id;
        _viewModel.SelectedTourLog = new TourLog();

        var result = await _viewModel.SaveTourLogAsync();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.False);
            _mockToastService.Verify(static t => t.ShowError("Not Valid."), Times.Once);
        }
    }

    [Test]
    public async Task SaveTourLogAsync_WithNullTourId_ShouldReturnFalse()
    {
        _viewModel.SelectedTourId = null;
        _viewModel.SelectedTourLog = TestData.SampleTourLog();

        var result = await _viewModel.SaveTourLogAsync();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.False);
            _mockToastService.Verify(static t => t.ShowError("Not Valid."), Times.Once);
        }
    }

    [Test]
    public async Task EditHandleTourLogAction_WithExistingLog_ShouldLoadLogForEditing()
    {
        var logId = Guid.NewGuid();
        var tourLog = TestData.SampleTourLog(id: logId);
        var tourId = tourLog.TourId;

        TestData.SetupHandler(_mockHandler, HttpMethod.Get, $"api/tourlog/{logId}",
            tourLog);
        TestData.SetupHandler(_mockHandler, HttpMethod.Get, $"api/tourlog/bytour/{tourId}",
            TestData.SampleTourLogList());

        _viewModel.SelectedTourId = tourId;

        await _viewModel.EditHandleTourLogAction(logId);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_viewModel.IsEditing, Is.True);
            Assert.That(_viewModel.IsLogFormVisible, Is.True);
            Assert.That(_viewModel.SelectedTourLog.Id, Is.EqualTo(logId));
            Assert.That(_viewModel.SelectedTourLog.TourId, Is.EqualTo(tourId));
        }

        TestData.VerifyHandler(_mockHandler, HttpMethod.Get, $"api/tourlog/{logId}", Times.Once());
    }

    [Test]
    public async Task EditHandleTourLogAction_WithSameLogIdWhenEditing_ShouldResetForm()
    {
        var logId = Guid.NewGuid();
        var tourLog = TestData.SampleTourLog(id: logId);
        var tourId = tourLog.TourId;

        TestData.SetupHandler(_mockHandler, HttpMethod.Get, $"api/tourlog/{logId}",
            tourLog);
        TestData.SetupHandler(_mockHandler, HttpMethod.Get, $"api/tourlog/bytour/{tourId}",
            TestData.SampleTourLogList());

        _viewModel.SelectedTourId = tourId;

        await _viewModel.EditHandleTourLogAction(logId);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_viewModel.IsEditing, Is.True);
            Assert.That(_viewModel.IsLogFormVisible, Is.True);
            Assert.That(_viewModel.SelectedTourLog.Id, Is.EqualTo(logId));
        }

        await _viewModel.EditHandleTourLogAction(logId);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_viewModel.IsLogFormVisible, Is.False);
            Assert.That(_viewModel.IsEditing, Is.False);
            Assert.That(_viewModel.SelectedTourLog.Id, Is.EqualTo(Guid.Empty));
            Assert.That(_viewModel.SelectedTourLog.TourId, Is.EqualTo(tourId));
        }

        TestData.VerifyHandler(_mockHandler, HttpMethod.Get, $"api/tourlog/{logId}", Times.Once());
    }

    [Test]
    public async Task EditHandleTourLogAction_WithNullLogId_ShouldToggleLogForm()
    {
        _viewModel.SelectedTourId = Guid.NewGuid();
        _viewModel.IsLogFormVisible = false;

        await _viewModel.EditHandleTourLogAction();

        Assert.That(_viewModel.IsLogFormVisible, Is.True);
    }

    [Test]
    public async Task EditHandleTourLogAction_WithEmptyLogId_ShouldToggleLogForm()
    {
        _viewModel.SelectedTourId = Guid.NewGuid();
        _viewModel.IsLogFormVisible = false;

        await _viewModel.EditHandleTourLogAction(Guid.Empty);

        Assert.That(_viewModel.IsLogFormVisible, Is.True);
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task DeleteTourLogAsync_WithUserResponse_ShouldHandleCorrectly(bool userConfirms)
    {
        var logId = TestData.TestGuid;
        _mockJsRuntime
            .Setup(static j => j.InvokeAsync<bool>(It.IsAny<string>(), It.IsAny<object[]>()))
            .ReturnsAsync(userConfirms);

        if (userConfirms)
        {
            TestData.SetupHandler(_mockHandler, HttpMethod.Delete, $"api/tourlog/{logId}", "{}");
            TestData.SetupHandler(_mockHandler, HttpMethod.Get, "api/tourlog/bytour/", "[]");
        }

        await _viewModel.DeleteTourLogAsync(logId);

        if (userConfirms)
            using (Assert.EnterMultipleScope())
            {
                TestData.VerifyHandler(_mockHandler, HttpMethod.Delete, $"api/tourlog/{logId}", Times.Once());
                _mockToastService.Verify(static t => t.ShowSuccess("Tour log deleted successfully."), Times.Once);
            }
        else
            using (Assert.EnterMultipleScope())
            {
                TestData.VerifyHandler(_mockHandler, HttpMethod.Delete, $"api/tourlog/{logId}", Times.Never());
                _mockToastService.Verify(static t => t.ShowSuccess(It.IsAny<string>()), Times.Never);
            }
    }

    [Test]
    public async Task DeleteTourLogAsync_WithEmptyGuid_ShouldReturnEarly()
    {
        _mockJsRuntime
            .Setup(static j => j.InvokeAsync<bool>(It.IsAny<string>(), It.IsAny<object[]>()))
            .ReturnsAsync(true);

        await _viewModel.DeleteTourLogAsync(Guid.Empty);

        TestData.VerifyHandler(_mockHandler, HttpMethod.Delete, "api/tourlog/", Times.Never());
    }

    [Test]
    public void ShowAddLogForm_WithValidTourId_ShouldCreateNewTourLog()
    {
        var tourId = Guid.NewGuid();
        _viewModel.SelectedTourId = tourId;

        _viewModel.ShowAddLogForm();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_viewModel.SelectedTourLog, Is.Not.Null);
            Assert.That(_viewModel.SelectedTourLog.TourId, Is.EqualTo(tourId));
            Assert.That(_viewModel.SelectedTourLog.DateTime,
                Is.EqualTo(TimeProvider.System.GetUtcNow().UtcDateTime).Within(1).Seconds);
            Assert.That(_viewModel.SelectedTourLog.Difficulty, Is.EqualTo(1));
            Assert.That(_viewModel.SelectedTourLog.Rating, Is.EqualTo(1));
            Assert.That(_viewModel.IsLogFormVisible, Is.True);
            Assert.That(_viewModel.IsEditing, Is.False);
        }
    }

    [Test]
    public void ShowAddLogForm_WithNullTourId_ShouldReturnEarly()
    {
        _viewModel.SelectedTourId = null;
        var previousTourLog = _viewModel.SelectedTourLog;
        var previousFormVisible = _viewModel.IsLogFormVisible;

        _viewModel.ShowAddLogForm();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_viewModel.SelectedTourLog, Is.SameAs(previousTourLog));
            Assert.That(_viewModel.IsLogFormVisible, Is.EqualTo(previousFormVisible));
        }
    }

    [Test]
    public void ResetForm_WithValidTourId_ShouldResetAllProperties()
    {
        var tourId = Guid.NewGuid();
        _viewModel.SelectedTourId = tourId;
        _viewModel.IsLogFormVisible = true;
        _viewModel.IsEditing = true;
        _viewModel.SelectedTourLog = TestData.SampleTourLog(tourId: Guid.NewGuid());

        _viewModel.ResetForm();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_viewModel.SelectedTourLog, Is.Not.Null);
            Assert.That(_viewModel.SelectedTourLog.TourId, Is.EqualTo(tourId));
            Assert.That(_viewModel.SelectedTourLog.Id, Is.EqualTo(Guid.Empty));
            Assert.That(_viewModel.IsLogFormVisible, Is.False);
            Assert.That(_viewModel.IsEditing, Is.False);
        }
    }

    [Test]
    public void ResetForm_WithNullTourId_ShouldUseEmptyGuid()
    {
        _viewModel.SelectedTourId = null;

        _viewModel.ResetForm();

        Assert.That(_viewModel.SelectedTourLog.TourId, Is.EqualTo(Guid.Empty));
    }
}
