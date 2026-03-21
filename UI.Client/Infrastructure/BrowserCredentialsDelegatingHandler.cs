using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace UI.Infrastructure;

[ExcludeFromCodeCoverage(Justification = "Requires browser runtime — cannot be unit tested")]
public class BrowserCredentialsDelegatingHandler() : DelegatingHandler(new HttpClientHandler())
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);
        return base.SendAsync(request, cancellationToken);
    }
}
