using System.Collections.ObjectModel;
using System.Text;
using System.Text.Json;
using Blazor.DownloadFileFast.Interfaces;
using Microsoft.AspNetCore.Components.Forms;
using UI.Decorator;
using UI.Model;
using UI.Service.Interface;
using UI.ViewModel.Base;
using ILogger = Serilog.ILogger;

namespace UI.ViewModel;

public class ReportViewModel : BaseViewModel
{
    private static readonly JsonSerializerOptions CamelCaseOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IBlazorDownloadFileService _blazorDownloadFile;

    public ReportViewModel(
        IHttpService httpService,
        IToastServiceWrapper toastServiceWrapper,
        ILogger logger,
        IBlazorDownloadFileService blazorDownloadFile
    )
        : base(httpService, toastServiceWrapper, logger)
    {
        _blazorDownloadFile = blazorDownloadFile;
    }

    public string CurrentReportUrl
    {
        get;
        set => SetProperty(ref field, value);
    } = "";

    public Guid SelectedDetailedTourId
    {
        get;
        set => SetProperty(ref field, value);
    } = Guid.Empty;

    public ObservableCollection<Tour> Tours
    {
        get;
        private set => SetProperty(ref field, value);
    } = [];

    [UiMethodDecorator]
    public Task InitializeAsync()
    {
        return LoadToursAsync();
    }

    private Task LoadToursAsync()
    {
        return HandleApiRequestAsync(
            async () =>
            {
                var tours = await HttpService.GetListAsync<Tour>("api/tour");
                Tours = new ObservableCollection<Tour>(tours ?? []);
            },
            "Error loading tours"
        );
    }

    public void ClearCurrentReport()
    {
        CurrentReportUrl = "";
    }

    [UiMethodDecorator]
    public Task GenerateDetailedReportAsync()
    {
        return Process(async () =>
        {
            await HandleApiRequestAsync(
                async () =>
                {
                    if (SelectedDetailedTourId == Guid.Empty) return;

                    await GenerateAndDownloadReport($"api/reports/tour/{SelectedDetailedTourId}", "DetailedReport");
                },
                "Error generating detailed report"
            );
        });
    }

    [UiMethodDecorator]
    public Task GenerateSummaryReportAsync()
    {
        return Process(async () =>
        {
            await HandleApiRequestAsync(
                async () => await GenerateAndDownloadReport("api/reports/summary", "SummaryReport"),
                "Error generating summary report"
            );
        });
    }

    [UiMethodDecorator]
    public async Task GenerateAndDownloadReport(string uri, string reportType)
    {
        var reportBytes = await HttpService.GetByteArrayAsync(uri);
        var fileName = $"{reportType}_{TimeProvider.System.GetUtcNow().UtcDateTime:yyyyMMdd_HHmmss}.pdf";
        if (reportBytes is null || reportBytes.Length is 0)
        {
            ToastServiceWrapper.ShowError($"Error generating {reportType}: No data received.");
            return;
        }

        await _blazorDownloadFile.DownloadFileAsync(
            fileName,
            reportBytes,
            "application/pdf"
        );
        OnPropertyChanged(nameof(CurrentReportUrl));

        ToastServiceWrapper.ShowSuccess($"{reportType} generated successfully.");
    }

    [UiMethodDecorator]
    public Task ExportTourToJsonAsync(Guid tourId)
    {
        return Process(async () =>
        {
            await HandleApiRequestAsync(
                async () =>
                {
                    var json = await HttpService.GetStringAsync($"api/reports/export/{tourId}");
                    if (string.IsNullOrEmpty(json))
                    {
                        ToastServiceWrapper.ShowError("Error exporting tour: Invalid tour data.");
                        return;
                    }

                    var fileName = $"Tour_{tourId}_{TimeProvider.System.GetUtcNow().UtcDateTime:yyyyMMdd_HHmmss}.json";
                    await _blazorDownloadFile.DownloadFileAsync(
                        fileName,
                        Encoding.UTF8.GetBytes(json),
                        "application/json"
                    );
                    ToastServiceWrapper.ShowSuccess("Tour exported successfully.");
                },
                "Error exporting tour"
            );
        });
    }

    [UiMethodDecorator]
    public Task ImportTourFromJsonAsync(InputFileChangeEventArgs e)
    {
        return HandleApiRequestAsync(
            async () =>
            {
                await using var stream = e.File.OpenReadStream();
                using var reader = new StreamReader(stream);
                var json = await reader.ReadToEndAsync();

                var tour = JsonSerializer.Deserialize<Tour>(json, CamelCaseOptions);

                if (tour is null)
                {
                    ToastServiceWrapper.ShowError("Error importing tour: Invalid tour data.");
                    return;
                }

                var existingTour = Tours.FirstOrDefault(t => t.Id == tour.Id);
                if (existingTour is not null)
                {
                    ToastServiceWrapper.ShowError($"Tour already exists. Delete {existingTour.Name} first.");
                    return;
                }

                await HttpService.PostAsync("api/tour", tour);
                await LoadToursAsync();
                ToastServiceWrapper.ShowSuccess("Tour imported successfully.");
            },
            "Error importing tour"
        );
    }
}