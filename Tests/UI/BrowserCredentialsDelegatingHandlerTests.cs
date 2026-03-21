using Tests.API.Integration;
using UI.Infrastructure;

namespace Tests.UI;

[TestFixture]
public class BrowserCredentialsDelegatingHandlerTests
{
    private TourPlannerApplication _app = null!;

    [SetUp]
    public void SetUp() => _app = new TourPlannerApplication();

    [TearDown]
    public void TearDown() => _app.Dispose();

    [Test]
    public async Task SendAsync_SetsBrowserRequestCredentialsToInclude()
    {
        using var client = _app.CreateDefaultClient(new BrowserCredentialsDelegatingHandler());
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health");

        await client.SendAsync(request);

        request.Options.TryGetValue(
            new HttpRequestOptionsKey<IDictionary<string, object>>("WebAssemblyFetchOptions"),
            out var fetchOptions);
        Assert.That(fetchOptions?["credentials"], Is.EqualTo("include"));
    }
}
