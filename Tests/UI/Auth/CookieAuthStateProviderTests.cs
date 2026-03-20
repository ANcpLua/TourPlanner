using System.Net;
using System.Security.Claims;
using UI.Auth;

namespace Tests.UI.Auth;

[TestFixture]
public class CookieAuthStateProviderTests
{
    [Test]
    public async Task GetAuthenticationStateAsync_SuccessfulResponse_ReturnsAuthenticatedState()
    {
        const string json = """{"userId":"test-id","email":"test@example.com"}""";
        var handler = new Mock<HttpMessageHandler>();
        TestData.SetupHttpMessageHandlerSuccess(handler, json);
        var httpClient = new HttpClient(handler.Object) { BaseAddress = new Uri("http://localhost") };

        var sut = new CookieAuthenticationStateProvider(httpClient);
        var state = await sut.GetAuthenticationStateAsync();

        Assert.That(state.User.Identity!.IsAuthenticated, Is.True);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(state.User.FindFirstValue(ClaimTypes.NameIdentifier), Is.EqualTo("test-id"));
            Assert.That(state.User.FindFirstValue(ClaimTypes.Email), Is.EqualTo("test@example.com"));
        }
    }

    [Test]
    public async Task GetAuthenticationStateAsync_HttpError_ReturnsAnonymousState()
    {
        var handler = new Mock<HttpMessageHandler>();
        TestData.SetupHttpMessageHandlerError(handler, HttpStatusCode.InternalServerError, "error");
        var httpClient = new HttpClient(handler.Object) { BaseAddress = new Uri("http://localhost") };

        var sut = new CookieAuthenticationStateProvider(httpClient);
        var state = await sut.GetAuthenticationStateAsync();

        Assert.That(state.User.Identity!.IsAuthenticated, Is.False);
    }

    [Test]
    public void NotifyAuthStateChanged_RaisesEvent()
    {
        const string json = """{"userId":"test-id","email":"test@example.com"}""";
        var handler = new Mock<HttpMessageHandler>();
        TestData.SetupHttpMessageHandlerSuccess(handler, json);
        var httpClient = new HttpClient(handler.Object) { BaseAddress = new Uri("http://localhost") };

        var sut = new CookieAuthenticationStateProvider(httpClient);
        var eventFired = false;
        sut.AuthenticationStateChanged += _ => { eventFired = true; };

        sut.NotifyAuthStateChanged();

        Assert.That(eventFired, Is.True);
    }
}
