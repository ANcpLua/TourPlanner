using System.Net;
using System.Net.Http.Json;

namespace Tests.Fixtures;

public static class HttpTestHelper
{
    public static (HttpClient Client, Mock<HttpMessageHandler> Handler) MockedClient()
    {
        var handler = new Mock<HttpMessageHandler>();
        SetupSuccess(handler, "[]");
        var client = new HttpClient(handler.Object) { BaseAddress = new Uri("https://test.invalid/") };
        return (client, handler);
    }

    public static void SetupHandler(
        Mock<HttpMessageHandler> handler,
        HttpMethod method,
        string urlContains,
        object responseBody,
        HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        handler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == method &&
                    req.RequestUri!.ToString().Contains(urlContains)),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(statusCode)
            {
                Content = responseBody is string s
                    ? new StringContent(s, Encoding.UTF8, "application/json")
                    : JsonContent.Create(responseBody, responseBody.GetType())
            });
    }

    public static void SetupHandlerBytes(
        Mock<HttpMessageHandler> handler,
        string urlContains,
        byte[] content,
        HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        handler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri!.ToString().Contains(urlContains)),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(statusCode)
            {
                Content = new ByteArrayContent(content)
            });
    }

    public static void VerifyHandler(
        Mock<HttpMessageHandler> handler,
        HttpMethod method,
        string urlContains,
        Times times)
    {
        handler
            .Protected()
            .Verify(
                "SendAsync",
                times,
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == method &&
                    req.RequestUri!.ToString().Contains(urlContains)),
                ItExpr.IsAny<CancellationToken>());
    }

    public static void SetupSuccess(Mock<HttpMessageHandler> mockHandler, object responseBody)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = responseBody is string s
                ? new StringContent(s, Encoding.UTF8, "application/json")
                : JsonContent.Create(responseBody, responseBody.GetType())
        };

        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
    }

    public static void SetupError(Mock<HttpMessageHandler> mockHandler, HttpStatusCode statusCode, string content)
    {
        var response = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(content, Encoding.UTF8, "text/plain")
        };

        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
    }

    public static void VerifyPostRequest(Mock<HttpMessageHandler> mockHandler, string expectedEndpoint)
    {
        mockHandler
            .Protected()
            .Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri!.ToString().Contains(expectedEndpoint)),
                ItExpr.IsAny<CancellationToken>());
    }

    public static void VerifyRequestHeaders(Mock<HttpMessageHandler> mockHandler, string expectedApiKey)
    {
        mockHandler
            .Protected()
            .Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Headers.Authorization != null &&
                    req.Headers.Authorization.Scheme == "Bearer" &&
                    req.Headers.Authorization.Parameter == expectedApiKey &&
                    req.Headers.Accept.Any(static h => h.MediaType == "application/json")),
                ItExpr.IsAny<CancellationToken>());
    }

    public static void VerifyJsonContent<T>(
        Mock<HttpMessageHandler> mockHandler,
        Func<T, bool> predicate)
    {
        mockHandler
            .Protected()
            .Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Content != null &&
                    req.Content.Headers.ContentType!.MediaType == "application/json" &&
                    predicate(
                        req.Content.ReadFromJsonAsync<T>().GetAwaiter().GetResult()!
                    )),
                ItExpr.IsAny<CancellationToken>());
    }
}
