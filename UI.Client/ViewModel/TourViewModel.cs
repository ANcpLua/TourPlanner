using System.Collections.ObjectModel;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.JSInterop;
using UI.Decorator;
using UI.Model;
using UI.Service.Interface;
using UI.ViewModel.Base;

namespace UI.ViewModel;

public class TourViewModel(
    HttpClient httpClient,
    IToastServiceWrapper toastServiceWrapper,
    TryCatchToastWrapper tryCatchToastWrapper,
    IConfiguration configuration,
    IJSRuntime jsRuntime,
    IRouteApiService routeApiService,
    MapViewModel mapViewModel)
    : BaseViewModel(httpClient, toastServiceWrapper, tryCatchToastWrapper)
{
    public ObservableCollection<Tour> Tours { get; set; } = [];

    public bool IsFormVisible
    {
        get;
        set => SetProperty(ref field, value);
    }

    public Tour SelectedTour
    {
        get;
        set
        {
            if (!SetProperty(ref field, value)) return;
            mapViewModel.FromCity = value.From;
            mapViewModel.ToCity = value.To;
            OnPropertyChanged(nameof(FilteredToCities));
        }
    } = Tour.Empty;

    public Tour ModalTour
    {
        get;
        set => SetProperty(ref field, value);
    } = Tour.Empty;

    public bool IsMapVisible
    {
        get;
        set => SetProperty(ref field, value);
    }

    public bool IsFormValid =>
        !string.IsNullOrWhiteSpace(SelectedTour.Name) &&
        !string.IsNullOrWhiteSpace(SelectedTour.Description) &&
        !string.IsNullOrWhiteSpace(SelectedTour.From) &&
        !string.IsNullOrWhiteSpace(SelectedTour.To) &&
        !string.IsNullOrWhiteSpace(SelectedTour.TransportType);

    public IEnumerable<string> FilteredToCities => mapViewModel.CityNames.Where(city => city != SelectedTour.From);

    public string MapToggleButtonText => IsMapVisible ? "Hide Map" : "Show Map";
    public string FormToggleButtonText => IsFormVisible ? "Hide Form" : "Add Tour";
    public string SaveButtonText => IsProcessing ? "Saving..." : "Save Tour";
    public string EditButtonText(Guid tourId) => IsFormVisible && SelectedTour.Id == tourId ? "Hide Edit Form" : "Edit";

    public void ShowAddTourForm()
    {
        if (IsFormVisible)
            ResetForm();
        else
        {
            SelectedTour = Tour.Empty;
            IsFormVisible = true;
        }
    }

    public void ResetForm()
    {
        SelectedTour = Tour.Empty;
        IsFormVisible = false;
    }

    public void ToggleMap() => IsMapVisible = !IsMapVisible;

    [UiMethodDecorator]
    public async Task LoadToursAsync()
    {
        await HandleApiRequestAsync(async () =>
        {
            var tours = await HttpClient.GetFromJsonAsync<List<Tour>>("api/tour");
            Tours = new ObservableCollection<Tour>(tours ?? []);
            OnPropertyChanged(nameof(Tours));
        }, "Error loading tours");
    }

    [UiMethodDecorator]
    public async Task<bool> SaveTourAsync()
    {
        return await ExecuteAsync(async () =>
        {
            var fromCoords = ResolveCoordinates(SelectedTour.From);
            var toCoords = ResolveCoordinates(SelectedTour.To);
            if (fromCoords is null || toCoords is null) return false;

            var (distance, duration) = await routeApiService.FetchRouteDataAsync(
                fromCoords.Value, toCoords.Value, SelectedTour.TransportType);

            EnrichTourWithRouteData(fromCoords.Value, toCoords.Value, distance, duration);
            await PersistTourAsync();
            await LoadToursAsync();
            await ShowRouteOnMapAsync(fromCoords.Value, toCoords.Value);

            ResetForm();
            return true;
        }, "Error saving tour") is true;
    }

    [UiMethodDecorator]
    public async Task ShowTourDetailsAsync(Guid id)
    {
        await ExecuteAsync(async () =>
        {
            var tour = await HttpClient.GetFromJsonAsync<Tour>($"api/tour/{id}");
            if (tour is null)
            {
                ToastServiceWrapper.ShowError("Tour not found.");
                return;
            }

            ModalTour = tour;
            await jsRuntime.InvokeVoidAsync("showModal", "tourDetailsModal");
        }, "Error loading tour details");
    }

    [UiMethodDecorator]
    public async Task EditTourAsync(Guid id)
    {
        await ExecuteAsync(async () =>
        {
            if (IsFormVisible && SelectedTour.Id == id)
            {
                ResetForm();
                return;
            }

            var tour = await HttpClient.GetFromJsonAsync<Tour>($"api/tour/{id}");
            if (tour is null)
            {
                ToastServiceWrapper.ShowError("Tour not found.");
                return;
            }

            SelectedTour = tour;
            IsFormVisible = true;
        }, "Error handling tour edit action");
    }

    [UiMethodDecorator]
    public async Task DeleteTourAsync(Guid id)
    {
        var confirmed = await jsRuntime.InvokeAsync<bool>(
            "confirm", "Are you sure you want to delete this tour?");

        if (!confirmed) return;

        await ExecuteAsync(async () =>
        {
            (await HttpClient.DeleteAsync($"api/tour/{id}")).EnsureSuccessStatusCode();
            await LoadToursAsync();
            ToastServiceWrapper.ShowSuccess("Tour deleted successfully.");
        }, "Error deleting tour");
    }

    private (double Latitude, double Longitude)? ResolveCoordinates(string city) =>
        mapViewModel.GetCoordinates(city);

    private void EnrichTourWithRouteData(
        (double Latitude, double Longitude) from,
        (double Latitude, double Longitude) to,
        double distance,
        double duration)
    {
        SelectedTour.Distance = distance;
        SelectedTour.EstimatedTime = duration;
        SelectedTour.ImagePath =
            $"{configuration["AppSettings:ImageBasePath"]}{SelectedTour.From}{SelectedTour.To}.png";
        SelectedTour.RouteInformation = JsonSerializer.Serialize(new
        {
            FromCoordinates = new { from.Latitude, from.Longitude },
            ToCoordinates = new { to.Latitude, to.Longitude },
            Distance = distance,
            Duration = duration
        });
    }

    private async Task PersistTourAsync()
    {
        if (SelectedTour.Id == Guid.Empty)
        {
            (await HttpClient.PostAsJsonAsync("api/tour", SelectedTour)).EnsureSuccessStatusCode();
            ToastServiceWrapper.ShowSuccess("Tour saved successfully.");
        }
        else
        {
            (await HttpClient.PutAsJsonAsync($"api/tour/{SelectedTour.Id}", SelectedTour)).EnsureSuccessStatusCode();
            ToastServiceWrapper.ShowSuccess("Tour updated successfully.");
        }
    }

    private async Task ShowRouteOnMapAsync(
        (double Latitude, double Longitude) from,
        (double Latitude, double Longitude) to)
    {
        IsMapVisible = true;
        OnPropertyChanged(nameof(IsMapVisible));
        await jsRuntime.InvokeVoidAsync(
            "TourPlannerMap.setRoute",
            from.Latitude, from.Longitude,
            to.Latitude, to.Longitude);
    }
}
