using System.Xml.Serialization;

namespace Contracts.Reports;

[XmlRoot(RootElementName)]
[TourPlanner.Xml.GenerateXml]
public sealed partial class TourXmlDocument
{
    public const int MaximumDocumentCharacters = 1_048_576;
    public const string RootElementName = "tour";
    public const string NameElementName = "name";
    public const string DescriptionElementName = "description";
    public const string FromElementName = "from";
    public const string ToElementName = "to";
    public const string ImagePathElementName = "imagePath";
    public const string RouteInformationElementName = "routeInformation";
    public const string DistanceElementName = "distance";
    public const string EstimatedTimeElementName = "estimatedTime";
    public const string TransportTypeElementName = "transportType";

    [XmlElement(NameElementName)]
    public string Name { get; set; } = string.Empty;

    [XmlElement(DescriptionElementName)]
    public string Description { get; set; } = string.Empty;

    [XmlElement(FromElementName)]
    public string From { get; set; } = string.Empty;

    [XmlElement(ToElementName)]
    public string To { get; set; } = string.Empty;

    [XmlElement(ImagePathElementName)]
    public string? ImagePath { get; set; }

    [XmlElement(RouteInformationElementName)]
    public string? RouteInformation { get; set; }

    [XmlElement(DistanceElementName)]
    public double? Distance { get; set; }

    [XmlElement(EstimatedTimeElementName)]
    public double? EstimatedTime { get; set; }

    [XmlElement(TransportTypeElementName)]
    public string TransportType { get; set; } = string.Empty;

    [XmlElement(TourLogXmlItem.ElementName)]
    public TourLogXmlItem[] TourLogs { get; set; } = [];
}

[TourPlanner.Xml.GenerateXml]
public sealed partial class TourLogXmlItem
{
    public const string ElementName = "tourLog";
    public const string DateTimeElementName = "dateTime";
    public const string CommentElementName = "comment";
    public const string DifficultyElementName = "difficulty";
    public const string TotalDistanceElementName = "totalDistance";
    public const string TotalTimeElementName = "totalTime";
    public const string RatingElementName = "rating";

    [XmlElement(DateTimeElementName)]
    public DateTime DateTime { get; set; }

    [XmlElement(CommentElementName)]
    public string? Comment { get; set; }

    [XmlElement(DifficultyElementName)]
    public double? Difficulty { get; set; }

    [XmlElement(TotalDistanceElementName)]
    public double? TotalDistance { get; set; }

    [XmlElement(TotalTimeElementName)]
    public double? TotalTime { get; set; }

    [XmlElement(RatingElementName)]
    public double? Rating { get; set; }
}
