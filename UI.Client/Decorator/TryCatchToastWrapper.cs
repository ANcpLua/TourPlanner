using UI.Service.Interface;
using ILogger = Serilog.ILogger;

namespace UI.Decorator;

public class TryCatchToastWrapper(IToastServiceWrapper toastServiceWrapper, ILogger logger)
{
    public async Task<T?> ExecuteAsync<T>(
        Func<Task<T>> action,
        string errorMessage
    )
    {
        try
        {
            return await action();
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Operation failed: {ErrorContext}", errorMessage);
            toastServiceWrapper.ShowError($"{errorMessage}: {ex.Message}");
            return default;
        }
    }

    public async Task ExecuteAsync(
        Func<Task> action,
        string errorMessage
    )
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Operation failed: {ErrorContext}", errorMessage);
            toastServiceWrapper.ShowError($"{errorMessage}: {ex.Message}");
        }
    }
}
