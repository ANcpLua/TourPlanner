using System.Net;
using System.Net.Http.Json;
using API.Endpoints;
using Contracts.Auth;
using Contracts.TourLogs;
using Contracts.Tours;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Tests.API.Integration;

public abstract class ApiIntegrationTestBase
{
    protected TourPlannerApplication App { get; private set; } = null!;
    protected HttpClient Client { get; private set; } = null!;

    [SetUp]
    public async Task BaseSetUp()
    {
        await PostgreSqlContainerHost.EnsureStartedAsync();
        App = CreateApplication();
        await App.InitializeDatabaseAsync();
        Client = CreateClient();
    }

    [TearDown]
    public void BaseTearDown()
    {
        Client.Dispose();
        App.Dispose();
    }

    protected virtual TourPlannerApplication CreateApplication() => new();

    protected HttpClient CreateClient()
    {
        return App.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    protected static TourDto NewTourDto(
        string name = "Alpine Loop",
        string description = "Scenic mountain route",
        string from = "Vienna",
        string to = "Graz",
        string transportType = "Car")
    {
        return new TourDto
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            From = from,
            To = to,
            TransportType = transportType,
            Distance = 120.5,
            EstimatedTime = 95.0,
            ImagePath = null,
            RouteInformation = "A1 motorway",
            TourLogs = []
        };
    }

    protected static TourLogDto NewTourLogDto(
        Guid tourId,
        string comment = "Strong climb with great views",
        double difficulty = 3,
        double totalDistance = 42.5,
        double totalTime = 180,
        double rating = 4)
    {
        return new TourLogDto
        {
            Id = Guid.NewGuid(),
            TourId = tourId,
            DateTime = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc),
            Comment = comment,
            Difficulty = difficulty,
            TotalDistance = totalDistance,
            TotalTime = totalTime,
            Rating = rating
        };
    }

    protected async Task<(string Email, string Password)> AuthenticateAsync(HttpClient? client = null)
    {
        var target = client ?? Client;
        var email = $"user-{Guid.NewGuid():N}@example.com";
        const string password = "Passw0rd!";

        var response = await target.PostAsJsonAsync(ApiRoute.Auth.RegisterPath, new RegisterRequest
        {
            Email = email,
            Password = password
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), await response.Content.ReadAsStringAsync());
        return (email, password);
    }

    protected async Task<TourDto> CreateTourAsync(HttpClient? client = null, TourDto? request = null)
    {
        var target = client ?? Client;
        var response = await target.PostAsJsonAsync(ApiRoute.Tour.Base, request ?? NewTourDto());

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<TourDto>())!;
    }

    protected async Task<TourLogDto> CreateTourLogAsync(Guid tourId, HttpClient? client = null, TourLogDto? request = null)
    {
        var target = client ?? Client;
        var response = await target.PostAsJsonAsync(ApiRoute.TourLog.Base, request ?? NewTourLogDto(tourId));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<TourLogDto>())!;
    }

    protected async Task<IReadOnlyList<TourDto>> GetToursAsync(HttpClient? client = null)
    {
        var target = client ?? Client;
        var response = await target.GetAsync(ApiRoute.Tour.Base);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<List<TourDto>>())!;
    }

    protected async Task<IReadOnlyList<TourLogDto>> GetTourLogsAsync(Guid tourId, HttpClient? client = null)
    {
        var target = client ?? Client;
        var response = await target.GetAsync(ApiRoute.TourLog.ByTourId(tourId));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<List<TourLogDto>>())!;
    }

    protected static async Task AssertPdfResponseAsync(HttpResponseMessage response)
    {
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), await response.Content.ReadAsStringAsync());
        Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/pdf"));

        var bytes = await response.Content.ReadAsByteArrayAsync();
        PdfAssertions.AssertValidPdf(bytes);
    }
}
