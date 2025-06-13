using Microsoft.JSInterop;
using Moq;
using Serilog;
using UI.Model;
using UI.Service.Interface;
using UI.ViewModel;

namespace Test.UI.ViewModel;

[TestFixture]
public class TourLogViewModelTests
{
    [SetUp]
    public void Setup()
    {
        _mockHttpService = TestData.MockHttpService();
        _mockToastService = TestData.MockToastService();
        _mockJsRuntime = TestData.MockJsRuntime();
        _mockLogger = TestData.MockLogger();
        _mockViewModelHelperService = TestData.MockViewModelHelperService();

        _viewModel = new TourLogViewModel(
            _mockHttpService.Object,
            _mockToastService.Object,
            _mockJsRuntime.Object,
            _mockLogger.Object,
            _mockViewModelHelperService.Object
        );
    }

    private Mock<IHttpService> _mockHttpService = null!;
    private Mock<IToastServiceWrapper> _mockToastService = null!;
    private Mock<IJSRuntime> _mockJsRuntime = null!;
    private Mock<IViewModelHelperService> _mockViewModelHelperService = null!;
    private Mock<ILogger> _mockLogger = null!;
    private TourLogViewModel _viewModel = null!;

    [Test]
    public void Constructor_ShouldInitializePropertiesCorrectly()
    {
        Assert.Multiple(() =>
        {
            Assert.That(_viewModel.TourLogs, Is.Not.Null);
            Assert.That(_viewModel.TourLogs, Is.Empty);
            Assert.That(_viewModel.SelectedTourLog, Is.Not.Null);
            Assert.That(_viewModel.IsLogFormVisible, Is.False);
            Assert.That(_viewModel.IsEditing, Is.False);
            Assert.That(_viewModel.SelectedTourId, Is.Null);
        });
    }

    [Test]
    public void SelectedTourLog_WhenSet_ShouldRaisePropertyChangedEvent()
    {
        var newTourLog = TestData.SampleTourLogDto();
        var eventRaised = false;
        _viewModel.PropertyChanged += (_, _) => eventRaised = true;


        _viewModel.SelectedTourLog = newTourLog;


        Assert.Multiple(() =>
        {
            Assert.That(_viewModel.SelectedTourLog, Is.EqualTo(newTourLog));
            Assert.That(eventRaised, Is.True);
        });
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


        Assert.Multiple(() =>
        {
            Assert.That(_viewModel.IsLogFormVisible, Is.False);
            Assert.That(_viewModel.IsEditing, Is.False);
        });
    }

