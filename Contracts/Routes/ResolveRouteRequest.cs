using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Contracts.Routes;

public sealed class ResolveRouteRequest
{
    [Required]
    [JsonPropertyName("fromLatitude")]
    public required double? FromLatitude { get; set; }

    [Required]
    [JsonPropertyName("fromLongitude")]
    public required double? FromLongitude { get; set; }

    [Required]
    [JsonPropertyName("toLatitude")]
    public required double? ToLatitude { get; set; }

    [Required]
    [JsonPropertyName("toLongitude")]
    public required double? ToLongitude { get; set; }

    [Required]
    [JsonPropertyName("transportType")]
    public required string TransportType { get; set; }
}
