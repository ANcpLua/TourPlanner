using Microsoft.AspNetCore.Components;
using IComponent = Microsoft.AspNetCore.Components.IComponent;
using UI.Model;
using UI.Service.Interface;
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
        s.Mock<IHttpService>().Setup(x => x.GetListAsync<Tour>("api/tour")).ReturnsAsync(new List<Tour>());
        s.ViewModel<TourViewModel>().Tours.Clear();
    }

    public static Guid FirstTourId(this IServiceProvider s) =>
        s.ViewModel<TourViewModel>().Tours.First().Id;

    public static void WithValidTourForm(this IServiceProvider s)
    {
        var tour = TestData.SampleTour();
        tour.Name = "Valid Tour";
        tour.From = "Vienna";
        tour.To = "Paris";
        tour.TransportType = "Car";
        s.ViewModel<TourViewModel>().SelectedTour = tour;
    }

    public static void WithEmptyTourForm(this IServiceProvider s) =>
        s.ViewModel<TourViewModel>().SelectedTour = Tour.Empty;

    public static void WithModalTour(this IServiceProvider s, string name = "Test Tour") =>
        s.ViewModel<TourViewModel>().ModalTour = TestData.SampleTour(name);

    public static void WithMinimalModalTour(this IServiceProvider s) =>
        s.ViewModel<TourViewModel>().ModalTour = new Tour
            { Name = "Tour", Description = "", From = "A", To = "B", TransportType = "Walk" };

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
        s.ViewModel<TourLogViewModel>().SelectedTourLog = new TourLog();

    public static void WithTourLogFormVisible(this IServiceProvider s, bool newLog = true)
    {
        var vm = s.ViewModel<TourLogViewModel>();
        vm.SelectedTourId = s.FirstTourId();
        vm.IsLogFormVisible = true;
        vm.SelectedTourLog = newLog ? new TourLog { Id = Guid.Empty } : new TourLog { Id = Guid.NewGuid() };
    }

    // ── Search ViewModel setup ──

    public static void WithSearchResults(this IServiceProvider s, int count = 1) =>
        s.ViewModel<SearchViewModel>().SearchResults = [..TestData.SampleTourList(count)];

    public static void WithSearchResultWithLogs(this IServiceProvider s)
    {
        var tour = TestData.SampleTour();
        tour.TourLogs = [TestData.SampleTourLog(tourId: tour.Id)];
        s.ViewModel<SearchViewModel>().SearchResults = [tour];
    }

    public static void WithSearchResultWithoutLogs(this IServiceProvider s)
    {
        var tour = TestData.SampleTour();
        tour.TourLogs.Clear();
        s.ViewModel<SearchViewModel>().SearchResults = [tour];
    }

    public static Guid FirstSearchResultId(this IServiceProvider s) =>
        s.ViewModel<SearchViewModel>().SearchResults.First().Id;

    // ── Mock setup helpers (hides Tour/TourLog generic params) ──

    public static void SetupMockDeleteTour(this IServiceProvider s, Guid id) =>
        s.Mock<IHttpService>().Setup(x => x.DeleteAsync($"api/tour/{id}")).Returns(Task.CompletedTask);

    public static void SetupMockGetTour(this IServiceProvider s, Guid id) =>
        s.Mock<IHttpService>().Setup(x => x.GetAsync<Tour>($"api/tour/{id}")).ReturnsAsync(TestData.SampleTour(id: id));

    public static void SetupMockPostTour(this IServiceProvider s)
    {
        s.Mock<IHttpService>().Setup(x => x.PostAsync<Tour>("api/tour", It.IsAny<Tour>()))
            .ReturnsAsync(TestData.SampleTour());
        s.Mock<IHttpService>().Setup(x => x.PostAsync("api/tour", It.IsAny<object>()))
            .Returns(Task.CompletedTask);
    }

    public static void SetupMockGetTourLogs(this IServiceProvider s, Guid tourId, int count = 3) =>
        s.Mock<IHttpService>().Setup(x => x.GetListAsync<TourLog>($"api/tourlog/bytour/{tourId}"))
            .ReturnsAsync(TestData.SampleTourLogList(count, tourId));

    public static void SetupMockGetTourLog(this IServiceProvider s, Guid logId) =>
        s.Mock<IHttpService>().Setup(x => x.GetAsync<TourLog>($"api/tourlog/{logId}"))
            .ReturnsAsync(TestData.SampleTourLog(id: logId));

    public static void SetupMockPostTourLog(this IServiceProvider s)
    {
        s.Mock<IHttpService>().Setup(x => x.PostAsync<TourLog>("api/tourlog", It.IsAny<TourLog>()))
            .ReturnsAsync(TestData.SampleTourLog());
        s.Mock<IHttpService>().Setup(x => x.PostAsync("api/tourlog", It.IsAny<object>()))
            .Returns(Task.CompletedTask);
    }

    public static void SetupMockDeleteTourLog(this IServiceProvider s, Guid id) =>
        s.Mock<IHttpService>().Setup(x => x.DeleteAsync($"api/tourlog/{id}")).Returns(Task.CompletedTask);

    public static void SetupMockRouteData(this IServiceProvider s) =>
        s.Mock<IRouteApiService>().Setup(x => x.FetchRouteDataAsync(
            It.IsAny<(double, double)>(), It.IsAny<(double, double)>(), It.IsAny<string>())).ReturnsAsync((100.5, 60.5));

    public static void SetupMockReportBytes(this IServiceProvider s, string uri) =>
        s.Mock<IHttpService>().Setup(x => x.GetByteArrayAsync(uri)).ReturnsAsync([1, 2, 3]);

    public static void SetupMockDownloadFile(this IServiceProvider s) =>
        s.Mock<IBlazorDownloadFileService>().Setup(x => x.DownloadFileAsync(
            It.IsAny<string>(), It.IsAny<byte[]>(), "application/pdf")).Returns(new ValueTask<bool>(true));

    // ── Mock verify helpers ──

    public static void VerifyMockDeleteTour(this IServiceProvider s, Guid id, Times times) =>
        s.Mock<IHttpService>().Verify(x => x.DeleteAsync($"api/tour/{id}"), times);

    public static void VerifyMockPostTour(this IServiceProvider s, Times times)
    {
        var http = s.Mock<IHttpService>();
        try { http.Verify(x => x.PostAsync("api/tour", It.IsAny<object>()), times); }
        catch (MockException) { http.Verify(x => x.PostAsync<Tour>("api/tour", It.IsAny<Tour>()), times); }
    }

    public static void VerifyMockGetTour(this IServiceProvider s, Guid id, Times times) =>
        s.Mock<IHttpService>().Verify(x => x.GetAsync<Tour>($"api/tour/{id}"), times);

    public static void VerifyMockPostTourLog(this IServiceProvider s, Times times) =>
        s.Mock<IHttpService>().Verify(x => x.PostAsync<TourLog>("api/tourlog", It.IsAny<TourLog>()), times);

    public static void VerifyMockDeleteTourLog(this IServiceProvider s, Guid id, Times times) =>
        s.Mock<IHttpService>().Verify(x => x.DeleteAsync($"api/tourlog/{id}"), times);
}
