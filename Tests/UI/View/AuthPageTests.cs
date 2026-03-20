using System.Net;
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
            Assert.That(cut.FindAll("input"), Has.Count.GreaterThanOrEqualTo(2));
            Assert.That(cut.Find("button[type=submit]").TextContent, Does.Contain("Login"));
            Assert.That(cut.Find("a[href='/register']"), Is.Not.Null);
        }
    }

    [Test]
    public async Task LoginPage_SuccessfulLogin_NavigatesToHome()
    {
        Services.SetupAuthHandler(HttpStatusCode.OK);

        var nav = Context.Services.GetRequiredService<NavigationManager>();
        var cut = RenderComponent<LoginPage>();

        await cut.Find("input[type=email]").ChangeAsync("test@example.com");
        await cut.Find("input[type=password]").ChangeAsync("Test1234!");
        await cut.Find("form").SubmitAsync();

        Assert.That(nav.Uri, Does.EndWith("/"));
    }

    [Test]
    public async Task LoginPage_FailedLogin_ShowsError()
    {
        Services.SetupAuthHandler(HttpStatusCode.Unauthorized);

        var cut = RenderComponent<LoginPage>();
        await cut.Find("input[type=email]").ChangeAsync("test@example.com");
        await cut.Find("input[type=password]").ChangeAsync("wrong");
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
            Assert.That(cut.FindAll("input"), Has.Count.GreaterThanOrEqualTo(2));
            Assert.That(cut.Find("button[type=submit]").TextContent, Does.Contain("Register"));
            Assert.That(cut.Find("a[href='/login']"), Is.Not.Null);
        }
    }

    [Test]
    public async Task RegisterPage_SuccessfulRegister_NavigatesToHome()
    {
        Services.SetupAuthHandler(HttpStatusCode.OK);

        var nav = Context.Services.GetRequiredService<NavigationManager>();
        var cut = RenderComponent<RegisterPage>();

        await cut.Find("input[type=email]").ChangeAsync("new@example.com");
        await cut.Find("input[type=password]").ChangeAsync("Test1234!");
        await cut.Find("form").SubmitAsync();

        Assert.That(nav.Uri, Does.EndWith("/"));
    }

    [Test]
    public async Task RegisterPage_DuplicateUser_ShowsDuplicateError()
    {
        Services.SetupAuthHandler(HttpStatusCode.BadRequest,
            """{"DuplicateUserName":["Already exists"]}""");

        var cut = RenderComponent<RegisterPage>();
        await cut.Find("input[type=email]").ChangeAsync("dupe@example.com");
        await cut.Find("input[type=password]").ChangeAsync("Test1234!");
        await cut.Find("form").SubmitAsync();

        Assert.That(cut.Find(".alert-danger").TextContent, Does.Contain("already exists"));
    }

    [Test]
    public async Task RegisterPage_GenericError_ShowsGenericMessage()
    {
        Services.SetupAuthHandler(HttpStatusCode.BadRequest, "other error");

        var cut = RenderComponent<RegisterPage>();
        await cut.Find("input[type=email]").ChangeAsync("fail@example.com");
        await cut.Find("input[type=password]").ChangeAsync("Test1234!");
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
