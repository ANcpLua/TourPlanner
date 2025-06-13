using System.Drawing;
using System.Drawing.Imaging;
using System.Text;
using BL.DomainModel;
using BL.Service;

namespace Test.BL;

[TestFixture]
public class PdfReportServiceTests
{
    [SetUp]
    public void Setup()
    {
        _pdfReportService = new PdfReportService();
    }

    private PdfReportService _pdfReportService = null!;
    private const string PdfHeader = "%PDF";

    [Test]
    public void GenerateTourReport_ValidTour_ReturnsPdfBytes()
    {
        var tour = TestData.SampleTourDomain();
        var result = _pdfReportService.GenerateTourReport(tour);
        AssertValidPdf(result);
    }

    [Test]
    public void GenerateSummaryReport_ValidTours_ReturnsPdfBytes()
    {
        var tours = TestData.SampleTourDomainList();
        var result = _pdfReportService.GenerateSummaryReport(tours);
        AssertValidPdf(result);
    }

    [Test]
    public void GenerateSummaryReport_EmptyTourList_GeneratesEmptyReport()
    {
        var result = _pdfReportService.GenerateSummaryReport([]);
        AssertValidPdf(result);
    }

    [Test]
    public void GenerateTourReport_EmptyTour_GeneratesEmptyReport()
    {
        var result = _pdfReportService.GenerateTourReport(new TourDomain());
        AssertValidPdf(result);
    }

    [Test]
    public void GenerateTourReport_SpecialCharacters_HandlesSpecialCharacters()
    {
        var tour = TestData.SampleTourDomain();
        const string specialChars = "Special characters: áéíóú ñ ¿¡ € &<>\"'";

        tour.Name = specialChars;
        tour.Description = specialChars;
        tour.From = specialChars;
        tour.To = specialChars;
        tour.Logs =
        [
            new TourLogDomain
            {
                DateTime = DateTime.Now,
                Comment = specialChars,
                Difficulty = 5,
                Rating = 4,
                TotalDistance = 123,
                TotalTime = 68
            }
        ];

        var result = _pdfReportService.GenerateTourReport(tour);
        AssertValidPdf(result);
    }

    [Test]
    public void GenerateReport_LargeDataSets_HandlesLargeData()
    {
        var tours = Enumerable.Range(0, 100)
            .Select(_ => TestData.SampleTourDomain())
            .ToList();

        foreach (var tour in tours)
        {
            tour.Description = new string('A', 1000);
            tour.Logs = Enumerable.Range(0, 50)
                .Select(_ => TestData.SampleTourLogDomain())
                .ToList();
        }

        var result = _pdfReportService.GenerateSummaryReport(tours);
        AssertValidPdf(result);
    }

    [Test]
    public void GenerateTourReport_NullImagePath_HandlesNullPath()
    {
        var tour = TestData.SampleTourDomain();
        tour.ImagePath = null;

        var result = _pdfReportService.GenerateTourReport(tour);
        AssertValidPdf(result);
    }

    [Test]
    public void GenerateTourReport_EmptyImagePath_HandlesEmptyPath()
    {
        var tour = TestData.SampleTourDomain();
        tour.ImagePath = string.Empty;

        var result = _pdfReportService.GenerateTourReport(tour);
        AssertValidPdf(result);
    }

    [Test]
    public void GenerateTourReport_InvalidImagePath_HandlesInvalidPath()
    {
        var tour = TestData.SampleTourDomain();
        tour.ImagePath = "invalid/path/to/image.png";

        var result = _pdfReportService.GenerateTourReport(tour);
        AssertValidPdf(result);
    }

    [Test]
    public void GenerateTourReport_ValidImagePath_IncludesImageInPdf()
    {
        var tempImagePath = Path.GetTempFileName();

#pragma warning disable CA1416
        using (var bitmap = new Bitmap(1, 1))
        {
            bitmap.Save(tempImagePath, ImageFormat.Png);
        }
#pragma warning restore CA1416

        try
        {
            var tour = TestData.SampleTourDomain();
            tour.ImagePath = tempImagePath;

            var result = _pdfReportService.GenerateTourReport(tour);
            AssertValidPdf(result);
        }
        finally
        {
            if (File.Exists(tempImagePath))
                File.Delete(tempImagePath);
        }
    }

    [Test]
    public void GenerateTourReport_CorruptedImagePath_HandlesImageException()
    {
        var tempFilePath = Path.GetTempFileName();
        File.WriteAllText(tempFilePath, "This is not a valid image file content");

        try
        {
            var tour = TestData.SampleTourDomain();
            tour.ImagePath = tempFilePath;

            var result = _pdfReportService.GenerateTourReport(tour);
            AssertValidPdf(result);
        }
        finally
        {
            if (File.Exists(tempFilePath))
                File.Delete(tempFilePath);
        }
    }

    private static void AssertValidPdf(byte[] pdfBytes)
    {
        Assert.Multiple(() =>
        {
            Assert.That(pdfBytes, Is.Not.Null);
            Assert.That(pdfBytes, Is.Not.Empty);
            Assert.That(Encoding.UTF8.GetString(pdfBytes.Take(4).ToArray()), Is.EqualTo(PdfHeader));
        });
    }
}