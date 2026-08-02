using System.Xml;
using System.Xml.Linq;
using Contracts.Reports;

namespace API.Endpoints;

internal static class TourXmlParser
{
    private const string XmlSchemaInstanceNamespace = "http://www.w3.org/2001/XMLSchema-instance";
    private const int MaximumNameLength = 200;
    private const int MaximumDescriptionLength = 500;
    private const int MaximumLocationLength = 100;
    private const int MaximumTransportTypeLength = 50;
    private const int MaximumImagePathLength = 10_000;
    private const int MaximumRouteInformationLength = 30_000;
    private const int MaximumCommentLength = 500;
    private const int MaxTourLogCount = 1_000;

    private static readonly XName NilAttributeName = XName.Get("nil", XmlSchemaInstanceNamespace);

    public static TourXmlParseResult Parse(string xml)
    {
        try
        {
            return new TourXmlParseResult.Parsed(Read(xml));
        }
        catch (TourXmlFormatException exception)
        {
            return new TourXmlParseResult.Invalid(exception.Message);
        }
    }

    private static TourXmlDocument Read(string xml)
    {
        if (string.IsNullOrWhiteSpace(xml))
            throw Invalid("The XML document is required.");

        if (xml.Length > TourXmlDocument.MaximumDocumentCharacters)
            throw Invalid($"The XML document cannot exceed {TourXmlDocument.MaximumDocumentCharacters} characters.");

        XDocument source;
        try
        {
            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                MaxCharactersInDocument = TourXmlDocument.MaximumDocumentCharacters,
                MaxCharactersFromEntities = 0,
                IgnoreComments = false,
                IgnoreProcessingInstructions = false,
                IgnoreWhitespace = false
            };

            using var textReader = new StringReader(xml);
            using var reader = XmlReader.Create(textReader, settings);
            source = XDocument.Load(reader, LoadOptions.PreserveWhitespace);
        }
        catch (XmlException exception)
        {
            throw Invalid($"Malformed XML: {exception.Message}");
        }

        ValidateDocumentNodes(source);

        var root = source.Root;
        if (root is null || root.Name != XName.Get(TourXmlDocument.RootElementName))
            throw Invalid($"The root element must be '{TourXmlDocument.RootElementName}'.");

        ValidateContainerAttributes(root);

        var result = new TourXmlDocument();
        var tourLogs = new List<TourLogXmlItem>();
        var seenElements = new HashSet<string>(StringComparer.Ordinal);

        foreach (var element in ChildElements(root, TourXmlDocument.RootElementName))
        {
            if (element.Name.NamespaceName.Length is not 0)
                throw Invalid($"Unknown element '{element.Name}'.");

            switch (element.Name.LocalName)
            {
                case TourXmlDocument.NameElementName:
                    EnsureSingle(seenElements, element.Name.LocalName);
                    result.Name = ReadRequiredString(element, MaximumNameLength);
                    break;
                case TourXmlDocument.DescriptionElementName:
                    EnsureSingle(seenElements, element.Name.LocalName);
                    result.Description = ReadRequiredString(element, MaximumDescriptionLength);
                    break;
                case TourXmlDocument.FromElementName:
                    EnsureSingle(seenElements, element.Name.LocalName);
                    result.From = ReadRequiredString(element, MaximumLocationLength);
                    break;
                case TourXmlDocument.ToElementName:
                    EnsureSingle(seenElements, element.Name.LocalName);
                    result.To = ReadRequiredString(element, MaximumLocationLength);
                    break;
                case TourXmlDocument.ImagePathElementName:
                    EnsureSingle(seenElements, element.Name.LocalName);
                    result.ImagePath = ReadOptionalString(element, MaximumImagePathLength);
                    break;
                case TourXmlDocument.RouteInformationElementName:
                    EnsureSingle(seenElements, element.Name.LocalName);
                    result.RouteInformation = ReadOptionalString(element, MaximumRouteInformationLength);
                    break;
                case TourXmlDocument.DistanceElementName:
                    EnsureSingle(seenElements, element.Name.LocalName);
                    result.Distance = ReadOptionalDouble(element, minimum: 0);
                    break;
                case TourXmlDocument.EstimatedTimeElementName:
                    EnsureSingle(seenElements, element.Name.LocalName);
                    result.EstimatedTime = ReadOptionalDouble(element, minimum: 0);
                    break;
                case TourXmlDocument.TransportTypeElementName:
                    EnsureSingle(seenElements, element.Name.LocalName);
                    result.TransportType = ReadRequiredString(element, MaximumTransportTypeLength);
                    break;
                case TourLogXmlItem.ElementName:
                    if (tourLogs.Count >= MaxTourLogCount)
                        throw Invalid($"A tour can contain at most {MaxTourLogCount} tour logs.");

                    tourLogs.Add(ReadTourLog(element));
                    break;
                default:
                    throw Invalid($"Unknown element '{element.Name.LocalName}'.");
            }
        }

