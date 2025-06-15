using UI.Model;
using UI.Service.Interface;
using UI.ViewModel;

namespace Test.UI.ViewModel;

[TestFixture]
public class TourLogViewModelTests
{
    private Mock<IHttpService> _mockHttpService = null!;
    private Mock<IToastServiceWrapper> _mockToastService = null!;
    private Mock<IJSRuntime> _mockJsRuntime = null!;
    private Mock<ILogger> _mockLogger = null!;
    private TourLogViewModel _viewModel = null!;

    [SetUp]
    public void Setup()
    {
        _mockHttpService = TestData.MockHttpService();
        _mockToastService = TestData.MockToastService();
        _mockJsRuntime = TestData.MockJsRuntime();
        _mockLogger = TestData.MockLogger();

        _viewModel = new TourLogViewModel(
            _mockHttpService.Object,
            _mockToastService.Object,
            _mockJsRuntime.Object,
            _mockLogger.Object
        );
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
        var newTourLog = TestData.SampleTourLogDto();
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
    public Task SelectedTourId_WhenSetToValidGuid_ShouldLoadTourLogs()
    {
        var newTourId = TestData.SampleTour().Id;
        _mockHttpService
            .Setup(s => s.GetListAsync<TourLog>($"api/tourlog/bytour/{newTourId}"))
            .ReturnsAsync(TestData.SampleTourLogDtoList());

        _viewModel.SelectedTourId = newTourId;

        Assert.That(_viewModel.SelectedTourId, Is.EqualTo(newTourId));
        return Task.CompletedTask;
    }

    [Test]
    public void ClearTourData_ShouldClearTourLogs()
    {
        _viewModel.TourLogs.Add(TestData.SampleTourLogDto());

        _viewModel.ClearTourData();

        Assert.That(_viewModel.TourLogs, Is.Empty);
    }

    [Test]
    public void SelectedTourId_WhenSetToEmpty_ShouldClearTourData()
    {
        _viewModel.TourLogs.Add(TestData.SampleTourLogDto());

        _viewModel.SelectedTourId = Guid.Empty;

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
        string comment, int difficulty, double distance, int time, int? rating, bool expected)
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
        var logs = TestData.SampleTourLogDtoList();
        _mockHttpService
            .Setup(s => s.GetListAsync<TourLog>($"api/tourlog/bytour/{tourId}"))
            .ReturnsAsync(logs);

        await _viewModel.LoadTourLogsAsync();

        Assert.That(_viewModel.TourLogs, Has.Count.EqualTo(logs.Count));
    }

    [Test]
    public async Task LoadTourLogsAsync_WithNullTourId_ShouldReturnEarly()
    {
        _viewModel.SelectedTourId = null;

        await _viewModel.LoadTourLogsAsync();

        _mockHttpService.Verify(
            s => s.GetListAsync<TourLog>(It.IsAny<string>()),
            Times.Never
        );
    }

