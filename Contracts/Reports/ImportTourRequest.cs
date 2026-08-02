using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Contracts.Reports;

public sealed class ImportTourRequest
{
    [Required]
    [StringLength(TourXmlDocument.MaximumDocumentCharacters, MinimumLength = 1)]
    [JsonPropertyName("xml")]
    public required string Xml { get; init; }
}
