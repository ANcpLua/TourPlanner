using System.ComponentModel;
using System.Runtime.CompilerServices;
using UI.Decorator;
using UI.Service.Interface;

namespace UI.ViewModel.Base;

public abstract class BaseViewModel(
    IHttpService httpService,
    IToastServiceWrapper toastServiceWrapper,
    TryCatchToastWrapper tryCatchToastWrapper) : INotifyPropertyChanged
{
    public IHttpService HttpService { get; } = httpService;
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

    public T Process<T>(Func<T> func)
    {
        if (IsProcessing) return default!;

        try
        {
            IsProcessing = true;
            return func();
        }
        finally
        {
            IsProcessing = false;
        }
    }

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

    public Task<T?> HandleApiRequestAsync<T>(Func<Task<T>> apiCall, string errorMessage)
    {
        return tryCatchToastWrapper.ExecuteAsync(apiCall, errorMessage);
    }

    protected Task HandleApiRequestAsync(Func<Task> apiCall, string errorMessage)
    {
        return tryCatchToastWrapper.ExecuteAsync(apiCall, errorMessage);
    }
}
