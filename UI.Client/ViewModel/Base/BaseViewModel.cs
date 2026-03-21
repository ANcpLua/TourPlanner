using System.ComponentModel;
using System.Runtime.CompilerServices;
using UI.Decorator;
using UI.Service.Interface;

namespace UI.ViewModel.Base;

public abstract class BaseViewModel(
    HttpClient httpClient,
    IToastServiceWrapper toastServiceWrapper,
    TryCatchToastWrapper tryCatchToastWrapper) : INotifyPropertyChanged
{
    protected HttpClient HttpClient { get; } = httpClient;
    public IToastServiceWrapper ToastServiceWrapper { get; } = toastServiceWrapper;

    public bool IsProcessing
    {
        get;
        set => SetProperty(ref field, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected async Task ExecuteAsync(Func<Task> action, string errorContext)
    {
        if (IsProcessing) return;

        try
        {
            IsProcessing = true;
            await tryCatchToastWrapper.ExecuteAsync(action, errorContext);
        }
        finally
        {
            IsProcessing = false;
        }
    }

    protected async Task<T?> ExecuteAsync<T>(Func<Task<T>> action, string errorContext)
    {
        if (IsProcessing) return default;

        try
        {
            IsProcessing = true;
            return await tryCatchToastWrapper.ExecuteAsync(action, errorContext);
        }
        finally
        {
            IsProcessing = false;
        }
    }

    protected Task HandleApiRequestAsync(Func<Task> action, string errorContext) =>
        tryCatchToastWrapper.ExecuteAsync(action, errorContext);

    protected bool SetProperty<T>(
        ref T field,
        T value,
        [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
