using Microsoft.AspNetCore.Components;
using UI.Model;
using UI.ViewModel;

namespace Test.UI.View;

public abstract class BunitTestBase : IDisposable
{
    protected Bunit.BunitContext Context { get; private set; } = null!;
    protected IServiceProvider Services => Context.Services;
    protected BunitJSInterop JSInterop => Context.JSInterop;

    [SetUp]
    public void BaseSetUp()
    {
        Context = new Bunit.BunitContext();
        JSInterop.Mode = JSRuntimeMode.Loose;

        RegisterServices();
        OnSetup();
    }

    [TearDown]
    public void BaseTearDown()
    {
        Context.Dispose();
    }

    public void Dispose()
    {
        Context?.Dispose();
        GC.SuppressFinalize(this);
    }

    protected IRenderedComponent<T> RenderComponent<T>()
        where T : Microsoft.AspNetCore.Components.IComponent =>
        Context.Render<T>();

    protected IRenderedComponent<T> RenderComponent<T>(Action<ComponentParameterCollectionBuilder<T>> parameterBuilder)
        where T : Microsoft.AspNetCore.Components.IComponent =>
        Context.Render(parameterBuilder);

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

        Context.Services.AddSingleton(http.Object);
        Context.Services.AddSingleton(toast.Object);
        Context.Services.AddSingleton(logger.Object);
        Context.Services.AddSingleton(config.Object);
        Context.Services.AddSingleton(route.Object);

        Context.Services.AddSingleton(download.Object);
        Context.Services.AddSingleton(new Mock<IToastService>().Object);

        Context.Services.AddSingleton(http);
        Context.Services.AddSingleton(toast);
        Context.Services.AddSingleton(logger);
        Context.Services.AddSingleton(config);
        Context.Services.AddSingleton(route);
        Context.Services.AddSingleton(download);

        typeof(TourViewModel).Assembly
            .GetTypes()
            .Where(t => t.Name.EndsWith("ViewModel") && t is { IsClass: true, IsAbstract: false })
            .ToList()
            .ForEach(vm => Context.Services.AddScoped(vm));
    }
}

public static class TestExtensions
{
    public static T ViewModel<T>(this IServiceProvider services) where T : class =>
        services.GetRequiredService<T>();

    public static Mock<T> Mock<T>(this IServiceProvider services) where T : class =>
        services.GetRequiredService<Mock<T>>();
}
