using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace UI.Model;

public class Tour
{
    [JsonPropertyName("id")] public Guid Id { get; set; }

    [Required(ErrorMessage = "Name is required")]
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Description is required")]
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "From city is required")]
    [JsonPropertyName("from")]
    public string From { get; set; } = string.Empty;

    [Required(ErrorMessage = "To city is required")]
    [JsonPropertyName("to")]
    public string To { get; set; } = string.Empty;

    [JsonPropertyName("imagePath")] public string? ImagePath { get; set; }

    [JsonPropertyName("routeInformation")] public string? RouteInformation { get; set; }

    [JsonPropertyName("distance")] public double? Distance { get; set; }

    [JsonPropertyName("estimatedTime")] public double? EstimatedTime { get; set; }

    [Required(ErrorMessage = "Transport type is required")]
    [JsonPropertyName("transportType")]
    public string TransportType { get; set; } = string.Empty;

    public List<TourLog> TourLogs { get; set; } = [];

    [JsonIgnore]
    public string Popularity => TourLogs.Count switch
    {
        0 => "Not popular",
        < 2 => "Less popular",
        < 3 => "Moderately popular",
        < 4 => "Popular",
        _ => "Very popular"
    };

    [JsonIgnore]
    public double AverageRating => TourLogs
        .Where(x => x.Rating is not null)
        .Select(x => x.Rating!.Value)
        .DefaultIfEmpty()
        .Average();

    [JsonIgnore]
    public bool IsChildFriendly =>
        TourLogs.Count > 0 &&
        TourLogs.All(x => x is { Difficulty: <= 2, Rating: >= 3 });
}