using System.ComponentModel;
using System.Runtime.CompilerServices;
using UI.Decorator;
using UI.Service.Interface;
using ILogger = Serilog.ILogger;

namespace UI.ViewModel.Base;

public abstract class BaseViewModel : INotifyPropertyChanged
{
    public readonly IHttpService HttpService;
    public readonly IToastServiceWrapper ToastServiceWrapper;
    private readonly TryCatchToastWrapper _tryCatchToastWrapper;

    protected BaseViewModel(
        IHttpService httpService,
        IToastServiceWrapper toastServiceWrapper,
        ILogger logger)
    {
        HttpService = httpService;
        ToastServiceWrapper = toastServiceWrapper;
        _tryCatchToastWrapper = new TryCatchToastWrapper(toastServiceWrapper, logger);
    }

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
        return _tryCatchToastWrapper.ExecuteAsync(apiCall, errorMessage);
    }

    protected Task HandleApiRequestAsync(Func<Task> apiCall, string errorMessage)
    {
        return _tryCatchToastWrapper.ExecuteAsync(apiCall, errorMessage);
    }
}