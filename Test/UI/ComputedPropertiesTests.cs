using UI.Model;

namespace Test.UI;

[TestFixture]
public class TourComputedPropertiesTests
{
    private static readonly TestCaseData[] AverageRatingCases =
    [
        new TestCaseData(Array.Empty<int>()).Returns(0.0).SetName("Empty"),
        new TestCaseData(new[] { 3, 4, 5 }).Returns(4.0).SetName("AllValid"),
        new TestCaseData(new[] { 2, -1, 4, -1 }).Returns(3.0).SetName("MixedWithNull"),
        new TestCaseData(new[] { -1, -1 }).Returns(0.0).SetName("OnlyNull")
    ];

    private static readonly TestCaseData[] ChildFriendlyCases =
    [
        new TestCaseData(new[] { 1, 2, 1 }, new[] { 3, 4, 5 }).Returns(true).SetName("AllGood"),
        new TestCaseData(new[] { 1, 3, 1 }, new[] { 4, 5, 4 }).Returns(false).SetName("HighDifficulty"),
        new TestCaseData(new[] { 1, 2, 1 }, new[] { 2, 4, 5 }).Returns(false).SetName("LowRating"),
        new TestCaseData(new[] { 1 }, new[] { -1 }).Returns(false).SetName("NullRating"),
        new TestCaseData(Array.Empty<int>(), Array.Empty<int>()).Returns(false).SetName("NoLogs")
    ];

    [TestCase(0, ExpectedResult = "Not popular")]
    [TestCase(1, ExpectedResult = "Less popular")]
    [TestCase(2, ExpectedResult = "Moderately popular")]
    [TestCase(3, ExpectedResult = "Popular")]
    [TestCase(4, ExpectedResult = "Very popular")]
    [TestCase(10, ExpectedResult = "Very popular")]
    public string Popularity_ReturnsExpectedValue(int logCount)
    {
        var tour = TestData.SampleTour();
        for (var i = 0; i < logCount; i++)
            tour.TourLogs.Add(new TourLog());

        return tour.Popularity;
    }

    [TestCaseSource(nameof(AverageRatingCases))]
    public double AverageRating_CalculatesCorrectly(int[] ratings)
    {
        var tour = TestData.SampleTour();
        foreach (var rating in ratings)
            tour.TourLogs.Add(new TourLog { Rating = rating == -1 ? null : rating });

        return tour.AverageRating;
    }

    [TestCaseSource(nameof(ChildFriendlyCases))]
    public bool IsChildFriendly_EvaluatesCorrectly(int[] difficulties, int[] ratings)
    {
        var tour = TestData.SampleTour();
        for (var i = 0; i < difficulties.Length; i++)
            tour.TourLogs.Add(new TourLog
            {
                Difficulty = difficulties[i],
                Rating = ratings[i] == -1 ? null : ratings[i]
            });

        return tour.IsChildFriendly;
    }
}