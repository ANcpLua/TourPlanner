using System.Net;
using UI.Service;
using UI.Service.Interface;

namespace Test.UI.Services;

[TestFixture]
public class HttpServiceTests
{
    private Mock<HttpMessageHandler> _mockHttpMessageHandler = null!;
    private HttpClient _httpClient = null!;
    private Mock<IToastServiceWrapper> _mockToastService = null!;
    private Mock<ILogger> _mockLogger = null!;
    private HttpService _httpService = null!;

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

    [Test]
    public async Task GetAsync_SuccessfulRequest_ReturnsDeserializedObject()
    {
        var tour = TestData.SampleTour();
        var jsonResponse = JsonSerializer.Serialize(tour);
        TestData.SetupHttpMessageHandlerSuccess(_mockHttpMessageHandler, jsonResponse);

        var result = await _httpService.GetAsync<dynamic>("api/test");

        Assert.That(result, Is.Not.Null);
        TestData.VerifyHttpRequest(_mockHttpMessageHandler, HttpMethod.Get, "api/test");
    }

    [Test]
    public async Task GetAsync_HttpRequestException_HandlesError()
    {
        TestData.SetupHttpMessageHandlerError(_mockHttpMessageHandler, HttpStatusCode.NotFound, "Not Found");

        await _httpService.GetAsync<dynamic>("api/test");

        TestData.VerifyHttpRequest(_mockHttpMessageHandler, HttpMethod.Get, "api/test");
        _mockToastService.Verify(t => t.ShowError(It.IsAny<string>()), Times.Once);
    }

    [Test]
    public async Task GetListAsync_SuccessfulRequest_ReturnsCollection()
    {
        var tours = TestData.SampleTourList(2);
        var jsonResponse = JsonSerializer.Serialize(tours);
        TestData.SetupHttpMessageHandlerSuccess(_mockHttpMessageHandler, jsonResponse);

        var result = await _httpService.GetListAsync<dynamic>("api/tours");

        Assert.That(result, Is.Not.Null);
        TestData.VerifyHttpRequest(_mockHttpMessageHandler, HttpMethod.Get, "api/tours");
    }

    [Test]
    public async Task PostAsync_WithGenericReturn_SendsDataAndReturnsResult()
    {
        var tour = TestData.SampleTour();
        var jsonResponse = JsonSerializer.Serialize(tour);
        TestData.SetupHttpMessageHandlerSuccess(_mockHttpMessageHandler, jsonResponse);

        var result = await _httpService.PostAsync<dynamic>("api/tours", tour);

        Assert.That(result, Is.Not.Null);
        TestData.VerifyHttpRequest(_mockHttpMessageHandler, HttpMethod.Post, "api/tours");
    }

    [Test]
    public async Task PostAsync_VoidReturn_SendsDataSuccessfully()
    {
        var tour = TestData.SampleTour();
        TestData.SetupHttpMessageHandlerSuccess(_mockHttpMessageHandler, "");

        await _httpService.PostAsync("api/tours", tour);

        TestData.VerifyHttpRequest(_mockHttpMessageHandler, HttpMethod.Post, "api/tours");
    }

    [Test]
    public async Task PutAsync_SendsDataAndReturnsResult()
    {
        var tour = TestData.SampleTour();
        var jsonResponse = JsonSerializer.Serialize(tour);
        TestData.SetupHttpMessageHandlerSuccess(_mockHttpMessageHandler, jsonResponse);

        var result = await _httpService.PutAsync<dynamic>($"api/tours/{tour.Id}", tour);

        Assert.That(result, Is.Not.Null);
        TestData.VerifyHttpRequest(_mockHttpMessageHandler, HttpMethod.Put, $"api/tours/{tour.Id}");
    }

    [Test]
    public async Task DeleteAsync_SendsDeleteRequest()
    {
        var tourId = TestData.TestGuid;
        TestData.SetupHttpMessageHandlerSuccess(_mockHttpMessageHandler, "");

        await _httpService.DeleteAsync($"api/tours/{tourId}");

        TestData.VerifyHttpRequest(_mockHttpMessageHandler, HttpMethod.Delete, $"api/tours/{tourId}");
    }

