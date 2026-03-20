using System.Net;
using UI.Auth;
using UI.View.Pages;

namespace Tests.UI.View;

[TestFixture]
[Parallelizable(ParallelScope.All)]
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public sealed class LoginPageTests : BunitTestBase
{
    [Test]
    public void LoginPage_RendersFormElements()
    {
        var cut = RenderComponent<LoginPage>();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(cut.FindAll("input").Count, Is.GreaterThanOrEqualTo(2));
            Assert.That(cut.Find("button[type=submit]").TextContent, Does.Contain("Login"));
            Assert.That(cut.Find("a[href='/register']"), Is.Not.Null);
        }
    }

    [Test]
    public async Task LoginPage_SuccessfulLogin_NavigatesToHome()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"userId":"id","email":"e@e.com"}""",
                    Encoding.UTF8, "application/json")
            });
        var httpClient = new HttpClient(handler.Object) { BaseAddress = new Uri("http://localhost") };
        Context.Services.AddSingleton(httpClient);
        Context.Services.AddSingleton(new CookieAuthenticationStateProvider(httpClient));

        var nav = Context.Services.GetRequiredService<NavigationManager>();
        var cut = RenderComponent<LoginPage>();

        cut.Find("input[type=email]").Change("test@example.com");
        cut.Find("input[type=password]").Change("Test1234!");
        await cut.Find("form").SubmitAsync();

        Assert.That(nav.Uri, Does.EndWith("/"));
    }

    [Test]
    public async Task LoginPage_FailedLogin_ShowsError()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var httpClient = new HttpClient(handler.Object) { BaseAddress = new Uri("http://localhost") };
        Context.Services.AddSingleton(httpClient);
        Context.Services.AddSingleton(new CookieAuthenticationStateProvider(httpClient));

        var cut = RenderComponent<LoginPage>();
        cut.Find("input[type=email]").Change("test@example.com");
        cut.Find("input[type=password]").Change("wrong");
        await cut.Find("form").SubmitAsync();

        Assert.That(cut.Find(".alert-danger").TextContent, Does.Contain("Invalid"));
    }
}

[TestFixture]
[Parallelizable(ParallelScope.All)]
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public sealed class RegisterPageTests : BunitTestBase
{
    [Test]
    public void RegisterPage_RendersFormElements()
    {
        var cut = RenderComponent<RegisterPage>();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(cut.FindAll("input").Count, Is.GreaterThanOrEqualTo(2));
            Assert.That(cut.Find("button[type=submit]").TextContent, Does.Contain("Register"));
            Assert.That(cut.Find("a[href='/login']"), Is.Not.Null);
        }
    }

    [Test]
    public async Task RegisterPage_SuccessfulRegister_NavigatesToHome()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"userId":"id","email":"e@e.com"}""",
                    Encoding.UTF8, "application/json")
            });
        var httpClient = new HttpClient(handler.Object) { BaseAddress = new Uri("http://localhost") };
        Context.Services.AddSingleton(httpClient);
        Context.Services.AddSingleton(new CookieAuthenticationStateProvider(httpClient));

        var nav = Context.Services.GetRequiredService<NavigationManager>();
        var cut = RenderComponent<RegisterPage>();

        cut.Find("input[type=email]").Change("new@example.com");
        cut.Find("input[type=password]").Change("Test1234!");
        await cut.Find("form").SubmitAsync();

        Assert.That(nav.Uri, Does.EndWith("/"));
    }

    [Test]
    public async Task RegisterPage_DuplicateUser_ShowsDuplicateError()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("""{"DuplicateUserName":["Already exists"]}""",
                    Encoding.UTF8, "application/json")
            });
        var httpClient = new HttpClient(handler.Object) { BaseAddress = new Uri("http://localhost") };
        Context.Services.AddSingleton(httpClient);
        Context.Services.AddSingleton(new CookieAuthenticationStateProvider(httpClient));

        var cut = RenderComponent<RegisterPage>();
        cut.Find("input[type=email]").Change("dupe@example.com");
        cut.Find("input[type=password]").Change("Test1234!");
        await cut.Find("form").SubmitAsync();

        Assert.That(cut.Find(".alert-danger").TextContent, Does.Contain("already exists"));
    }

    [Test]
    public async Task RegisterPage_GenericError_ShowsGenericMessage()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("other error", Encoding.UTF8, "text/plain")
            });
        var httpClient = new HttpClient(handler.Object) { BaseAddress = new Uri("http://localhost") };
        Context.Services.AddSingleton(httpClient);
        Context.Services.AddSingleton(new CookieAuthenticationStateProvider(httpClient));

        var cut = RenderComponent<RegisterPage>();
        cut.Find("input[type=email]").Change("fail@example.com");
        cut.Find("input[type=password]").Change("Test1234!");
        await cut.Find("form").SubmitAsync();

        Assert.That(cut.Find(".alert-danger").TextContent, Does.Contain("Registration failed"));
    }
}

[TestFixture]
[Parallelizable(ParallelScope.All)]
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public sealed class RedirectToLoginTests : BunitTestBase
{
    [Test]
    public void RedirectToLogin_NavigatesToLoginPage()
    {
        var nav = Context.Services.GetRequiredService<NavigationManager>();

        RenderComponent<RedirectToLogin>();

        Assert.That(nav.Uri, Does.EndWith("/login"));
    }
}
