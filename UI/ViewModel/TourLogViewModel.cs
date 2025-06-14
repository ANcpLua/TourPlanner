using System.Collections.ObjectModel;
using Microsoft.JSInterop;
using UI.Decorator;
using UI.Model;
using UI.Service.Interface;
using UI.ViewModel.Base;
using ILogger = Serilog.ILogger;

namespace UI.ViewModel;

public class TourLogViewModel : BaseViewModel
{
    private readonly IJSRuntime _jsRuntime;
    private Guid? _selectedTourId;

    public TourLogViewModel(
        IHttpService httpService,
        IToastServiceWrapper toastServiceWrapper,
        IJSRuntime jsRuntime,
        ILogger logger
    )
        : base(httpService, toastServiceWrapper, logger)
    {
        TourLogs = [];
        _jsRuntime = jsRuntime;
    }

    public ObservableCollection<TourLog> TourLogs { get; set; }

    public TourLog SelectedTourLog
    {
        get;
        set => SetProperty(ref field, value);
    } = new();

    public bool IsLogFormVisible
    {
        get;
        set => SetProperty(ref field, value);
    }

    public bool IsEditing
    {
        get;
        set => SetProperty(ref field, value);
    }

    public Guid? SelectedTourId
    {
        get => _selectedTourId;
        set
        {
            if (SetProperty(ref _selectedTourId, value)) _ = HandleTourSelection();
        }
    }

    public bool IsFormValid =>
        !string.IsNullOrWhiteSpace(SelectedTourLog.Comment) &&
        SelectedTourLog.Difficulty is >= 1 and <= 5 &&
        SelectedTourLog is { TotalDistance: > 0, TotalTime: > 0, Rating: >= 1 and <= 5 };

    private Task HandleTourSelection()
    {
        return HandleApiRequestAsync(
            async () =>
            {
                if (_selectedTourId == null || _selectedTourId == Guid.Empty)
                    ClearTourData();
                else
                    await LoadTourLogsAsync();
            },
            "Error handling tour selection"
        );
    }

    public void ClearTourData()
    {
        TourLogs.Clear();
        ResetForm();
    }

    public void ShowAddLogForm()
    {
        if (!SelectedTourId.HasValue) return;

        SelectedTourLog = new TourLog
        {
            TourId = SelectedTourId.Value,
            DateTime = DateTime.Now,
            Difficulty = 1,
            Rating = 1
        };
        IsLogFormVisible = true;
        IsEditing = false;
    }

    public void ResetForm()
    {
        SelectedTourLog = new TourLog
        {
            TourId = SelectedTourId ?? Guid.Empty,
            DateTime = DateTime.Now,
            Difficulty = 1,
            Rating = 1
        };
        IsLogFormVisible = false;
        IsEditing = false;
    }

    public void ToggleLogForm()
    {
        if (IsLogFormVisible)
            ResetForm();
        else
            ShowAddLogForm();
    }

    [UiMethodDecorator]
    public Task LoadTourLogsAsync()
    {
        return HandleApiRequestAsync(
            async () =>
            {
                if (!SelectedTourId.HasValue) return;

                var logs = await HttpService.GetListAsync<TourLog>(
                    $"api/tourlog/bytour/{SelectedTourId}"
                );
                TourLogs.Clear();
                foreach (var log in logs ?? []) TourLogs.Add(log);

                ResetForm();
            },
            "Error loading tour logs"
        );
    }

    [UiMethodDecorator]
    public Task<bool> SaveTourLogAsync()
    {
        return HandleApiRequestAsync(
            async () =>
            {
                if (!IsFormValid || !SelectedTourId.HasValue)
                {
                    ToastServiceWrapper.ShowError("Not Valid.");
                    return false;
                }

                if (SelectedTourLog.Id == Guid.Empty)
                {
                    await HttpService.PostAsync<TourLog>("api/tourlog", SelectedTourLog);
                    ToastServiceWrapper.ShowSuccess("Tour log created successfully.");
                }
                else
                {
                    await HttpService.PutAsync<TourLog>(
                        $"api/tourlog/{SelectedTourLog.Id}",
                        SelectedTourLog
                    );
                    ToastServiceWrapper.ShowSuccess("Tour log updated successfully.");
                }

                await LoadTourLogsAsync();
                ResetForm();
                return true;
            },
            "Error saving tour log"
        );
    }

    [UiMethodDecorator]
    private Task EditTourLogAsync(Guid logId)
    {
        return HandleApiRequestAsync(
            async () =>
            {
                var log = await HttpService.GetAsync<TourLog>($"api/tourlog/{logId}");
                if (log != null)
                {
                    SelectedTourLog = log;
                    SelectedTourId = log.TourId;
                    IsLogFormVisible = true;
                    IsEditing = true;
                }
            },
            "Error loading tour log for editing"
        );
    }

    [UiMethodDecorator]
    public Task EditHandleTourLogAction(Guid? logId = null)
    {
        return HandleApiRequestAsync(
            async () =>
            {
                if (logId.HasValue && logId != Guid.Empty)
                {
                    if (IsEditing && IsLogFormVisible && SelectedTourLog.Id == logId)
                        ResetForm();
                    else
                        await EditTourLogAsync(logId.Value);
                }
                else
                {
                    ToggleLogForm();
                }
            },
            "Error handling tour log action"
        );
    }

    [UiMethodDecorator]
    public async Task DeleteTourLogAsync(Guid logId)
    {
        var confirmed = await _jsRuntime.InvokeAsync<bool>(
            "confirm",
            "Are you sure you want to delete this tour log?"
        );
        if (confirmed)
            await HandleApiRequestAsync(
                async () =>
                {
                    if (logId == Guid.Empty) return;
                    await HttpService.DeleteAsync($"api/tourlog/{logId}");
                    await LoadTourLogsAsync();
                    ToastServiceWrapper.ShowSuccess("Tour log deleted successfully.");
                },
                "Error deleting tour log"
            );
    }
}