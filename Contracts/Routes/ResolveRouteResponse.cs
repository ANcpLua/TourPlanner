using System.Text.Json.Serialization;

namespace Contracts.Routes;

public sealed class ResolveRouteResponse
{
    [JsonPropertyName("distance")]
    public required double Distance { get; set; }

    [JsonPropertyName("duration")]
    public required double Duration { get; set; }
}
