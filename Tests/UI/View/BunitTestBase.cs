using System.Net;
using System.Security.Claims;
using UI.Auth;
using UI.Model;
using UI.Service.Interface;
using UI.ViewModel;
using IComponent = Microsoft.AspNetCore.Components.IComponent;

namespace Tests.UI.View;

public abstract class BunitTestBase
{
    protected BunitContext Context { get; private set; } = null!;
    protected IServiceProvider Services => Context.Services;
    protected BunitJSInterop JsInterop => Context.JSInterop;

    [SetUp]
    public void BaseSetUp()
    {
        Context = new BunitContext();
        JsInterop.Mode = JSRuntimeMode.Strict;
        JsInterop.SetupVoid("TourPlannerMap.initializeMap", static _ => true).SetVoidResult();
        JsInterop.SetupVoid("TourPlannerMap.setRoute", static _ => true).SetVoidResult();
        JsInterop.SetupVoid("TourPlannerMap.clearMap").SetVoidResult();

        RegisterServices();
        RegisterAuth();
        OnSetup();
    }

    [TearDown]
    public void BaseTearDown() => Context.Dispose();

    protected IRenderedComponent<T> RenderComponent<T>()
        where T : IComponent =>
        Context.Render<T>();

    protected IRenderedComponent<T> RenderComponent<T>(Action<ComponentParameterCollectionBuilder<T>> parameterBuilder)
        where T : IComponent =>
        Context.Render(parameterBuilder);

    protected virtual void OnSetup()
    {
    }

    private void RegisterServices()
    {
        var mockHandler = new Mock<HttpMessageHandler>();
        TestData.SetupHandler(mockHandler, HttpMethod.Get, "api/tour",
            JsonSerializer.Serialize(TestData.SampleTourList(2)));
        var httpClient = new HttpClient(mockHandler.Object)
            { BaseAddress = new UriBuilder { Scheme = "https", Host = "test.invalid" }.Uri };

        var toast = TestData.MockToastService();
        var logger = TestData.MockLogger();
        var config = TestData.MockConfiguration();
        var route = TestData.MockRouteApiService();
        var download = TestData.MockBlazorDownloadFileService();

        Context.Services.AddSingleton(httpClient);
        Context.Services.AddSingleton(mockHandler);
        Context.Services.AddSingleton(toast.Object);
        Context.Services.AddSingleton(logger.Object);
        Context.Services.AddSingleton(config.Object);
        Context.Services.AddSingleton(route.Object);
        Context.Services.AddSingleton(download.Object);
        Context.Services.AddSingleton(new Mock<IToastService>().Object);

        Context.Services.AddSingleton(toast);
        Context.Services.AddSingleton(logger);
        Context.Services.AddSingleton(config);
        Context.Services.AddSingleton(route);
        Context.Services.AddSingleton(download);

        Context.Services.AddSingleton(TestData.MockTryCatchToastWrapper(toast.Object));

        Context.Services.AddScoped<MapViewModel>();
        Context.Services.AddScoped<AuthViewModel>();
        Context.Services.AddScoped<TourViewModel>();
        Context.Services.AddScoped<TourLogViewModel>();
        Context.Services.AddScoped<SearchViewModel>();
        Context.Services.AddScoped<ReportViewModel>();
    }

    private void RegisterAuth()
    {
        var authClient = new HttpClient(new HttpClientHandler())
        {
            BaseAddress = new UriBuilder { Scheme = "https", Host = "test.invalid" }.Uri
        };
        Context.Services.AddSingleton(new CookieAuthenticationStateProvider(authClient));

        var auth = Context.AddAuthorization();
        auth.SetAuthorized("test@example.com");
        auth.SetClaims(
            new Claim(ClaimTypes.NameIdentifier, TestData.TestUserId),
            new Claim(ClaimTypes.Email, "test@example.com"));
    }
}

public static class ViewTestExtensions
{
    public static T ViewModel<T>(this IServiceProvider services) where T : class =>
        services.GetRequiredService<T>();

    public static Mock<T> Mock<T>(this IServiceProvider services) where T : class =>
        services.GetRequiredService<Mock<T>>();

    // ── Tour ViewModel setup ──

