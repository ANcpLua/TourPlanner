using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Contracts.TourLogs;

public sealed class TourLogDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("tourId")]
    public Guid TourId { get; set; }

    [JsonPropertyName("dateTime")]
    public DateTime DateTime { get; set; }

    [Required]
    [JsonPropertyName("comment")]
    public string? Comment { get; set; }

    [Required]
    [Range(1, 5, ErrorMessage = "Difficulty must be between 1 and 5")]
    [JsonPropertyName("difficulty")]
    public double? Difficulty { get; set; }

    [Required]
    [Range(0, double.MaxValue, ErrorMessage = "Total Distance must be positive")]
    [JsonPropertyName("totalDistance")]
    public double? TotalDistance { get; set; }

    [Required]
    [Range(0, double.MaxValue, ErrorMessage = "Total Time must be positive")]
    [JsonPropertyName("totalTime")]
    public double? TotalTime { get; set; }

    [Required]
    [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
    [JsonPropertyName("rating")]
    public double? Rating { get; set; }
}
