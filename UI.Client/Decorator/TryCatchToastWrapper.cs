using UI.Service.Interface;
using ILogger = Serilog.ILogger;

namespace UI.Decorator;

public class TryCatchToastWrapper(IToastServiceWrapper toastServiceWrapper, ILogger logger)
{
    public async Task<T?> ExecuteAsync<T>(
        Func<Task<T>> action,
        string errorMessage,
        Action<Exception>? errorHandler = null
    )
    {
        try
        {
            return await action();
        }
        catch (Exception ex)
        {
            errorHandler?.Invoke(ex);
            logger.Error(ex, "Operation failed: {ErrorContext}", errorMessage);
            toastServiceWrapper.ShowError($"{errorMessage}: {ex.Message}");
            return default;
        }
    }

    public async Task ExecuteAsync(
        Func<Task> action,
        string errorMessage,
        Action<Exception>? errorHandler = null
    )
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            errorHandler?.Invoke(ex);
            logger.Error(ex, "Operation failed: {ErrorContext}", errorMessage);
            toastServiceWrapper.ShowError($"{errorMessage}: {ex.Message}");
        }
    }
}