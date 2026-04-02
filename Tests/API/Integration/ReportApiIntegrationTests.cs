using System.Net;
using System.Net.Http.Json;
using API.Endpoints;

namespace Tests.API.Integration;

[TestFixture]
public sealed class ReportApiIntegrationTests : ApiIntegrationTestBase
{
    [Test]
    public async Task GetSummaryReport_ReturnsPdfDocument()
    {
        await AuthenticateAsync();
        await CreateTourAsync(request: NewTourDto(name: "Summary Tour"));

        var response = await Client.GetAsync(ApiRoute.Reports.Summary);
        await AssertPdfResponseAsync(response);
    }

    [Test]
    public async Task GetTourReport_WhenTourExists_ReturnsPdfDocument()
    {
        await AuthenticateAsync();
        var tour = await CreateTourAsync(request: NewTourDto(name: "Report Tour"));

        var response = await Client.GetAsync(ApiRoute.Reports.TourById(tour.Id));
        await AssertPdfResponseAsync(response);
    }

    [Test]
    public async Task GetTourReport_WhenTourDoesNotExist_ReturnsNotFound()
    {
        await AuthenticateAsync();

        var response = await Client.GetAsync(ApiRoute.Reports.TourById(Guid.NewGuid()));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task ExportTourToJson_ReturnsPersistedTourPayload()
    {
        await AuthenticateAsync();
        var tour = await CreateTourAsync(request: NewTourDto(name: "Exported Tour"));

        var response = await Client.GetAsync(ApiRoute.Reports.ExportById(tour.Id));
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), await response.Content.ReadAsStringAsync());

        var exportedTour = (await response.Content.ReadFromJsonAsync<Contracts.Tours.TourDto>())!;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(exportedTour.Id, Is.EqualTo(tour.Id));
            Assert.That(exportedTour.Name, Is.EqualTo("Exported Tour"));
        }
    }

    [Test]
    public async Task ImportTourFromJsonAsync_ValidPayload_CreatesTourVisibleThroughApi()
    {
        await AuthenticateAsync();

        var importedTour = TourTestData.SampleTourDomain("Imported Tour");
        importedTour.Id = Guid.NewGuid();

        var response = await Client.PostAsJsonAsync(ApiRoute.Reports.Import, JsonSerializer.Serialize(importedTour));
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), await response.Content.ReadAsStringAsync());

        var tours = await GetToursAsync();
        Assert.That(tours.Select(static t => t.Name), Contains.Item("Imported Tour"));
    }

    [Test]
    public async Task ImportTourFromJsonAsync_InvalidPayload_ReturnsBadRequest()
    {
        await AuthenticateAsync();

        var response = await Client.PostAsJsonAsync(ApiRoute.Reports.Import, "not-json");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(await response.Content.ReadAsStringAsync(), Does.Contain("Invalid or empty tour data."));
    }
}
