using System.Net;
using System.Net.Http.Json;
using API.Endpoints;

namespace Tests.API.Integration;

[TestFixture]
public sealed class TourLogApiIntegrationTests : ApiIntegrationTestBase
{
    [Test]
    public async Task CreateTourLogAsync_ValidRequest_RoundTripsThroughListAndGetById()
    {
        await AuthenticateAsync();
        var tour = await CreateTourAsync();

        var createdLog = await CreateTourLogAsync(tour.Id, request: NewTourLogDto(tour.Id, comment: "Summit reached"));
        var logs = await GetTourLogsAsync(tour.Id);
        var getByIdResponse = await Client.GetAsync(ApiRoute.TourLog.ById(createdLog.Id));

        Assert.That(getByIdResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), await getByIdResponse.Content.ReadAsStringAsync());
        var retrievedLog = (await getByIdResponse.Content.ReadFromJsonAsync<Contracts.TourLogs.TourLogDto>())!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(logs.Select(static l => l.Id), Contains.Item(createdLog.Id));
            Assert.That(retrievedLog.Comment, Is.EqualTo("Summit reached"));
            Assert.That(retrievedLog.TourId, Is.EqualTo(tour.Id));
        }
    }

    [Test]
    public async Task CreateTourLogAsync_InvalidPayload_ReturnsValidationProblem()
    {
        await AuthenticateAsync();
        var tour = await CreateTourAsync();

        var invalidLog = NewTourLogDto(tour.Id, rating: 8);
        var response = await Client.PostAsJsonAsync(ApiRoute.TourLog.Base, invalidLog);
        var body = await response.Content.ReadAsStringAsync();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(body, Does.Contain("Rating must be between 1 and 5"));
        }
    }

    [Test]
    public async Task UpdateTourLogAsync_MismatchedRouteId_ReturnsBadRequest()
    {
        await AuthenticateAsync();
        var tour = await CreateTourAsync();
        var log = NewTourLogDto(tour.Id);

        var response = await Client.PutAsJsonAsync(ApiRoute.TourLog.ById(Guid.NewGuid()), log);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(await response.Content.ReadAsStringAsync(), Does.Contain("ID mismatch"));
    }

    [Test]
    public async Task DeleteTourLogAsync_RemovesPersistedLog()
    {
        await AuthenticateAsync();
        var tour = await CreateTourAsync();
        var log = await CreateTourLogAsync(tour.Id);

        var deleteResponse = await Client.DeleteAsync(ApiRoute.TourLog.ById(log.Id));
        Assert.That(deleteResponse.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        var logs = await GetTourLogsAsync(tour.Id);
        Assert.That(logs, Is.Empty);
    }

    [Test]
    public async Task GetTourLogById_IsUserScoped()
    {
        var firstClient = Client;
        var secondClient = CreateClient();

        try
        {
            await AuthenticateAsync(firstClient);
            await AuthenticateAsync(secondClient);

            var tour = await CreateTourAsync(firstClient);
            var log = await CreateTourLogAsync(tour.Id, firstClient);
            var response = await secondClient.GetAsync(ApiRoute.TourLog.ById(log.Id));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }
        finally
        {
            secondClient.Dispose();
        }
    }
}
