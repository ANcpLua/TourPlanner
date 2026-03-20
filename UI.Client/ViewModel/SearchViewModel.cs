using System.Collections.ObjectModel;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using UI.Decorator;
using UI.Model;
using UI.Service.Interface;
using UI.ViewModel.Base;

namespace UI.ViewModel;

public class SearchViewModel(
    IHttpService httpService,
    IToastServiceWrapper toastServiceWrapper,
    TryCatchToastWrapper tryCatchToastWrapper,
    NavigationManager navigationManager)
    : BaseViewModel(httpService, toastServiceWrapper, tryCatchToastWrapper)
{
    public string SearchText
    {
        get;
        set => SetProperty(ref field, value);
    } = "";

    public ObservableCollection<Tour> SearchResults
    {
        get;
        set => SetProperty(ref field, value);
    } = [];

    [UiMethodDecorator]
    public Task SearchToursAsync()
    {
        return HandleApiRequestAsync(
            async () =>
            {
                if (string.IsNullOrWhiteSpace(SearchText))
                {
                    SearchResults.Clear();
                    return;
                }

                var tours = await HttpService.GetListAsync<Tour>(
                    $"api/tour/search/{SearchText.Trim()}"
                );
                SearchResults = new ObservableCollection<Tour>(tours ?? []);

                if (!SearchResults.Any())
                    ToastServiceWrapper.ShowSuccess(
                        "No tours found matching your search criteria."
                    );
            },
            "Error searching tours"
        );
    }

    public void ClearSearch()
    {
        SearchText = "";
        SearchResults.Clear();
    }

    public void NavigateToTour(Guid tourId)
    {
        navigationManager.NavigateTo($"/?tourId={tourId}");
    }

    public async Task HandleKeyPress(KeyboardEventArgs e)
    {
        if (e.Key == "Enter") await SearchToursAsync();
    }
}
