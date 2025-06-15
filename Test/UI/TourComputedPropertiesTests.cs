using UI.Model;

namespace Test.UI;

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
    public void AverageRating_EmptyLogs_ReturnsZero()
    {
        var tour = TestData.SampleTour();
        tour.TourLogs.Clear();

        Assert.That(tour.AverageRating, Is.Zero);
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
    public void AverageRating_OnlyNullRatings_ReturnsZero()
    {
        var tour = TestData.SampleTour();
        tour.TourLogs.Clear();
        tour.TourLogs.Add(new TourLog { Rating = null });
        tour.TourLogs.Add(new TourLog { Rating = null });

        Assert.That(tour.AverageRating, Is.Zero);
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
}