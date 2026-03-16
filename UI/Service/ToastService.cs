using Blazored.Toast.Services;
using UI.Service.Interface;

namespace UI.Service;

public class ToastService(IToastService toastService) : IToastServiceWrapper
{
    public void ShowSuccess(string message)
    {
        toastService.ShowSuccess(message);
    }

    public void ShowError(string message)
    {
        toastService.ShowError(message);
    }
}