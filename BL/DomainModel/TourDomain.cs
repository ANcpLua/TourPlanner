namespace BL.DomainModel;

public class TourDomain
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required string From { get; set; }
    public required string To { get; set; }
    public required string TransportType { get; set; }
    public string? ImagePath { get; set; }
    public string? RouteInformation { get; set; }
    public double? Distance { get; set; }
    public double? EstimatedTime { get; set; }
    public List<TourLogDomain> Logs { get; set; } = [];
}
