using System.Net;
using System.Net.Http.Json;
using System.Xml.Linq;
using API.Endpoints;
using Contracts.Reports;
using Contracts.Tours;

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
    public async Task ExportTourToXml_ReturnsPersistedTourWithoutIdentifiers()
    {
        await AuthenticateAsync();
        var tour = await CreateTourAsync(request: NewTourDto(name: "Exported Tour"));

        var response = await Client.GetAsync(ApiRoute.Reports.ExportById(tour.Id));
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), await response.Content.ReadAsStringAsync());

        var xml = await response.Content.ReadAsStringAsync();
        var document = XDocument.Parse(xml);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/xml"));
            Assert.That(document.Root?.Element(TourXmlDocument.NameElementName)?.Value, Is.EqualTo("Exported Tour"));
            Assert.That(document.Descendants("id"), Is.Empty);
        }
    }

    [Test]
    public async Task ImportTourFromXmlAsync_ValidPayload_CreatesTourVisibleThroughApi()
    {
        await AuthenticateAsync();

        var response = await Client.PostAsJsonAsync(ApiRoute.Reports.Import, new ImportTourRequest
        {
            Xml = TourTestData.SampleTourXml("Imported Tour")
        });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created), await response.Content.ReadAsStringAsync());

        var importedTour = (await response.Content.ReadFromJsonAsync<TourDto>())!;
        var tours = await GetToursAsync();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(importedTour.Id, Is.Not.EqualTo(Guid.Empty));
            Assert.That(importedTour.Name, Is.EqualTo("Imported Tour"));
            Assert.That(tours.Select(static t => t.Name), Contains.Item("Imported Tour"));
        }
    }

    [Test]
    public async Task ExportTourToXml_WhenTourDoesNotExist_ReturnsNotFound()
    {
        await AuthenticateAsync();

        var response = await Client.GetAsync(ApiRoute.Reports.ExportById(Guid.NewGuid()));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task ImportTourFromXmlAsync_InvalidPayload_ReturnsValidationProblem()
    {
        await AuthenticateAsync();

        var response = await Client.PostAsJsonAsync(ApiRoute.Reports.Import, new ImportTourRequest { Xml = "not-xml" });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(await response.Content.ReadAsStringAsync(), Does.Contain("Malformed XML"));
    }
}
