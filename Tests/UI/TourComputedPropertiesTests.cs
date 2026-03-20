using UI.Model;

namespace Tests.UI;

[TestFixture]
public class TourComputedPropertiesTests
{
    private static readonly TestCaseData[] ChildFriendlyCases =
    [
        new TestCaseData(new[] { 1d, 2d, 1d }, new double?[] { 3, 4, 5 }).Returns(true).SetName("AllGood"),
        new TestCaseData(new[] { 1d, 3d, 1d }, new double?[] { 4, 5, 4 }).Returns(false).SetName("HighDifficulty"),
        new TestCaseData(new[] { 1d, 2d, 1d }, new double?[] { 2, 4, 5 }).Returns(false).SetName("LowRating"),
        new TestCaseData(new[] { 1d }, new double?[] { null }).Returns(false).SetName("NullRating"),
        new TestCaseData(Array.Empty<double>(), Array.Empty<double?>()).Returns(false).SetName("NoLogs")
    ];

    [TestCase(0, ExpectedResult = "Not popular")]
    [TestCase(1, ExpectedResult = "Less popular")]
    [TestCase(2, ExpectedResult = "Moderately popular")]
    [TestCase(3, ExpectedResult = "Popular")]
    [TestCase(4, ExpectedResult = "Very popular")]
    [TestCase(10, ExpectedResult = "Very popular")]
    public string Popularity_WithLogCount_ReturnsExpectedValue(int logCount)
    {
        var tour = TestData.SampleTour();
        for (var i = 0; i < logCount; i++)
            tour.TourLogs.Add(new TourLog());

        return tour.Popularity;
    }

    [TestCase(new[] { 3d, 4d, 5d }, 4.0)]
    [TestCase(new[] { 2d, 4d }, 3.0)]
    [TestCase(new[] { 5d, 5d, 5d }, 5.0)]
    [TestCase(new[] { 1d, 2d, 3d, 4d, 5d }, 3.0)]
    public void AverageRating_WithRatings_CalculatesCorrectly(double[] ratings, double expectedAverage)
    {
        var tour = TestData.SampleTour();
        tour.TourLogs.Clear();
        foreach (var rating in ratings)
            tour.TourLogs.Add(new TourLog { Rating = rating });

        Assert.That(tour.AverageRating, Is.EqualTo(expectedAverage));
    }

    [Test]
    public void AverageRating_EmptyLogs_ReturnsNull()
    {
        var tour = TestData.SampleTour();
        tour.TourLogs.Clear();

        Assert.That(tour.AverageRating, Is.Null);
    }

    [Test]
    public void AverageRating_MixedWithNull_IgnoresNullValues()
    {
        var tour = TestData.SampleTour();
        tour.TourLogs.Clear();
        tour.TourLogs.Add(new TourLog { Rating = 2 });
        tour.TourLogs.Add(new TourLog { Rating = null });
        tour.TourLogs.Add(new TourLog { Rating = 4 });
        tour.TourLogs.Add(new TourLog { Rating = null });

        Assert.That(tour.AverageRating, Is.EqualTo(3d));
    }

    [Test]
    public void AverageRating_OnlyNullRatings_ReturnsNull()
    {
        var tour = TestData.SampleTour();
        tour.TourLogs.Clear();
        tour.TourLogs.Add(new TourLog { Rating = null });
        tour.TourLogs.Add(new TourLog { Rating = null });

        Assert.That(tour.AverageRating, Is.Null);
    }

    [TestCaseSource(nameof(ChildFriendlyCases))]
    public bool IsChildFriendly_WithVariousConditions_EvaluatesCorrectly(double[] difficulties, double?[] ratings)
    {
        var tour = TestData.SampleTour();
        tour.TourLogs.Clear();
        for (var i = 0; i < difficulties.Length; i++)
            tour.TourLogs.Add(new TourLog
            {
                Difficulty = difficulties[i],
                Rating = ratings[i]
            });

        return tour.IsChildFriendly;
    }

    [TestCase(true, "Yes")]
    [TestCase(false, "No")]
    public void ChildFriendlyText_ReflectsState(bool friendly, string expected)
    {
        var tour = TestData.SampleTour(tourLogs: friendly
            ? [TestData.SampleTourLog(rating: 5, difficulty: 1)]
            : []);
        Assert.That(tour.ChildFriendlyText, Is.EqualTo(expected));
    }

    [TestCase("A description", "A description")]
    [TestCase("", "N/A")]
    [TestCase(null, "N/A")]
    public void DescriptionDisplay_FormatsCorrectly(string? description, string expected)
    {
        var tour = TestData.SampleTour(description: description ?? "");
        if (description is null)
        {
            // Force null after construction since 'required' prevents null in constructor
            tour.Description = null!;
        }
        Assert.That(tour.DescriptionDisplay, Is.EqualTo(expected));
    }

    [TestCase("/images/test.png", "Available")]
    [TestCase("", "N/A")]
    [TestCase(null, "N/A")]
    public void ImageDisplay_FormatsCorrectly(string? imagePath, string expected)
    {
        var tour = TestData.SampleTour(imagePath: imagePath);
        Assert.That(tour.ImageDisplay, Is.EqualTo(expected));
    }

    [TestCase(100.5, "100.50 meters")]
    [TestCase(null, "N/A meters")]
    public void DistanceDisplay_FormatsCorrectly(double? distance, string expected)
    {
        var tour = TestData.SampleTour(distance: distance);
        Assert.That(tour.DistanceDisplay, Is.EqualTo(expected));
    }

    [TestCase(60.0, "60 minutes")]
    [TestCase(null, "N/A minutes")]
    public void EstimatedTimeDisplay_FormatsCorrectly(double? time, string expected)
    {
        var tour = TestData.SampleTour(estimatedTime: time);
        Assert.That(tour.EstimatedTimeDisplay, Is.EqualTo(expected));
    }

    [Test]
    public void AverageRatingDisplay_WithRating_FormatsValue()
    {
        var tour = TestData.SampleTour(tourLogs: [TestData.SampleTourLog(rating: 4.5)]);
        Assert.That(tour.AverageRatingDisplay, Is.EqualTo("4.5"));
    }

    [Test]
    public void AverageRatingDisplay_NoLogs_ShowsNA()
    {
        var tour = TestData.SampleTour();
        tour.TourLogs.Clear();
        Assert.That(tour.AverageRatingDisplay, Is.EqualTo("N/A"));
    }
}