using System.Net;
using UI.Infrastructure;

namespace Tests.UI;

[TestFixture]
public sealed class BrowserCredentialsDelegatingHandlerTests
{
    [Test]
    public async Task SendAsync_SetsBrowserRequestCredentialsToInclude()
    {
        using var handler = new BrowserCredentialsDelegatingHandler
        {
            InnerHandler = new StubHttpMessageHandler()
        };
        using var client = new HttpClient(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://example.test/health");

        await client.SendAsync(request);

        request.Options.TryGetValue(
            new HttpRequestOptionsKey<IDictionary<string, object>>("WebAssemblyFetchOptions"),
            out var fetchOptions);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(fetchOptions, Is.Not.Null);
            Assert.That(fetchOptions!["credentials"], Is.EqualTo("include"));
        }
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}
