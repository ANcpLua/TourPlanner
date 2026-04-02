using API.Endpoints;
using System.Net;

namespace Tests.API.Integration;

[TestFixture]
public sealed class OpenApiDocumentTests : ApiIntegrationTestBase
{
    [Test]
    public async Task OpenApiDocument_ExposesConfiguredMetadataAndCookieSecurityScheme()
    {
        var response = await Client.GetAsync(ApiRoute.OpenApiDocument);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);

        var info = document.RootElement.GetProperty("info");
        var securitySchemes = document.RootElement
            .GetProperty("components")
            .GetProperty("securitySchemes");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(info.GetProperty("title").GetString(), Is.EqualTo("TourPlanner API"));
            Assert.That(info.GetProperty("version").GetString(), Is.EqualTo("v1"));
            Assert.That(securitySchemes.TryGetProperty("cookie", out var cookieScheme), Is.True);
            Assert.That(cookieScheme.GetProperty("type").GetString(), Is.EqualTo("apiKey"));
            Assert.That(cookieScheme.GetProperty("in").GetString(), Is.EqualTo("cookie"));
            Assert.That(cookieScheme.GetProperty("name").GetString(),
                Is.EqualTo(".AspNetCore.Identity.Application"));
        }
    }

    [Test]
    public async Task OpenApiDocument_AppliesSecurityToProtectedOperationsOnly()
    {
        var response = await Client.GetAsync(ApiRoute.OpenApiDocument);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);

        var paths = document.RootElement.GetProperty("paths");
        var tourGet = paths.GetProperty(ApiRoute.Tour.Base).GetProperty("get");
        var loginPost = paths.GetProperty(ApiRoute.Auth.LoginPath).GetProperty("post");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(tourGet.GetProperty("security").GetArrayLength(), Is.GreaterThan(0));
            Assert.That(loginPost.TryGetProperty("security", out _), Is.False);
        }
    }
}