    [Test]
    public async Task SaveTourLogAsync_WithValidNewLog_ShouldSaveSuccessfully()
    {
        var newLog = TestData.SampleTourLogDto();
        newLog.Id = Guid.Empty;
        _viewModel.SelectedTourId = TestData.SampleTour().Id;
        _viewModel.SelectedTourLog = newLog;

        _mockHttpService
            .Setup(s => s.PostAsync<TourLog>(It.IsAny<string>(), It.IsAny<TourLog>()))
            .ReturnsAsync(newLog);

        var result = await _viewModel.SaveTourLogAsync();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.True);
            _mockHttpService.Verify(
                s => s.PostAsync<TourLog>("api/tourlog", It.IsAny<TourLog>()),
                Times.Once
            );
            _mockToastService.Verify(t => t.ShowSuccess("Tour log created successfully."), Times.Once);
        }
    }

    [Test]
    public async Task SaveTourLogAsync_WithValidExistingLog_ShouldUpdateSuccessfully()
    {
        var existingLog = TestData.SampleTourLogDto();
        _viewModel.SelectedTourId = TestData.SampleTour().Id;
        _viewModel.SelectedTourLog = existingLog;

        _mockHttpService
            .Setup(s => s.PutAsync<TourLog>(It.IsAny<string>(), It.IsAny<TourLog>()))
            .ReturnsAsync(existingLog);

        var result = await _viewModel.SaveTourLogAsync();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.True);
            _mockHttpService.Verify(
                s => s.PutAsync<TourLog>($"api/tourlog/{existingLog.Id}", It.IsAny<TourLog>()),
                Times.Once
            );
            _mockToastService.Verify(t => t.ShowSuccess("Tour log updated successfully."), Times.Once);
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
            _mockToastService.Verify(t => t.ShowError("Not Valid."), Times.Once);
        }
    }

    [Test]
    public async Task SaveTourLogAsync_WithNullTourId_ShouldReturnFalse()
    {
        _viewModel.SelectedTourId = null;
        _viewModel.SelectedTourLog = TestData.SampleTourLogDto();

        var result = await _viewModel.SaveTourLogAsync();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.False);
            _mockToastService.Verify(t => t.ShowError("Not Valid."), Times.Once);
        }
    }

    [Test]
    public async Task EditHandleTourLogAction_WithExistingLog_ShouldLoadLogForEditing()
    {
        var logId = Guid.NewGuid();
        var tourLog = TestData.SampleTourLog(id: logId);
        var tourId = tourLog.TourId;

        _mockHttpService
            .Setup(s => s.GetAsync<TourLog>($"api/tourlog/{logId}"))
            .ReturnsAsync(tourLog);

        _mockHttpService
            .Setup(s => s.GetListAsync<TourLog>(It.IsAny<string>()))
            .ReturnsAsync(new List<TourLog>());

        _viewModel.SelectedTourId = tourId;

        await _viewModel.EditHandleTourLogAction(logId);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_viewModel.IsEditing, Is.True);
            Assert.That(_viewModel.IsLogFormVisible, Is.True);
            Assert.That(_viewModel.SelectedTourLog.Id, Is.EqualTo(logId));
            Assert.That(_viewModel.SelectedTourLog.TourId, Is.EqualTo(tourId));
        }

        _mockHttpService.Verify(s => s.GetAsync<TourLog>($"api/tourlog/{logId}"), Times.Once);
    }

    [Test]
    public async Task EditHandleTourLogAction_WithSameLogIdWhenEditing_ShouldResetForm()
    {
        var logId = Guid.NewGuid();
        var tourLog = TestData.SampleTourLog(id: logId);
        var tourId = tourLog.TourId;

        _mockHttpService
            .Setup(s => s.GetAsync<TourLog>($"api/tourlog/{logId}"))
            .ReturnsAsync(tourLog);

        _mockHttpService
            .Setup(s => s.GetListAsync<TourLog>(It.IsAny<string>()))
            .ReturnsAsync(new List<TourLog>());

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

        _mockHttpService.Verify(s => s.GetAsync<TourLog>($"api/tourlog/{logId}"), Times.Once);
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task DeleteTourLogAsync_WithUserResponse_ShouldHandleCorrectly(bool userConfirms)
    {
        var logId = TestData.TestGuid;
        _mockJsRuntime
            .Setup(j => j.InvokeAsync<bool>(It.IsAny<string>(), It.IsAny<object[]>()))
            .ReturnsAsync(userConfirms);

        await _viewModel.DeleteTourLogAsync(logId);

        if (userConfirms)
            using (Assert.EnterMultipleScope())
            {
                _mockHttpService.Verify(s => s.DeleteAsync($"api/tourlog/{logId}"), Times.Once);
                _mockToastService.Verify(t => t.ShowSuccess("Tour log deleted successfully."), Times.Once);
            }
        else
            using (Assert.EnterMultipleScope())
            {
                _mockHttpService.Verify(s => s.DeleteAsync(It.IsAny<string>()), Times.Never);
                _mockToastService.Verify(t => t.ShowSuccess(It.IsAny<string>()), Times.Never);
            }
    }

    [Test]
    public async Task DeleteTourLogAsync_WithEmptyGuid_ShouldReturnEarly()
    {
        _mockJsRuntime
            .Setup(j => j.InvokeAsync<bool>(It.IsAny<string>(), It.IsAny<object[]>()))
            .ReturnsAsync(true);

        await _viewModel.DeleteTourLogAsync(Guid.Empty);

        _mockHttpService.Verify(s => s.DeleteAsync(It.IsAny<string>()), Times.Never);
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
            Assert.That(_viewModel.SelectedTourLog.DateTime, Is.EqualTo(DateTime.Now).Within(1).Seconds);
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