using API.Endpoints;
using System.Net;

namespace Tests.API.Integration;

[TestFixture]
public sealed class FallbackPolicyTests : ApiIntegrationTestBase
{
    [TestCase(ApiRoute.Tour.Base)]
    [TestCase(ApiRoute.TourLog.Base)]
    [TestCase(ApiRoute.Routes.Resolve)]
    [TestCase(ApiRoute.Reports.Summary)]
    [TestCase(ApiRoute.Auth.MePath)]
    [TestCase(ApiRoute.Auth.LogoutPath)]
    public async Task ProtectedEndpoint_Anonymous_Returns401(string url)
    {
        var response = await Client.GetAsync(url);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [TestCase(ApiRoute.Health)]
    [TestCase(ApiRoute.OpenApiDocument)]
    public async Task AnonymousEndpoint_Returns200(string url)
    {
        var response = await Client.GetAsync(url);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }
}
