using System.Collections.ObjectModel;
using System.Net.Http.Json;
using Microsoft.JSInterop;
using UI.Decorator;
using UI.Model;
using UI.Service.Interface;
using UI.ViewModel.Base;

namespace UI.ViewModel;

public class TourLogViewModel(
    HttpClient httpClient,
    IToastServiceWrapper toastServiceWrapper,
    TryCatchToastWrapper tryCatchToastWrapper,
    IJSRuntime jsRuntime)
    : BaseViewModel(httpClient, toastServiceWrapper, tryCatchToastWrapper)
{
    private Guid? _selectedTourId;

    public ObservableCollection<TourLog> TourLogs { get; set; } = [];

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

    public string LogFormToggleButtonText => IsLogFormVisible ? "Hide Form" : "Add New Log";
    public string LogFormTitle => SelectedTourLog.Id == Guid.Empty ? "Add New Log" : "Edit Log";
    public string EditLogButtonText(Guid logId) => IsEditing && IsLogFormVisible && SelectedTourLog.Id == logId ? "Hide Edit Form" : "Edit";

    public bool IsFormValid =>
        !string.IsNullOrWhiteSpace(SelectedTourLog.Comment) &&
        SelectedTourLog.Difficulty is >= 1 and <= 5 &&
        SelectedTourLog is { TotalDistance: > 0, TotalTime: > 0, Rating: >= 1 and <= 5 };

    public void ClearTourData()
    {
        TourLogs.Clear();
        ResetForm();
    }

    public void ShowAddLogForm()
    {
        if (!SelectedTourId.HasValue) return;

        SelectedTourLog = CreateEmptyLog();
        IsLogFormVisible = true;
        IsEditing = false;
    }

    public void ResetForm()
    {
        SelectedTourLog = CreateEmptyLog();
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
    public async Task LoadTourLogsAsync()
    {
        if (!SelectedTourId.HasValue) return;

        await HandleApiRequestAsync(async () =>
        {
            var logs = await HttpClient.GetFromJsonAsync<List<TourLog>>(
                $"api/tourlog/bytour/{SelectedTourId}");
            TourLogs.Clear();
            foreach (var log in logs ?? []) TourLogs.Add(log);
            ResetForm();
        }, "Error loading tour logs");
    }

    [UiMethodDecorator]
    public async Task<bool> SaveTourLogAsync()
    {
        if (!IsFormValid || !SelectedTourId.HasValue)
        {
            ToastServiceWrapper.ShowError("Not Valid.");
            return false;
        }

        return await ExecuteAsync(async () =>
        {
            await PersistTourLogAsync();
            await LoadTourLogsAsync();
            ResetForm();
            return true;
        }, "Error saving tour log") is true;
    }

    [UiMethodDecorator]
    public async Task EditHandleTourLogAction(Guid? logId = null)
    {
        if (!logId.HasValue || logId == Guid.Empty)
        {
            ToggleLogForm();
            return;
        }

        if (IsEditing && IsLogFormVisible && SelectedTourLog.Id == logId)
        {
            ResetForm();
            return;
        }

        await HandleApiRequestAsync(async () =>
        {
            var log = await HttpClient.GetFromJsonAsync<TourLog>($"api/tourlog/{logId}");
            if (log is null)
            {
                ToastServiceWrapper.ShowError("Tour log not found.");
                return;
            }

            SelectedTourLog = log;
            SelectedTourId = log.TourId;
            IsLogFormVisible = true;
            IsEditing = true;
        }, "Error loading tour log for editing");
    }

    [UiMethodDecorator]
    public async Task DeleteTourLogAsync(Guid logId)
    {
        if (logId == Guid.Empty) return;

        var confirmed = await jsRuntime.InvokeAsync<bool>(
            "confirm", "Are you sure you want to delete this tour log?");

        if (!confirmed) return;

        await ExecuteAsync(async () =>
        {
            (await HttpClient.DeleteAsync($"api/tourlog/{logId}")).EnsureSuccessStatusCode();
            await LoadTourLogsAsync();
            ToastServiceWrapper.ShowSuccess("Tour log deleted successfully.");
        }, "Error deleting tour log");
    }

    private async Task HandleTourSelection()
    {
        if (_selectedTourId is null || _selectedTourId == Guid.Empty)
            ClearTourData();
        else
            await LoadTourLogsAsync();
    }

    private async Task PersistTourLogAsync()
    {
        if (SelectedTourLog.Id == Guid.Empty)
        {
            (await HttpClient.PostAsJsonAsync("api/tourlog", SelectedTourLog)).EnsureSuccessStatusCode();
            ToastServiceWrapper.ShowSuccess("Tour log created successfully.");
        }
        else
        {
            (await HttpClient.PutAsJsonAsync($"api/tourlog/{SelectedTourLog.Id}", SelectedTourLog)).EnsureSuccessStatusCode();
            ToastServiceWrapper.ShowSuccess("Tour log updated successfully.");
        }
    }

    private TourLog CreateEmptyLog() => new()
    {
        TourId = SelectedTourId ?? Guid.Empty,
        DateTime = TimeProvider.System.GetUtcNow().UtcDateTime,
        Difficulty = 1,
        Rating = 1
    };
}