    public static void WithTours(this IServiceProvider s, int count = 2) =>
        s.ViewModel<TourViewModel>().Tours = [..TestData.SampleTourList(count)];

    public static void WithEmptyTours(this IServiceProvider s)
    {
        TestData.SetupHandler(s.GetRequiredService<Mock<HttpMessageHandler>>(),
            HttpMethod.Get, "api/tour", "[]");
        s.ViewModel<TourViewModel>().Tours = [];
    }

    public static Guid FirstTourId(this IServiceProvider s) =>
        s.ViewModel<TourViewModel>().Tours.First().Id;

    public static void WithValidTourForm(this IServiceProvider s) =>
        s.ViewModel<TourViewModel>().SelectedTour = TestData.SampleTour(
            name: "Valid Tour", from: "Vienna", to: "Paris", transportType: "Car");

    public static void WithEmptyTourForm(this IServiceProvider s) =>
        s.ViewModel<TourViewModel>().SelectedTour = Tour.Empty;

    public static void WithModalTour(this IServiceProvider s, string name = "Test Tour") =>
        s.ViewModel<TourViewModel>().ModalTour = TestData.SampleTour(name);

    public static void WithMinimalModalTour(this IServiceProvider s) =>
        s.ViewModel<TourViewModel>().ModalTour = TestData.SampleTour(
            name: "Tour", description: "", from: "A", to: "B", transportType: "Walk",
            distance: null, estimatedTime: null, imagePath: null, routeInformation: null);

    // ── TourLog ViewModel setup ──

    public static void WithTourLogs(this IServiceProvider s, int count = 2) =>
        s.ViewModel<TourLogViewModel>().TourLogs = new ObservableCollection<TourLog>(TestData.SampleTourLogList(count));

    public static void WithSingleTourLog(this IServiceProvider s) =>
        s.ViewModel<TourLogViewModel>().TourLogs = [TestData.SampleTourLog()];

    public static Guid FirstTourLogId(this IServiceProvider s) =>
        s.ViewModel<TourLogViewModel>().TourLogs.First().Id;

    public static void WithValidTourLogForm(this IServiceProvider s)
    {
        var log = TestData.SampleTourLog();
        s.ViewModel<TourLogViewModel>().SelectedTourLog = log;
        s.ViewModel<TourLogViewModel>().SelectedTourId = log.TourId;
    }

    public static void WithEmptyTourLogForm(this IServiceProvider s) =>
        s.ViewModel<TourLogViewModel>().ResetForm();

    public static void WithTourLogFormVisible(this IServiceProvider s, bool newLog = true)
    {
        var vm = s.ViewModel<TourLogViewModel>();
        vm.SelectedTourId = s.FirstTourId();
        vm.IsLogFormVisible = true;
        vm.SelectedTourLog = TestData.SampleTourLog(
            id: newLog ? Guid.Empty : Guid.NewGuid(),
            tourId: vm.SelectedTourId ?? Guid.Empty);
    }

    // ── Search ViewModel setup ──

    public static void WithSearchResults(this IServiceProvider s, int count = 1) =>
        s.ViewModel<SearchViewModel>().SearchResults = [..TestData.SampleTourList(count)];

    public static void WithSearchResultWithLogs(this IServiceProvider s)
    {
        var id = TestData.TestGuid;
        s.ViewModel<SearchViewModel>().SearchResults = [TestData.SampleTour(id: id,
            tourLogs: [TestData.SampleTourLog(tourId: id)])];
    }

    public static void WithSearchResultWithoutLogs(this IServiceProvider s) =>
        s.ViewModel<SearchViewModel>().SearchResults = [TestData.SampleTour()];

    public static Guid FirstSearchResultId(this IServiceProvider s) =>
        s.ViewModel<SearchViewModel>().SearchResults.First().Id;

    // ── Mock setup helpers ──

    public static void SetupMockDeleteTour(this IServiceProvider s, Guid id) =>
        TestData.SetupHandler(s.GetRequiredService<Mock<HttpMessageHandler>>(),
            HttpMethod.Delete, $"api/tour/{id}", "{}");

