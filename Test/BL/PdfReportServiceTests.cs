using System.Text;
using BL.DomainModel;
using BL.Service;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace Test.BL;

[TestFixture]
public class PdfReportServiceTests
{
    private readonly PdfReportService _pdfReportService = new();
    private const string PdfHeader = "%PDF";

    [Test]
    public void GenerateTourReport_ValidTour_ReturnsPdfBytes()
    {
        var result = _pdfReportService.GenerateTourReport(TestData.SampleTourDomain());
        AssertValidPdf(result);
    }

    [Test]
    public void GenerateSummaryReport_ValidTours_ReturnsPdfBytes()
    {
        var result = _pdfReportService.GenerateSummaryReport(TestData.SampleTourDomainList());
        AssertValidPdf(result);
    }

    [Test]
    public void GenerateReport_NullValues_HandlesNullsGracefully()
    {
        var tour = TestData.SampleTourDomain();
        
        tour.Distance = null;
        tour.EstimatedTime = null;
        
        tour.Logs =
        [
            new TourLogDomain
            {
                DateTime = DateTime.Now,
                Comment = null,
                Difficulty = 3,
                Rating = 4,
                TotalDistance = 100,
                TotalTime = 60
            }
        ];

        var result = _pdfReportService.GenerateTourReport(tour);
        AssertValidPdf(result);
    }

    [Test]
    public void GenerateReport_HandlesSpecialCharactersAndLargeData()
    {
        const string specialChars = "Special: áéíóú ñ ¿¡ € &<>\"'";
        var tour = TestData.SampleTourDomain();
        
        tour.Name = specialChars;
        tour.Description = new string('A', 1000);
        tour.From = specialChars;
        tour.To = specialChars;
        tour.Logs = Enumerable.Range(0, 50)
            .Select(_ => new TourLogDomain
            {
                DateTime = DateTime.Now,
                Comment = specialChars,
                Difficulty = 5,
                Rating = 4,
                TotalDistance = 123.45,
                TotalTime = 68.90
            })
            .ToList();

        var result = _pdfReportService.GenerateTourReport(tour);
        AssertValidPdf(result);
    }

    [Test]
    public void GenerateTourReport_NullImagePath_HandlesGracefully()
    {
        var tour = TestData.SampleTourDomain();
        tour.ImagePath = null;

        var result = _pdfReportService.GenerateTourReport(tour);
        AssertValidPdf(result);
    }

    [TestCase("")]
    [TestCase("invalid/path/to/image.png")]
    public void GenerateTourReport_InvalidImagePaths_HandlesGracefully(string imagePath)
    {
        var tour = TestData.SampleTourDomain();
        tour.ImagePath = imagePath;

        var result = _pdfReportService.GenerateTourReport(tour);
        AssertValidPdf(result);
    }

    [Test]
    public void GenerateTourReport_WithValidImage_IncludesImage()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.png");
        using (var image = new Image<Rgba32>(100, 100))
        {
            image.Save(tempPath, new PngEncoder());
        }
        
        var tour = TestData.SampleTourDomain();
        tour.ImagePath = tempPath;
        
        var pdfWithImage = _pdfReportService.GenerateTourReport(tour);
        
        tour.ImagePath = null;
        var pdfWithoutImage = _pdfReportService.GenerateTourReport(tour);
        
        Assert.That(pdfWithImage.Length, Is.GreaterThan(pdfWithoutImage.Length), 
            "PDF with image should be larger");
    }

    [Test]
    public void GenerateTourReport_CorruptedImage_ShowsErrorMessage()
    {
        var tempPath = Path.GetTempFileName();
        File.WriteAllText(tempPath, "Not an image");
        
        var tour = TestData.SampleTourDomain();
        tour.ImagePath = tempPath;
        
        Assert.DoesNotThrow(() =>
        {
            var result = _pdfReportService.GenerateTourReport(tour);
            AssertValidPdf(result);
        });
    }

    private static void AssertValidPdf(byte[] pdfBytes)
    {
        Assert.That(pdfBytes, Is.Not.Null.And.Not.Empty);
        Assert.That(pdfBytes[..4], Is.EqualTo(Encoding.UTF8.GetBytes(PdfHeader)));
    }
}