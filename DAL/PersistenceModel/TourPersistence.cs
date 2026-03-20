using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.PersistenceModel;

public class TourPersistence
{
    public List<TourLogPersistence> TourLogPersistence { get; set; } = [];

    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }

    [Required] public required string UserId { get; set; }
    [Required] public required string Name { get; set; }
    [Required] public required string Description { get; set; }
    [Required] public required string From { get; set; }
    [Required] public required string To { get; set; }
    [Required] public required string TransportType { get; set; }

    public string? ImagePath { get; set; }
    public string? RouteInformation { get; set; }
    public double? Distance { get; set; }
    public double? EstimatedTime { get; set; }
}
