using System.Collections.ObjectModel;
using System.Net.Http.Json;
using System.Text;
using Blazor.DownloadFileFast.Interfaces;
using Contracts.Reports;
using Microsoft.AspNetCore.Components.Forms;
using UI.Decorator;
using UI.Model;
using UI.Service.Interface;
using UI.ViewModel.Base;

namespace UI.ViewModel;

public class ReportViewModel(
    HttpClient httpClient,
    IToastServiceWrapper toastServiceWrapper,
    TryCatchToastWrapper tryCatchToastWrapper,
    IBlazorDownloadFileService blazorDownloadFile)
    : BaseViewModel(httpClient, toastServiceWrapper, tryCatchToastWrapper)
{
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

    public string SummaryButtonText => IsProcessing ? "Generating..." : "Generate Summary";
    public string ExportButtonText => IsProcessing ? "Exporting XML..." : "Export XML";

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
                var tours = await HttpClient.GetFromJsonAsync<List<Tour>>("api/tour");
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
        return ExecuteAsync(async () =>
        {
            if (SelectedDetailedTourId == Guid.Empty) return;
            await GenerateAndDownloadReport($"api/reports/tour/{SelectedDetailedTourId}", "DetailedReport");
        }, "Error generating detailed report");
    }

    [UiMethodDecorator]
    public Task GenerateSummaryReportAsync()
    {
        return ExecuteAsync(
            async () => await GenerateAndDownloadReport("api/reports/summary", "SummaryReport"),
            "Error generating summary report");
    }

    [UiMethodDecorator]
    public async Task GenerateAndDownloadReport(string uri, string reportType)
    {
        var reportBytes = await HttpClient.GetByteArrayAsync(uri);
        var fileName = $"{reportType}_{TimeProvider.System.GetUtcNow().UtcDateTime:yyyyMMdd_HHmmss}.pdf";
        if (reportBytes.Length is 0)
        {
            ToastServiceWrapper.ShowError($"Error generating {reportType}: No data received.");
            return;
        }

        await blazorDownloadFile.DownloadFileAsync(
            fileName,
            reportBytes,
            "application/pdf"
        );
        OnPropertyChanged(nameof(CurrentReportUrl));

        ToastServiceWrapper.ShowSuccess($"{reportType} generated successfully.");
    }

    [UiMethodDecorator]
    public Task ExportTourToXmlAsync(Guid tourId)
    {
        return ExecuteAsync(async () =>
        {
            var xml = await HttpClient.GetStringAsync($"api/reports/export/{tourId}");
            if (string.IsNullOrWhiteSpace(xml))
            {
                ToastServiceWrapper.ShowError("Error exporting tour XML: No data received.");
                return;
            }

            var fileName = $"Tour_{tourId}_{TimeProvider.System.GetUtcNow().UtcDateTime:yyyyMMdd_HHmmss}.xml";
            await blazorDownloadFile.DownloadFileAsync(
                fileName,
                Encoding.UTF8.GetBytes(xml),
                "application/xml"
            );
            ToastServiceWrapper.ShowSuccess("Tour XML exported successfully.");
        }, "Error exporting tour XML");
    }

    [UiMethodDecorator]
    public Task ImportTourFromXmlAsync(InputFileChangeEventArgs e)
    {
        return HandleApiRequestAsync(
            async () =>
            {
                await using var stream = e.File.OpenReadStream(
                    TourXmlDocument.MaximumDocumentCharacters * 4L);
                using var reader = new StreamReader(stream);
                var xml = await reader.ReadToEndAsync();
                using var response = await HttpClient.PostAsJsonAsync(
                    "api/reports/import",
                    new ImportTourRequest { Xml = xml });
                response.EnsureSuccessStatusCode();
                await LoadToursAsync();
                ToastServiceWrapper.ShowSuccess("Tour XML imported successfully.");
            },
            "Error importing tour XML"
        );
    }
}
