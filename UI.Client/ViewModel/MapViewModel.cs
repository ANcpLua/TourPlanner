using System.Collections.Frozen;
using System.Collections.ObjectModel;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using UI.Decorator;
using UI.Service.Interface;
using UI.ViewModel.Base;

namespace UI.ViewModel;

public class MapViewModel(
    IJSRuntime jsRuntime,
    HttpClient httpClient,
    IToastServiceWrapper toastServiceWrapper,
    TryCatchToastWrapper tryCatchToastWrapper)
    : BaseViewModel(httpClient, toastServiceWrapper, tryCatchToastWrapper)
{
    public static readonly FrozenDictionary<string, (double Latitude, double Longitude)> Coordinates =
        new Dictionary<string, (double Latitude, double Longitude)>
        {
            ["Vienna"] = (48.2082, 16.3738),
            ["Paris"] = (48.8566, 2.3522),
            ["Berlin"] = (52.5200, 13.4050),
            ["Budapest"] = (47.4979, 19.0402),
            ["Warsaw"] = (52.2297, 21.0122)
        }.ToFrozenDictionary();

    private bool _isMapInitialized;
    private string _toCity = "";

    public ObservableCollection<string> CityNames { get; } = new(Coordinates.Keys);

    public string FromCity
    {
        get;
        set
        {
            if (!SetProperty(ref field, value)) return;
            OnPropertyChanged(nameof(FilteredToCities));
            if (_toCity == field) ToCity = "";
        }
    } = "";

    public string ToCity
    {
        get => _toCity;
        set => SetProperty(ref _toCity, value);
    }

    public IEnumerable<string> FilteredToCities => CityNames.Where(city => city != FromCity);

    public async Task InitializeMapAsync(ElementReference mapElement)
    {
        await jsRuntime.InvokeVoidAsync("TourPlannerMap.initializeMap", mapElement);
        _isMapInitialized = true;
    }

    public Task ShowMapAsync()
    {
        return ExecuteAsync(async () =>
        {
            if (!_isMapInitialized)
            {
                ToastServiceWrapper.ShowError("Map is not initialized yet.");
                return;
            }

            if (string.IsNullOrWhiteSpace(FromCity) || string.IsNullOrWhiteSpace(ToCity))
            {
                ToastServiceWrapper.ShowError("Please select both cities.");
                return;
            }

            var fromCoords = GetCoordinates(FromCity);
            var toCoords = GetCoordinates(ToCity);

            if (fromCoords.HasValue && toCoords.HasValue)
            {
                await jsRuntime.InvokeVoidAsync(
                    "TourPlannerMap.setRoute",
                    fromCoords.Value.Latitude,
                    fromCoords.Value.Longitude,
                    toCoords.Value.Latitude,
                    toCoords.Value.Longitude
                );
            }
        }, "Error showing map");
    }

    public async Task ClearMapAsync()
    {
        await jsRuntime.InvokeVoidAsync("TourPlannerMap.clearMap");
        FromCity = "";
        ToCity = "";
        OnPropertyChanged(nameof(FromCity));
        OnPropertyChanged(nameof(ToCity));
    }

    public virtual (double Latitude, double Longitude)? GetCoordinates(string city)
    {
        return Coordinates.TryGetValue(city, out var coords) ? coords : null;
    }
}