    [Test]
    public void ToggleLogForm_WhenFormNotVisible_ShouldShowForm()
    {
        _viewModel.SelectedTourId = Guid.NewGuid();
        _viewModel.IsLogFormVisible = false;


        _viewModel.ToggleLogForm();


        _mockViewModelHelperService.Verify(
            v => v.ShowForm(ref It.Ref<bool>.IsAny),
            Times.Once
        );
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


        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            _mockHttpService.Verify(
                s => s.PostAsync<TourLog>("api/tourlog", It.IsAny<TourLog>()),
                Times.Once
            );
            _mockToastService.Verify(t => t.ShowSuccess("Tour log created successfully."), Times.Once);
        });
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


        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            _mockHttpService.Verify(
                s => s.PutAsync<TourLog>($"api/tourlog/{existingLog.Id}", It.IsAny<TourLog>()),
                Times.Once
            );
            _mockToastService.Verify(t => t.ShowSuccess("Tour log updated successfully."), Times.Once);
        });
    }

    [Test]
    public async Task SaveTourLogAsync_WithInvalidForm_ShouldReturnFalse()
    {
        _viewModel.SelectedTourId = TestData.SampleTour().Id;
        _viewModel.SelectedTourLog = new TourLog();


        var result = await _viewModel.SaveTourLogAsync();


        Assert.Multiple(() =>
        {
            Assert.That(result, Is.False);
            _mockToastService.Verify(t => t.ShowError("Not Valid."), Times.Once);
        });
    }

    [Test]
    public async Task SaveTourLogAsync_WithNullTourId_ShouldReturnFalse()
    {
        _viewModel.SelectedTourId = null;
        _viewModel.SelectedTourLog = TestData.SampleTourLogDto();


        var result = await _viewModel.SaveTourLogAsync();


        Assert.Multiple(() =>
        {
            Assert.That(result, Is.False);
            _mockToastService.Verify(t => t.ShowError("Not Valid."), Times.Once);
        });
    }

    [Test]
    public async Task EditHandleTourLogAction_WithExistingLog_ShouldLoadLogForEditing()
    {
        var existingLog = TestData.SampleTourLogDto();
        _mockHttpService
            .Setup(s => s.GetAsync<TourLog>(It.IsAny<string>()))
            .ReturnsAsync(existingLog);


        await _viewModel.EditHandleTourLogAction(existingLog.Id);


        Assert.Multiple(() =>
        {
            Assert.That(_viewModel.SelectedTourLog, Is.EqualTo(existingLog));
            Assert.That(_viewModel.SelectedTourId, Is.EqualTo(existingLog.TourId));
            Assert.That(_viewModel.IsLogFormVisible, Is.True);
            Assert.That(_viewModel.IsEditing, Is.True);
        });
    }

    [Test]
    public async Task EditHandleTourLogAction_WithSameLogIdWhenEditing_ShouldResetForm()
    {
        var logId = Guid.NewGuid();
        var tourLog = TestData.SampleTourLogDto();
        tourLog.Id = logId;

        _mockHttpService
            .Setup(s => s.GetAsync<TourLog>($"api/tourlog/{logId}"))
            .ReturnsAsync(tourLog);


        await _viewModel.EditHandleTourLogAction(logId);
        _viewModel.IsEditing = true;
        _viewModel.IsLogFormVisible = true;


        await _viewModel.EditHandleTourLogAction(logId);


        Assert.That(_viewModel.IsLogFormVisible, Is.False);
    }

    [Test]
    public async Task EditHandleTourLogAction_WithNullLogId_ShouldToggleForm()
    {
        _viewModel.SelectedTourId = Guid.NewGuid();


        await _viewModel.EditHandleTourLogAction();


        _mockViewModelHelperService.Verify(
            v => v.ShowForm(ref It.Ref<bool>.IsAny),
            Times.Once
        );
    }

    [Test]
    public async Task EditHandleTourLogAction_WithEmptyGuid_ShouldToggleForm()
    {
        _viewModel.SelectedTourId = Guid.NewGuid();


        await _viewModel.EditHandleTourLogAction(Guid.Empty);


        _mockViewModelHelperService.Verify(
            v => v.ShowForm(ref It.Ref<bool>.IsAny),
            Times.Once
        );
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
            Assert.Multiple(() =>
            {
                _mockHttpService.Verify(s => s.DeleteAsync($"api/tourlog/{logId}"), Times.Once);
                _mockToastService.Verify(t => t.ShowSuccess("Tour log deleted successfully."), Times.Once);
            });
        else
            Assert.Multiple(() =>
            {
                _mockHttpService.Verify(s => s.DeleteAsync(It.IsAny<string>()), Times.Never);
                _mockToastService.Verify(t => t.ShowSuccess(It.IsAny<string>()), Times.Never);
            });
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
    public void ShowAddLogForm_WithValidTourId_ShouldExecuteLambdaAndCreateTourLog()
    {
        var tourId = Guid.NewGuid();
        _viewModel.SelectedTourId = tourId;
        TourLog? capturedTourLog = null;


        _mockViewModelHelperService
            .Setup(v => v.ResetForm(ref It.Ref<TourLog>.IsAny, It.IsAny<Func<TourLog>>()))
            .Callback(new ResetFormCallback((ref TourLog tourLog, Func<TourLog> factory) =>
            {
                capturedTourLog = factory();
                tourLog = capturedTourLog;
            }));


        _viewModel.ShowAddLogForm();


        Assert.Multiple(() =>
        {
            Assert.That(capturedTourLog, Is.Not.Null);
            Assert.That(capturedTourLog!.TourId, Is.EqualTo(tourId));
            Assert.That(_viewModel.IsEditing, Is.False);
        });
    }

    [Test]
    public void ShowAddLogForm_WithNullTourId_ShouldReturnEarly()
    {
        _viewModel.SelectedTourId = null;


        _viewModel.ShowAddLogForm();


        _mockViewModelHelperService.Verify(
            v => v.ResetForm(ref It.Ref<TourLog>.IsAny, It.IsAny<Func<TourLog>>()),
            Times.Never);
    }

    [Test]
    public void ResetForm_ShouldExecuteLambdaAndCreateTourLog()
    {
        var tourId = Guid.NewGuid();
        _viewModel.SelectedTourId = tourId;
        TourLog? capturedTourLog = null;


        _mockViewModelHelperService
            .Setup(v => v.ResetForm(ref It.Ref<TourLog>.IsAny, It.IsAny<Func<TourLog>>()))
            .Callback(new ResetFormCallback((ref TourLog tourLog, Func<TourLog> factory) =>
            {
                capturedTourLog = factory();
                tourLog = capturedTourLog;
            }));


        _viewModel.ResetForm();


        Assert.Multiple(() =>
        {
            Assert.That(capturedTourLog, Is.Not.Null);
            Assert.That(capturedTourLog!.TourId, Is.EqualTo(tourId));
            Assert.That(_viewModel.IsLogFormVisible, Is.False);
            Assert.That(_viewModel.IsEditing, Is.False);
        });
    }

    [Test]
    public void ResetForm_WithNullTourId_ShouldUseFallbackGuid()
    {
        _viewModel.SelectedTourId = null;
        TourLog? capturedTourLog = null;


        _mockViewModelHelperService
            .Setup(v => v.ResetForm(ref It.Ref<TourLog>.IsAny, It.IsAny<Func<TourLog>>()))
            .Callback(new ResetFormCallback((ref TourLog tourLog, Func<TourLog> factory) =>
            {
                capturedTourLog = factory();
                tourLog = capturedTourLog;
            }));


        _viewModel.ResetForm();


        Assert.Multiple(() =>
        {
            Assert.That(capturedTourLog, Is.Not.Null);
            Assert.That(capturedTourLog!.TourId, Is.EqualTo(Guid.Empty));
        });
    }


    public delegate void ResetFormCallback(ref TourLog tourLog, Func<TourLog> factory);
}