    public static void SetupMockGetTour(this IServiceProvider s, Guid id) =>
        TestData.SetupHandler(s.GetRequiredService<Mock<HttpMessageHandler>>(),
            HttpMethod.Get, $"api/tour/{id}", JsonSerializer.Serialize(TestData.SampleTour(id: id)));

    public static void SetupMockPostTour(this IServiceProvider s)
    {
        var handler = s.GetRequiredService<Mock<HttpMessageHandler>>();
        TestData.SetupHandler(handler, HttpMethod.Post, "api/tour", "{}");
    }

    public static void SetupMockGetTourLogs(this IServiceProvider s, Guid tourId, int count = 3) =>
        TestData.SetupHandler(s.GetRequiredService<Mock<HttpMessageHandler>>(),
            HttpMethod.Get, $"api/tourlog/bytour/{tourId}",
            JsonSerializer.Serialize(TestData.SampleTourLogList(count, tourId)));

    public static void SetupMockGetTourLog(this IServiceProvider s, Guid logId) =>
        TestData.SetupHandler(s.GetRequiredService<Mock<HttpMessageHandler>>(),
            HttpMethod.Get, $"api/tourlog/{logId}", JsonSerializer.Serialize(TestData.SampleTourLog(id: logId)));

    public static void SetupMockPostTourLog(this IServiceProvider s) =>
        TestData.SetupHandler(s.GetRequiredService<Mock<HttpMessageHandler>>(),
            HttpMethod.Post, "api/tourlog", "{}");

    public static void SetupMockDeleteTourLog(this IServiceProvider s, Guid id) =>
        TestData.SetupHandler(s.GetRequiredService<Mock<HttpMessageHandler>>(),
            HttpMethod.Delete, $"api/tourlog/{id}", "{}");

    public static void SetupMockRouteData(this IServiceProvider s) =>
        s.Mock<IRouteApiService>().Setup(static x => x.FetchRouteDataAsync(
            It.IsAny<(double, double)>(), It.IsAny<(double, double)>(), It.IsAny<string>())).ReturnsAsync((100.5, 60.5));

    public static void SetupMockReportBytes(this IServiceProvider s, string uri) =>
        TestData.SetupHandlerBytes(s.GetRequiredService<Mock<HttpMessageHandler>>(),
            uri, [1, 2, 3]);

    public static void SetupMockDownloadFile(this IServiceProvider s) =>
        s.Mock<IBlazorDownloadFileService>().Setup(static x => x.DownloadFileAsync(
            It.IsAny<string>(), It.IsAny<byte[]>(), "application/pdf")).Returns(new ValueTask<bool>(true));

    // ── Mock verify helpers ──

    public static void VerifyMockDeleteTour(this IServiceProvider s, Guid id, Times times) =>
        TestData.VerifyHandler(s.GetRequiredService<Mock<HttpMessageHandler>>(),
            HttpMethod.Delete, $"api/tour/{id}", times);

    public static void VerifyMockPostTour(this IServiceProvider s, Times times) =>
        TestData.VerifyHandler(s.GetRequiredService<Mock<HttpMessageHandler>>(),
            HttpMethod.Post, "api/tour", times);

    public static void VerifyMockGetTour(this IServiceProvider s, Guid id, Times times) =>
        TestData.VerifyHandler(s.GetRequiredService<Mock<HttpMessageHandler>>(),
            HttpMethod.Get, $"api/tour/{id}", times);

    public static void VerifyMockPostTourLog(this IServiceProvider s, Times times) =>
        TestData.VerifyHandler(s.GetRequiredService<Mock<HttpMessageHandler>>(),
            HttpMethod.Post, "api/tourlog", times);

    public static void VerifyMockDeleteTourLog(this IServiceProvider s, Guid id, Times times) =>
        TestData.VerifyHandler(s.GetRequiredService<Mock<HttpMessageHandler>>(),
            HttpMethod.Delete, $"api/tourlog/{id}", times);

    // ── Auth helpers ──

    public static void SetupAuthHandler(this IServiceProvider s, HttpStatusCode statusCode, string? content = null) =>
        TestData.SetupHandler(s.GetRequiredService<Mock<HttpMessageHandler>>(),
            HttpMethod.Post, "api/account",
            content ?? """{"userId":"id","email":"e@e.com"}""",
            statusCode);
}