    [Test]
    public async Task GetStringAsync_ReturnsStringContent()
    {
        const string expectedContent = "Sample tour data";
        TestData.SetupHttpMessageHandlerSuccess(_mockHttpMessageHandler, expectedContent);

        var result = await _httpService.GetStringAsync("api/tours/export");

        Assert.That(result, Is.EqualTo(expectedContent));
        TestData.VerifyHttpRequest(_mockHttpMessageHandler, HttpMethod.Get, "api/tours/export");
    }

    [Test]
    public async Task GetByteArrayAsync_SuccessfulRequest_ReturnsBytes()
    {
        var expectedBytes = new byte[] { 1, 2, 3, 4, 5 };
        TestData.SetupHttpMessageHandlerBytes(_mockHttpMessageHandler, expectedBytes);

        var result = await _httpService.GetByteArrayAsync("api/reports/tour");

        Assert.That(result, Is.EqualTo(expectedBytes));
        TestData.VerifyHttpRequest(_mockHttpMessageHandler, HttpMethod.Get, "api/reports/tour");
    }

    [Test]
    public async Task GetByteArrayAsync_EmptyResponse_ReturnsEmptyArray()
    {
        TestData.SetupHttpMessageHandlerBytes(_mockHttpMessageHandler, []);

        var result = await _httpService.GetByteArrayAsync("api/reports/empty");

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Has.Length.EqualTo(0));
    }

    [Test]
    public async Task PostAsync_WithNullData_SendsRequestWithoutContent()
    {
        TestData.SetupHttpMessageHandlerSuccess(_mockHttpMessageHandler, "{}");

        await _httpService.PostAsync<dynamic>("api/test", null);

        TestData.VerifyHttpRequest(_mockHttpMessageHandler, HttpMethod.Post, "api/test");
    }

    [Test]
    public async Task PutAsync_WithNullData_SendsRequestWithoutContent()
    {
        TestData.SetupHttpMessageHandlerSuccess(_mockHttpMessageHandler, "{}");

        await _httpService.PutAsync<dynamic>("api/test", null);

        TestData.VerifyHttpRequest(_mockHttpMessageHandler, HttpMethod.Put, "api/test");
    }

    [Test]
    public async Task PostAsync_VoidWithNullData_SendsRequestWithoutContent()
    {
        TestData.SetupHttpMessageHandlerSuccess(_mockHttpMessageHandler, "");

        await _httpService.PostAsync("api/test", null);

        TestData.VerifyHttpRequest(_mockHttpMessageHandler, HttpMethod.Post, "api/test");
    }

    [Test]
    public async Task SendRequestAsync_SuccessfulRequest_CompletesSuccessfully()
    {
        TestData.SetupHttpMessageHandlerSuccess(_mockHttpMessageHandler, "");

        await _httpService.SendRequestAsync(HttpMethod.Delete, "api/test");

        TestData.VerifyHttpRequest(_mockHttpMessageHandler, HttpMethod.Delete, "api/test");
    }

    [Test]
    public async Task SendRequestAsync_WithPostData_SendsDataSuccessfully()
    {
        var testData = new { Name = "Test", Value = 123 };
        TestData.SetupHttpMessageHandlerSuccess(_mockHttpMessageHandler, "");

        await _httpService.SendRequestAsync(HttpMethod.Post, "api/test", testData);

        TestData.VerifyHttpRequest(_mockHttpMessageHandler, HttpMethod.Post, "api/test");
    }

    [Test]
    public async Task SendRequestAsync_FailedRequest_HandlesError()
    {
        TestData.SetupHttpMessageHandlerError(_mockHttpMessageHandler, HttpStatusCode.InternalServerError,
            "Server Error");

        await _httpService.SendRequestAsync(HttpMethod.Get, "api/test");

        _mockToastService.Verify(t => t.ShowError(It.IsAny<string>()), Times.Once);
        TestData.VerifyHttpRequest(_mockHttpMessageHandler, HttpMethod.Get, "api/test");
    }
}