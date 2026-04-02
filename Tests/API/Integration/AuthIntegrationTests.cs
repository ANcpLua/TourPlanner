using System.Net;
using System.Net.Http.Json;
using API.Endpoints;
using Contracts.Auth;

namespace Tests.API.Integration;

[TestFixture]
public sealed class AuthIntegrationTests : ApiIntegrationTestBase
{
    [Test]
    public async Task RegisterAsync_ValidRequest_ReturnsUserInfoAndAuthenticatesClient()
    {
        var email = $"user-{Guid.NewGuid():N}@example.com";
        const string password = "Passw0rd!";

        var registerResponse = await Client.PostAsJsonAsync(ApiRoute.Auth.RegisterPath, new RegisterRequest
        {
            Email = email,
            Password = password
        });

        Assert.That(registerResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK),
            await registerResponse.Content.ReadAsStringAsync());

        var userInfo = (await registerResponse.Content.ReadFromJsonAsync<UserInfo>())!;
        var meResponse = await Client.GetAsync(ApiRoute.Auth.MePath);
        Assert.That(meResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), await meResponse.Content.ReadAsStringAsync());

        var currentUser = (await meResponse.Content.ReadFromJsonAsync<UserInfo>())!;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(userInfo.UserId, Is.Not.Empty);
            Assert.That(userInfo.Email, Is.EqualTo(email));
            Assert.That(currentUser.UserId, Is.EqualTo(userInfo.UserId));
            Assert.That(currentUser.Email, Is.EqualTo(email));
        }
    }

    [Test]
    public async Task LoginAndLogoutAsync_RestoreAndClearAuthenticatedSession()
    {
        var (email, password) = await AuthenticateAsync();
        var secondClient = CreateClient();

        try
        {
            var loginResponse = await secondClient.PostAsJsonAsync(ApiRoute.Auth.LoginPath, new LoginRequest
            {
                Email = email,
                Password = password
            });

            Assert.That(loginResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                await loginResponse.Content.ReadAsStringAsync());

            var meResponse = await secondClient.GetAsync(ApiRoute.Auth.MePath);
            Assert.That(meResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), await meResponse.Content.ReadAsStringAsync());

            var logoutResponse = await secondClient.PostAsync(ApiRoute.Auth.LogoutPath, null);
            Assert.That(logoutResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                await logoutResponse.Content.ReadAsStringAsync());

            var afterLogout = await secondClient.GetAsync(ApiRoute.Auth.MePath);
            Assert.That(afterLogout.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        }
        finally
        {
            secondClient.Dispose();
        }
    }

    [Test]
    public async Task RegisterAsync_InvalidRequest_ReturnsValidationProblem()
    {
        var response = await Client.PostAsJsonAsync(ApiRoute.Auth.RegisterPath, new RegisterRequest
        {
            Email = "not-an-email",
            Password = "123"
        });

        var body = await response.Content.ReadAsStringAsync();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(body, Does.Contain("Email"));
            Assert.That(body, Does.Contain("Password"));
        }
    }

    [Test]
    public async Task LoginAsync_NonExistentEmail_Returns401()
    {
        var response = await Client.PostAsJsonAsync(ApiRoute.Auth.LoginPath, new LoginRequest
        {
            Email = "nonexistent@example.com",
            Password = "Passw0rd!"
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task LoginAsync_WrongPassword_Returns401()
    {
        var (email, _) = await AuthenticateAsync();
        var secondClient = CreateClient();

        try
        {
            var response = await secondClient.PostAsJsonAsync(ApiRoute.Auth.LoginPath, new LoginRequest
            {
                Email = email,
                Password = "WrongPassw0rd!"
            });

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        }
        finally
        {
            secondClient.Dispose();
        }
    }

    [Test]
    public async Task LoginAsync_RepeatedFailures_LocksAccount()
    {
        var (email, _) = await AuthenticateAsync();
        var attackClient = CreateClient();

        try
        {
            for (var i = 0; i < 5; i++)
            {
                await attackClient.PostAsJsonAsync(ApiRoute.Auth.LoginPath, new LoginRequest
                {
                    Email = email,
                    Password = "WrongPassw0rd!"
                });
            }

            var lockedResponse = await attackClient.PostAsJsonAsync(ApiRoute.Auth.LoginPath, new LoginRequest
            {
                Email = email,
                Password = "WrongPassw0rd!"
            });

            Assert.That(lockedResponse.StatusCode, Is.EqualTo(HttpStatusCode.TooManyRequests));
        }
        finally
        {
            attackClient.Dispose();
        }
    }

    [Test]
    public async Task GetCurrentUser_Unauthenticated_Returns401()
    {
        var response = await Client.GetAsync(ApiRoute.Auth.MePath);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task LogoutAsync_Unauthenticated_Returns401()
    {
        var response = await Client.PostAsync(ApiRoute.Auth.LogoutPath, null);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }
}