        RequireElements(
            seenElements,
            TourXmlDocument.NameElementName,
            TourXmlDocument.DescriptionElementName,
            TourXmlDocument.FromElementName,
            TourXmlDocument.ToElementName,
            TourXmlDocument.TransportTypeElementName);

        if (result.TransportType is not ("Car" or "Bike" or "Foot"))
            throw Invalid($"Element '{TourXmlDocument.TransportTypeElementName}' must be Car, Bike, or Foot.");

        result.TourLogs = tourLogs.ToArray();
        return result;
    }

    private static TourLogXmlItem ReadTourLog(XElement source)
    {
        ValidateContainerAttributes(source);

        var result = new TourLogXmlItem();
        var seenElements = new HashSet<string>(StringComparer.Ordinal);

        foreach (var element in ChildElements(source, TourLogXmlItem.ElementName))
        {
            if (element.Name.NamespaceName.Length is not 0)
                throw Invalid($"Unknown element '{element.Name}'.");

            EnsureSingle(seenElements, element.Name.LocalName);
            switch (element.Name.LocalName)
            {
                case TourLogXmlItem.DateTimeElementName:
                    result.DateTime = ReadDateTime(element);
                    break;
                case TourLogXmlItem.CommentElementName:
                    result.Comment = ReadRequiredString(element, MaximumCommentLength);
                    break;
                case TourLogXmlItem.DifficultyElementName:
                    result.Difficulty = ReadRequiredDouble(element, minimum: 1, maximum: 5);
                    break;
                case TourLogXmlItem.TotalDistanceElementName:
                    result.TotalDistance = ReadRequiredDouble(element, minimum: 0);
                    break;
                case TourLogXmlItem.TotalTimeElementName:
                    result.TotalTime = ReadRequiredDouble(element, minimum: 0);
                    break;
                case TourLogXmlItem.RatingElementName:
                    result.Rating = ReadRequiredDouble(element, minimum: 1, maximum: 5);
                    break;
                default:
                    throw Invalid($"Unknown element '{element.Name.LocalName}'.");
            }
        }

        RequireElements(
            seenElements,
            TourLogXmlItem.DateTimeElementName,
            TourLogXmlItem.CommentElementName,
            TourLogXmlItem.DifficultyElementName,
            TourLogXmlItem.TotalDistanceElementName,
            TourLogXmlItem.TotalTimeElementName,
            TourLogXmlItem.RatingElementName);

        return result;
    }

    private static void ValidateDocumentNodes(XDocument document)
    {
        foreach (var node in document.Nodes())
        {
            if (node is XElement || node is XText text && string.IsNullOrWhiteSpace(text.Value))
                continue;

            throw Invalid("The XML document contains unsupported content outside its root element.");
        }
    }

    private static IEnumerable<XElement> ChildElements(XElement parent, string parentName)
    {
        foreach (var node in parent.Nodes())
        {
            if (node is XElement element)
            {
                yield return element;
                continue;
            }

            if (node is XText text && string.IsNullOrWhiteSpace(text.Value))
                continue;

            throw Invalid($"Element '{parentName}' contains unsupported content.");
        }
    }

    private static string ReadRequiredString(XElement element, int maximumLength)
    {
        var value = ReadRequiredScalarText(element);
        if (string.IsNullOrWhiteSpace(value))
            throw Invalid($"Element '{element.Name.LocalName}' requires a value.");

        ValidateLength(element.Name.LocalName, value, maximumLength);
        return value;
    }

    private static string? ReadOptionalString(XElement element, int maximumLength)
    {
        var value = ReadScalarText(element, allowNil: true);
        if (value is not null)
            ValidateLength(element.Name.LocalName, value, maximumLength);

        return value;
    }

    private static double? ReadOptionalDouble(XElement element, double minimum)
    {
        var value = ReadScalarText(element, allowNil: true);
        return value is null ? null : ParseDouble(element.Name.LocalName, value, minimum, maximum: null);
    }

    private static double ReadRequiredDouble(XElement element, double minimum, double? maximum = null)
    {
        var value = ReadRequiredScalarText(element);
        return ParseDouble(element.Name.LocalName, value, minimum, maximum);
    }

    private static double ParseDouble(string elementName, string text, double minimum, double? maximum)
    {
        double value;
        try
        {
            value = XmlConvert.ToDouble(text);
        }
        catch (FormatException)
        {
            throw Invalid($"Element '{elementName}' must contain an XML double.");
        }

        if (!double.IsFinite(value) || value < minimum || maximum is not null && value > maximum.Value)
        {
            var range = maximum is null ? $"at least {minimum}" : $"between {minimum} and {maximum.Value}";
            throw Invalid($"Element '{elementName}' must be finite and {range}.");
        }

        return value;
    }

    private static DateTime ReadDateTime(XElement element)
    {
        var value = ReadRequiredScalarText(element);
        try
        {
            return XmlConvert.ToDateTime(value, XmlDateTimeSerializationMode.RoundtripKind);
        }
        catch (FormatException)
        {
            throw Invalid($"Element '{element.Name.LocalName}' must contain an XML dateTime.");
        }
    }

    private static string? ReadScalarText(XElement element, bool allowNil)
    {
        var nilAttribute = ValidateScalarAttributes(element);
        if (nilAttribute is not null)
        {
            bool isNil;
            try
            {
                isNil = XmlConvert.ToBoolean(nilAttribute.Value);
            }
            catch (FormatException)
            {
                throw Invalid($"Element '{element.Name.LocalName}' has an invalid xsi:nil value.");
            }

            if (!isNil)
                throw Invalid($"Element '{element.Name.LocalName}' may only use xsi:nil='true'.");

            if (!allowNil)
                throw Invalid($"Element '{element.Name.LocalName}' requires a value.");

            if (element.Nodes().Any(static node => node is not XText text || !string.IsNullOrWhiteSpace(text.Value)))
                throw Invalid($"Nil element '{element.Name.LocalName}' cannot contain a value.");

            return null;
        }

        if (element.Nodes().Any(static node => node is not XText))
            throw Invalid($"Element '{element.Name.LocalName}' must contain text only.");

        return element.Value;
    }

    private static string ReadRequiredScalarText(XElement element) =>
        ReadScalarText(element, allowNil: false)
        ?? throw Invalid($"Element '{element.Name.LocalName}' requires a value.");

    private static XAttribute? ValidateScalarAttributes(XElement element)
    {
        XAttribute? nilAttribute = null;
        foreach (var attribute in element.Attributes())
        {
            if (attribute.IsNamespaceDeclaration)
                continue;

            if (attribute.Name == NilAttributeName && nilAttribute is null)
            {
                nilAttribute = attribute;
                continue;
            }

            throw Invalid($"Element '{element.Name.LocalName}' contains unknown attribute '{attribute.Name}'.");
        }

        return nilAttribute;
    }

    private static void ValidateContainerAttributes(XElement element)
    {
        var attribute = element.Attributes().FirstOrDefault(static candidate => !candidate.IsNamespaceDeclaration);
        if (attribute is not null)
            throw Invalid($"Element '{element.Name.LocalName}' contains unknown attribute '{attribute.Name}'.");
    }

    private static void EnsureSingle(HashSet<string> seenElements, string elementName)
    {
        if (!seenElements.Add(elementName))
            throw Invalid($"Element '{elementName}' cannot appear more than once.");
    }

    private static void RequireElements(HashSet<string> seenElements, params string[] requiredElements)
    {
        var missing = requiredElements.FirstOrDefault(element => !seenElements.Contains(element));
        if (missing is not null)
            throw Invalid($"Required element '{missing}' is missing.");
    }

    private static void ValidateLength(string elementName, string value, int maximumLength)
    {
        if (value.Length > maximumLength)
            throw Invalid($"Element '{elementName}' cannot exceed {maximumLength} characters.");
    }

    private static TourXmlFormatException Invalid(string message) => new(message);
}

internal sealed class TourXmlFormatException(string message) : Exception(message);

internal abstract record TourXmlParseResult
{
    private TourXmlParseResult()
    {
    }

    internal sealed record Parsed(TourXmlDocument Document) : TourXmlParseResult;

    internal sealed record Invalid(string Error) : TourXmlParseResult;
}
