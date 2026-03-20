using System.Security.Claims;
using API.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Tests.API;

[TestFixture]
public class AuthEndpointsTests
{
    [Test]
    public void MapAuthEndpoints_RegistersEndpoints()
    {
        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();
        var result = app.MapAuthEndpoints();

        Assert.That(result, Is.Not.Null);
        var dataSource = app as IEndpointRouteBuilder;
        Assert.That(dataSource.DataSources, Is.Not.Empty);
    }

    [Test]
    public void GetCurrentUser_WithValidClaims_ReturnsUserInfo()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, TestData.TestUserId),
            new Claim(ClaimTypes.Email, "test@example.com")
        };
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"))
        };

        var result = AuthEndpoints.GetCurrentUser(httpContext);

        Assert.That(result, Is.TypeOf<Ok<Contracts.Auth.UserInfo>>());
        var okResult = (Ok<Contracts.Auth.UserInfo>)result;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(okResult.Value!.UserId, Is.EqualTo(TestData.TestUserId));
            Assert.That(okResult.Value.Email, Is.EqualTo("test@example.com"));
        }
    }

    [Test]
    public void GetCurrentUser_WithNameClaimFallback_ReturnsUserInfo()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, TestData.TestUserId),
            new Claim(ClaimTypes.Name, "user@test.com")
        };
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"))
        };

        var result = AuthEndpoints.GetCurrentUser(httpContext);

        Assert.That(result, Is.TypeOf<Ok<Contracts.Auth.UserInfo>>());
        var okResult = (Ok<Contracts.Auth.UserInfo>)result;
        Assert.That(okResult.Value!.Email, Is.EqualTo("user@test.com"));
    }

    [Test]
    public void GetCurrentUser_WithNoClaims_Returns401()
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity())
        };

        var result = AuthEndpoints.GetCurrentUser(httpContext);

        Assert.That(result, Is.TypeOf<ProblemHttpResult>());
    }

    [Test]
    public void GetCurrentUser_WithUserIdButNoEmail_Returns401()
    {
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, TestData.TestUserId) };
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"))
        };

        var result = AuthEndpoints.GetCurrentUser(httpContext);

        Assert.That(result, Is.TypeOf<ProblemHttpResult>());
    }
}
