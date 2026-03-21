using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Tests.API.Integration;

[TestFixture]
public class FallbackPolicyTests
{
    private TourPlannerApplication _app = null!;
    private HttpClient _client = null!;

    [SetUp]
    public void SetUp()
    {
        _app = new TourPlannerApplication();
        _client = _app.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    [TearDown]
    public void TearDown()
    {
        _client.Dispose();
        _app.Dispose();
    }

    [TestCase("/api/tour")]
    [TestCase("/api/tourlog")]
    [TestCase("/api/routes/resolve")]
    [TestCase("/api/reports/summary")]
    [TestCase("/api/auth/me")]
    [TestCase("/api/auth/logout")]
    public async Task ProtectedEndpoint_Anonymous_Returns401(string url)
    {
        var response = await _client.GetAsync(url);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [TestCase("/health")]
    public async Task AnonymousEndpoint_Returns200(string url)
    {
        var response = await _client.GetAsync(url);

        Assert.That(response.IsSuccessStatusCode, Is.True);
    }

    [Test]
    public async Task LoginEndpoint_Anonymous_DoesNotReturn401()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new { Email = "x", Password = "x" });

        Assert.That(response.StatusCode, Is.Not.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task RegisterEndpoint_Anonymous_DoesNotReturn401()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register",
            new { Email = "x", Password = "x" });

        Assert.That(response.StatusCode, Is.Not.EqualTo(HttpStatusCode.Unauthorized));
    }
}
