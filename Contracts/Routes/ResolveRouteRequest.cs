using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Contracts.Routes;

public sealed class ResolveRouteRequest
{
    [Required]
    [Range(-90, 90)]
    [JsonPropertyName("fromLatitude")]
    public required double FromLatitude { get; set; }

    [Required]
    [Range(-180, 180)]
    [JsonPropertyName("fromLongitude")]
    public required double FromLongitude { get; set; }

    [Required]
    [Range(-90, 90)]
    [JsonPropertyName("toLatitude")]
    public required double ToLatitude { get; set; }

    [Required]
    [Range(-180, 180)]
    [JsonPropertyName("toLongitude")]
    public required double ToLongitude { get; set; }

    [Required]
    [AllowedValues("Car", "Bike", "Foot")]
    [JsonPropertyName("transportType")]
    public required string TransportType { get; set; }
}
