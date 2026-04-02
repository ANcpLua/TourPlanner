using System.Net;
using System.Net.Http.Json;
using API.Endpoints;

namespace Tests.API.Integration;

[TestFixture]
public sealed class TourApiIntegrationTests : ApiIntegrationTestBase
{
    [Test]
    public async Task CreateTourAsync_ValidRequest_RoundTripsThroughListEndpoint()
    {
        await AuthenticateAsync();

        var createdTour = await CreateTourAsync(request: NewTourDto(name: "Alpine Escape"));
        var tours = await GetToursAsync();
        var persistedTour = tours.Single(t => t.Id == createdTour.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(tours, Has.Count.EqualTo(1));
            Assert.That(persistedTour.Name, Is.EqualTo("Alpine Escape"));
            Assert.That(persistedTour.From, Is.EqualTo("Vienna"));
            Assert.That(persistedTour.To, Is.EqualTo("Graz"));
        }
    }

    [Test]
    public async Task Tours_AreIsolatedPerAuthenticatedUser()
    {
        var firstClient = Client;
        var secondClient = CreateClient();

        try
        {
            await AuthenticateAsync(firstClient);
            await AuthenticateAsync(secondClient);

            var createdTour = await CreateTourAsync(firstClient, NewTourDto(name: "Private Tour"));
            var firstUserTours = await GetToursAsync(firstClient);
            var secondUserTours = await GetToursAsync(secondClient);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(firstUserTours.Select(static t => t.Id), Contains.Item(createdTour.Id));
                Assert.That(secondUserTours, Is.Empty);
            }
        }
        finally
        {
            secondClient.Dispose();
        }
    }

    [Test]
    public async Task CreateTourAsync_InvalidPayload_ReturnsValidationProblem()
    {
        await AuthenticateAsync();

        var request = NewTourDto();
        request.Name = null!;

        var response = await Client.PostAsJsonAsync(ApiRoute.Tour.Base, request);
        var body = await response.Content.ReadAsStringAsync();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(body, Does.Contain("Name is required"));
        }
    }

    [Test]
    public async Task UpdateTourAsync_MismatchedRouteId_ReturnsBadRequest()
    {
        await AuthenticateAsync();

        var request = NewTourDto(name: "Mismatch Tour");
        var response = await Client.PutAsJsonAsync(ApiRoute.Tour.ById(Guid.NewGuid()), request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(await response.Content.ReadAsStringAsync(), Does.Contain("ID mismatch"));
    }

    [Test]
    public async Task SearchToursAsync_MatchesTourLogComments()
    {
        await AuthenticateAsync();

        var matchingTour = await CreateTourAsync(request: NewTourDto(name: "Comment Search Tour"));
        await CreateTourLogAsync(matchingTour.Id, request: NewTourLogDto(matchingTour.Id, comment: "glacier-marker"));
        await CreateTourAsync(request: NewTourDto(name: "Unrelated Tour"));

        var response = await Client.GetAsync(ApiRoute.Tour.SearchByText("glacier-marker"));
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), await response.Content.ReadAsStringAsync());

        var tours = (await response.Content.ReadFromJsonAsync<List<Contracts.Tours.TourDto>>())!;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(tours, Has.Count.EqualTo(1));
            Assert.That(tours.Single().Id, Is.EqualTo(matchingTour.Id));
        }
    }

    [Test]
    public async Task DeleteTourAsync_RemovesPersistedTour()
    {
        await AuthenticateAsync();

        var createdTour = await CreateTourAsync();
        var deleteResponse = await Client.DeleteAsync(ApiRoute.Tour.ById(createdTour.Id));
        Assert.That(deleteResponse.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        var tours = await GetToursAsync();
        Assert.That(tours, Is.Empty);
    }
}
