using System.Net;
using System.Text;
using System.Text.Json;
using Moq;
using Moq.Protected;
using Serilog;
using UI.Service;
using UI.Service.Interface;

namespace Test.UI.Services;

[TestFixture]
public class HttpServiceTests
{
    [SetUp]
    public void Setup()
    {
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("https://test.com/")
        };
        _mockToastService = TestData.MockToastService();
        _mockLogger = TestData.MockLogger();
        _httpService = new HttpService(_httpClient, _mockToastService.Object, _mockLogger.Object);
    }

    [TearDown]
    public void TearDown()
    {
        _httpClient.Dispose();
    }

    private Mock<HttpMessageHandler> _mockHttpMessageHandler = null!;
    private HttpClient _httpClient = null!;
    private Mock<IToastServiceWrapper> _mockToastService = null!;
    private Mock<ILogger> _mockLogger = null!;
    private HttpService _httpService = null!;

    [Test]
    public async Task GetAsync_SuccessfulRequest_ReturnsDeserializedObject()
    {
        var tour = TestData.SampleTour();
        var jsonResponse = JsonSerializer.Serialize(tour);
        SetupHttpResponse(HttpStatusCode.OK, jsonResponse);

        var result = await _httpService.GetAsync<dynamic>("api/test");

        Assert.That(result, Is.Not.Null);
        VerifyHttpRequest(HttpMethod.Get, "api/test");
    }

    [Test]
    public async Task GetAsync_HttpRequestException_HandlesError()
    {
        SetupHttpResponse(HttpStatusCode.NotFound, "Not Found");

        await _httpService.GetAsync<dynamic>("api/test");

        VerifyHttpRequest(HttpMethod.Get, "api/test");
        _mockToastService.Verify(t => t.ShowError(It.IsAny<string>()), Times.Once);
    }

    [Test]
    public async Task GetListAsync_SuccessfulRequest_ReturnsCollection()
    {
        var tours = TestData.SampleTourList(2);
        var jsonResponse = JsonSerializer.Serialize(tours);
        SetupHttpResponse(HttpStatusCode.OK, jsonResponse);

        var result = await _httpService.GetListAsync<dynamic>("api/tours");

        Assert.That(result, Is.Not.Null);
        VerifyHttpRequest(HttpMethod.Get, "api/tours");
    }

    [Test]
    public async Task PostAsync_WithGenericReturn_SendsDataAndReturnsResult()
    {
        var tour = TestData.SampleTour();
        var jsonResponse = JsonSerializer.Serialize(tour);
        SetupHttpResponse(HttpStatusCode.OK, jsonResponse);

        var result = await _httpService.PostAsync<dynamic>("api/tours", tour);

        Assert.That(result, Is.Not.Null);
        VerifyHttpRequest(HttpMethod.Post, "api/tours");
    }

    [Test]
    public async Task PostAsync_VoidReturn_SendsDataSuccessfully()
    {
        var tour = TestData.SampleTour();
        SetupHttpResponse(HttpStatusCode.OK, "");

        await _httpService.PostAsync("api/tours", tour);

        VerifyHttpRequest(HttpMethod.Post, "api/tours");
    }

    [Test]
    public async Task PutAsync_SendsDataAndReturnsResult()
    {
        var tour = TestData.SampleTour();
        var jsonResponse = JsonSerializer.Serialize(tour);
        SetupHttpResponse(HttpStatusCode.OK, jsonResponse);

        var result = await _httpService.PutAsync<dynamic>($"api/tours/{tour.Id}", tour);

        Assert.That(result, Is.Not.Null);
        VerifyHttpRequest(HttpMethod.Put, $"api/tours/{tour.Id}");
    }

    [Test]
    public async Task DeleteAsync_SendsDeleteRequest()
    {
        var tourId = TestData.TestGuid;
        SetupHttpResponse(HttpStatusCode.OK, "");

        await _httpService.DeleteAsync($"api/tours/{tourId}");

        VerifyHttpRequest(HttpMethod.Delete, $"api/tours/{tourId}");
    }

    [Test]
    public async Task GetStringAsync_ReturnsStringContent()
    {
        const string expectedContent = "Sample tour data";
        SetupHttpResponse(HttpStatusCode.OK, expectedContent);

        var result = await _httpService.GetStringAsync("api/tours/export");

        Assert.That(result, Is.EqualTo(expectedContent));
        VerifyHttpRequest(HttpMethod.Get, "api/tours/export");
    }

    [Test]
    public async Task GetByteArrayAsync_SuccessfulRequest_ReturnsBytes()
    {
        var expectedBytes = new byte[] { 1, 2, 3, 4, 5 };
        SetupHttpResponse(HttpStatusCode.OK, expectedBytes);

        var result = await _httpService.GetByteArrayAsync("api/reports/tour");

        Assert.That(result, Is.EqualTo(expectedBytes));
        VerifyHttpRequest(HttpMethod.Get, "api/reports/tour");
    }
    
    [Test]
    public async Task GetByteArrayAsync_EmptyResponse_ReturnsEmptyArray()
    {
        SetupHttpResponse(HttpStatusCode.OK, []);

        var result = await _httpService.GetByteArrayAsync("api/reports/empty");

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Has.Length.EqualTo(0));
    }

    [Test]
    public async Task PostAsync_WithNullData_SendsRequestWithoutContent()
    {
        SetupHttpResponse(HttpStatusCode.OK, "{}");

        await _httpService.PostAsync<dynamic>("api/test", null);

        VerifyHttpRequest(HttpMethod.Post, "api/test");
    }

    [Test]
    public async Task PutAsync_WithNullData_SendsRequestWithoutContent()
    {
        SetupHttpResponse(HttpStatusCode.OK, "{}");

        await _httpService.PutAsync<dynamic>("api/test", null);

        VerifyHttpRequest(HttpMethod.Put, "api/test");
    }

    [Test]
    public async Task PostAsync_VoidWithNullData_SendsRequestWithoutContent()
    {
        SetupHttpResponse(HttpStatusCode.OK, "");

        await _httpService.PostAsync("api/test", null);

        VerifyHttpRequest(HttpMethod.Post, "api/test");
    }
    
    [Test]
    public async Task SendRequestAsync_SuccessfulRequest_CompletesSuccessfully()
    {
        SetupHttpResponse(HttpStatusCode.OK, "");
    
        await _httpService.SendRequestAsync(HttpMethod.Delete, "api/test");
    
        VerifyHttpRequest(HttpMethod.Delete, "api/test");
    }

    [Test]
    public async Task SendRequestAsync_WithPostData_SendsDataSuccessfully()
    {
        var testData = new { Name = "Test", Value = 123 };
        SetupHttpResponse(HttpStatusCode.OK, "");
    
        await _httpService.SendRequestAsync(HttpMethod.Post, "api/test", testData);
    
        VerifyHttpRequest(HttpMethod.Post, "api/test");
    }

    [Test]
    public async Task SendRequestAsync_FailedRequest_HandlesError()
    {
        SetupHttpResponse(HttpStatusCode.InternalServerError, "Server Error");
    
        await _httpService.SendRequestAsync(HttpMethod.Get, "api/test");
    
        _mockToastService.Verify(t => t.ShowError(It.IsAny<string>()), Times.Once);
        VerifyHttpRequest(HttpMethod.Get, "api/test");
    }

    private void SetupHttpResponse(HttpStatusCode statusCode, string content)
    {
        var response = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(content, Encoding.UTF8, "application/json")
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
    }

    private void SetupHttpResponse(HttpStatusCode statusCode, byte[] content)
    {
        var response = new HttpResponseMessage(statusCode)
        {
            Content = new ByteArrayContent(content)
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
    }

    private void VerifyHttpRequest(HttpMethod method, string uri)
    {
        _mockHttpMessageHandler
            .Protected()
            .Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == method &&
                    req.RequestUri!.ToString().Contains(uri)),
                ItExpr.IsAny<CancellationToken>());
    }
}