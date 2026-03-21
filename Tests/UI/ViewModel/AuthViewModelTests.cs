using System.Net;
using System.Net.Http.Json;
using Contracts.Auth;
using UI.Auth;
using UI.ViewModel;

namespace Tests.UI.ViewModel;

[TestFixture]
public class AuthViewModelTests
{
    [SetUp]
    public void Setup()
    {
        var (client, handler) = TestData.MockedHttpClient();
        _httpClient = client;
        _mockHandler = handler;
        _navigationManager = new TestNavigationManager();
        _authStateProvider = new CookieAuthenticationStateProvider(new HttpClient());
        _viewModel = new AuthViewModel(_httpClient, _navigationManager, _authStateProvider);
    }

    private HttpClient _httpClient = null!;
    private Mock<HttpMessageHandler> _mockHandler = null!;
    private TestNavigationManager _navigationManager = null!;
    private CookieAuthenticationStateProvider _authStateProvider = null!;
    private AuthViewModel _viewModel = null!;

    private sealed class TestNavigationManager : NavigationManager
    {
        public string? LastUri;

        public TestNavigationManager()
        {
            Initialize("http://localhost/", "http://localhost/");
        }

        protected override void NavigateToCore(string uri, bool forceLoad)
        {
            LastUri = uri;
        }
    }

    [Test]
    public async Task LoginAsync_Success_NavigatesToHome()
    {
        TestData.SetupHandler(_mockHandler, HttpMethod.Post, "api/auth/login",
            new { userId = "id", email = "e@e.com" });

        await _viewModel.LoginAsync(new LoginRequest { Email = "test@example.com", Password = "Test1234!" });

        Assert.That(_navigationManager.LastUri, Is.EqualTo("/"));
    }

    [Test]
    public async Task LoginAsync_HttpFailure_SetsErrorMessage()
    {
        TestData.SetupHandler(_mockHandler, HttpMethod.Post, "api/auth/login",
            "", HttpStatusCode.Unauthorized);

        await _viewModel.LoginAsync(new LoginRequest { Email = "test@example.com", Password = "wrong" });

        Assert.That(_viewModel.ErrorMessage, Is.EqualTo("Invalid email or password."));
    }

    [Test]
    public async Task LoginAsync_NetworkError_SetsServerUnreachable()
    {
        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        await _viewModel.LoginAsync(new LoginRequest { Email = "test@example.com", Password = "Test1234!" });

        Assert.That(_viewModel.ErrorMessage, Is.EqualTo("Unable to reach the server."));
    }

    [Test]
    public async Task RegisterAsync_Success_NavigatesToHome()
    {
        TestData.SetupHandler(_mockHandler, HttpMethod.Post, "api/auth/register",
            new { userId = "id", email = "e@e.com" });

        await _viewModel.RegisterAsync(new RegisterRequest { Email = "new@example.com", Password = "Test1234!" });

        Assert.That(_navigationManager.LastUri, Is.EqualTo("/"));
    }

    [Test]
    public async Task RegisterAsync_DuplicateUser_SetsDuplicateMessage()
    {
        TestData.SetupHandler(_mockHandler, HttpMethod.Post, "api/auth/register",
            """{"DuplicateUserName":["Already exists"]}""", HttpStatusCode.BadRequest);

        await _viewModel.RegisterAsync(new RegisterRequest { Email = "dupe@example.com", Password = "Test1234!" });

        Assert.That(_viewModel.ErrorMessage, Is.EqualTo("An account with this email already exists."));
    }

    [Test]
    public async Task RegisterAsync_GenericError_SetsGenericMessage()
    {
        TestData.SetupHandler(_mockHandler, HttpMethod.Post, "api/auth/register",
            "other error", HttpStatusCode.BadRequest);

        await _viewModel.RegisterAsync(new RegisterRequest { Email = "fail@example.com", Password = "Test1234!" });

        Assert.That(_viewModel.ErrorMessage, Is.EqualTo("Registration failed. Please check your input."));
    }

    [Test]
    public async Task IsProcessing_SetDuringExecution()
    {
        var tcs = new TaskCompletionSource<HttpResponseMessage>();
        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Returns(tcs.Task);

        var loginTask = _viewModel.LoginAsync(new LoginRequest { Email = "e@e.com", Password = "p" });
        Assert.That(_viewModel.IsProcessing, Is.True);

        tcs.SetResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new { userId = "id", email = "e@e.com" })
        });
        await loginTask;

        Assert.That(_viewModel.IsProcessing, Is.False);
    }

    [Test]
    public async Task LoginAsync_ClearsErrorOnRetry()
    {
        TestData.SetupHandler(_mockHandler, HttpMethod.Post, "api/auth/login",
            "", HttpStatusCode.Unauthorized);

        await _viewModel.LoginAsync(new LoginRequest { Email = "e@e.com", Password = "wrong" });
        Assert.That(_viewModel.ErrorMessage, Is.Not.Null);

        TestData.SetupHandler(_mockHandler, HttpMethod.Post, "api/auth/login",
            new { userId = "id", email = "e@e.com" });

        await _viewModel.LoginAsync(new LoginRequest { Email = "e@e.com", Password = "correct" });
        Assert.That(_viewModel.ErrorMessage, Is.Null);
    }
}
