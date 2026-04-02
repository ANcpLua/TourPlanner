using System.Security.Claims;
using API.Infrastructure;
using Microsoft.AspNetCore.Http;

namespace Tests.API;

[TestFixture]
public class HttpUserContextTests
{
    [Test]
    public void UserId_AuthenticatedUser_ReturnsNameIdentifier()
    {
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, TestConstants.TestUserId) };
        var identity = new ClaimsIdentity(claims, "test");
        var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(static a => a.HttpContext).Returns(httpContext);

        var sut = new HttpUserContext(accessor.Object);

        Assert.That(sut.UserId, Is.EqualTo(TestConstants.TestUserId));
    }

    [Test]
    public void UserId_UnauthenticatedUser_ThrowsInvalidOperationException()
    {
        var httpContext = new DefaultHttpContext();
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(static a => a.HttpContext).Returns(httpContext);

        var sut = new HttpUserContext(accessor.Object);

        Assert.That(() => sut.UserId, Throws.TypeOf<InvalidOperationException>());
    }

    [Test]
    public void UserId_NullHttpContext_ThrowsInvalidOperationException()
    {
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(static a => a.HttpContext).Returns((HttpContext?)null);

        var sut = new HttpUserContext(accessor.Object);

        Assert.That(() => sut.UserId, Throws.TypeOf<InvalidOperationException>());
    }
}
