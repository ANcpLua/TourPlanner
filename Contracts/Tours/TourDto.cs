using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Contracts.TourLogs;

namespace Contracts.Tours;

public sealed class TourDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Name is required")]
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [Required(ErrorMessage = "Description is required")]
    [JsonPropertyName("description")]
    public required string Description { get; set; }

    [Required(ErrorMessage = "From city is required")]
    [JsonPropertyName("from")]
    public required string From { get; set; }

    [Required(ErrorMessage = "To city is required")]
    [JsonPropertyName("to")]
    public required string To { get; set; }

    [JsonPropertyName("imagePath")]
    public string? ImagePath { get; set; }

    [JsonPropertyName("routeInformation")]
    public string? RouteInformation { get; set; }

    [JsonPropertyName("distance")]
    public double? Distance { get; set; }

    [JsonPropertyName("estimatedTime")]
    public double? EstimatedTime { get; set; }

    [Required(ErrorMessage = "Transport type is required")]
    [JsonPropertyName("transportType")]
    public required string TransportType { get; set; }

    [JsonPropertyName("tourLogs")]
    public IReadOnlyList<TourLogDto> TourLogs { get; set; } = [];
}
