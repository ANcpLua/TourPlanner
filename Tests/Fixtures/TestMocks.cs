using System.Net;
using System.Net.Http.Json;
using BL.Interface;
using UI.Decorator;
using UI.Service.Interface;
using UI.ViewModel;

namespace Tests.Fixtures;

public static class TestMocks
{
    public static Mock<IUserContext> UserContext()
    {
        var mock = new Mock<IUserContext>();
        mock.Setup(static u => u.UserId).Returns(TestConstants.TestUserId);
        return mock;
    }

    public static Mock<ILogger> Logger() => new();

    public static Mock<IJSRuntime> JsRuntime() => new();

    public static Mock<IBlazorDownloadFileService> BlazorDownloadFileService() => new();

    public static Mock<IRouteApiService> RouteApiService() => new();

    public static Mock<IToastServiceWrapper> ToastService()
    {
        var mock = new Mock<IToastServiceWrapper>();
        mock.Setup(static t => t.ShowSuccess(It.IsAny<string>()));
        mock.Setup(static t => t.ShowError(It.IsAny<string>()));
        return mock;
    }

    public static Mock<IConfiguration> Configuration()
    {
        var mock = new Mock<IConfiguration>();
        mock.Setup(static c => c["AppSettings:ImageBasePath"]).Returns("/images/");
        return mock;
    }

    public static Mock<MapViewModel> MapViewModel()
    {
        return new Mock<MapViewModel>(
            JsRuntime().Object,
            new HttpClient(),
            ToastService().Object,
            TryCatchToastWrapper())
        {
            DefaultValue = DefaultValue.Mock,
            CallBase = false
        };
    }

    public static TryCatchToastWrapper TryCatchToastWrapper(IToastServiceWrapper? toastService = null)
    {
        return new TryCatchToastWrapper(toastService ?? ToastService().Object, Logger().Object);
    }

    public static Mock<IBrowserFile> BrowserFile(string content)
    {
        var mock = new Mock<IBrowserFile>();
        mock.Setup(static f => f.OpenReadStream(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .Returns(new MemoryStream(Encoding.UTF8.GetBytes(content)));
        return mock;
    }

    public static InputFileChangeEventArgs MakeFile(string content)
    {
        return new InputFileChangeEventArgs([BrowserFile(content).Object]);
    }
}
