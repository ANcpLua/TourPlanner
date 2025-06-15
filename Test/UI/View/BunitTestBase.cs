using UI.Model;
using UI.ViewModel;

namespace Test.UI.View;

public abstract class BunitTestBase : TestContextWrapper
{
    [SetUp]
    public void BaseSetUp()
    {
        TestContext = new TestContext();
        JSInterop.Mode = JSRuntimeMode.Loose;

        RegisterServices();
        OnSetup();
    }

    protected virtual void OnSetup()
    {
    }

    private void RegisterServices()
    {
        var http = TestData.MockHttpService();
        var toast = TestData.MockToastService();
        var logger = TestData.MockLogger();
        var config = TestData.MockConfiguration();
        var route = TestData.MockRouteApiService();
        var download = TestData.MockBlazorDownloadFileService();

        http.Setup(s => s.GetListAsync<Tour>("api/tour"))
            .ReturnsAsync(TestData.SampleTourList(2));

        Services.AddSingleton(http.Object);
        Services.AddSingleton(toast.Object);
        Services.AddSingleton(logger.Object);
        Services.AddSingleton(config.Object);
        Services.AddSingleton(route.Object);

        Services.AddSingleton(download.Object);
        Services.AddSingleton(new Mock<IToastService>().Object);

        Services.AddSingleton(http);
        Services.AddSingleton(toast);
        Services.AddSingleton(logger);
        Services.AddSingleton(config);
        Services.AddSingleton(route);
        Services.AddSingleton(download);

        typeof(TourViewModel).Assembly
            .GetTypes()
            .Where(t => t.Name.EndsWith("ViewModel") && t is { IsClass: true, IsAbstract: false })
            .ToList()
            .ForEach(vm => Services.AddScoped(vm));
    }
}

public static class TestExtensions
{
    public static T ViewModel<T>(this IServiceProvider services) where T : class
    {
        return services.GetRequiredService<T>();
    }

    public static Mock<T> Mock<T>(this IServiceProvider services) where T : class
    {
        return services.GetRequiredService<Mock<T>>();
    }
}