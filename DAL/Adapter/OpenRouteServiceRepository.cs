using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using DAL.Interface;
using Microsoft.Extensions.Configuration;

namespace DAL.Adapter;

public class OpenRouteServiceRepository(IHttpClientFactory httpClientFactory, IConfiguration configuration) : IRouteRepository
{
    public async Task<(double Distance, double Duration)> ResolveRouteAsync(
        (double Latitude, double Longitude) from,
        (double Latitude, double Longitude) to,
        string transportType,
        CancellationToken cancellationToken = default
    )
    {
        var endpoint = GetEndpointForTransportType(transportType);
        var apiKey = configuration["AppSettings:OpenRouteServiceApiKey"]
                     ?? throw new InvalidOperationException("OpenRouteService API key is not configured.");
        var baseUrl = configuration["AppSettings:OpenRouteServiceApiBaseUrl"]
                      ?? throw new InvalidOperationException("OpenRouteService base URL is not configured.");

        using var client = httpClientFactory.CreateClient("OpenRouteService");
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        using var content = new StringContent(
            JsonSerializer.Serialize(new
            {
                coordinates = (double[][])[[from.Longitude, from.Latitude], [to.Longitude, to.Latitude]]
            }),
            Encoding.UTF8,
            "application/json"
        );

        var response = await client.PostAsync($"{baseUrl}/v2/directions/{endpoint}", content, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var json = JsonDocument.Parse(
            await response.Content.ReadAsStreamAsync(cancellationToken)
        );
        var summary = json.RootElement.GetProperty("routes")[0].GetProperty("summary");
        return (
            summary.GetProperty("distance").GetDouble(),
            summary.GetProperty("duration").GetDouble()
        );
    }

    private static string GetEndpointForTransportType(string transportType)
    {
        return transportType switch
        {
            "Car" => "driving-car",
            "Bike" => "cycling-regular",
            "Foot" => "foot-walking",
            _ => throw new ArgumentOutOfRangeException(nameof(transportType), transportType, "Unsupported transport type")
        };
    }
}
