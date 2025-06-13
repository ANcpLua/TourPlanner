using UI.Service;

namespace Test.UI.Services;

[TestFixture]
public class ViewModelHelperServiceTests
{
    [SetUp]
    public void Setup()
    {
        _service = new ViewModelHelperService();
    }

    private ViewModelHelperService _service = null!;

    [TestCase(false, true)]
    [TestCase(true, false)]
    public void ToggleVisibility_TogglesValueCorrectly(bool initialValue, bool expectedResult)
    {
        var flag = initialValue;

        var result = _service.ToggleVisibility(ref flag);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(expectedResult));
            Assert.That(flag, Is.EqualTo(expectedResult));
        });
    }

    [Test]
    public void ToggleVisibility_MultipleCalls_AlternatesValues()
    {
        var flag = false;

        var result1 = _service.ToggleVisibility(ref flag);
        var result2 = _service.ToggleVisibility(ref flag);
        var result3 = _service.ToggleVisibility(ref flag);

        Assert.Multiple(() =>
        {
            Assert.That(result1, Is.True);
            Assert.That(result2, Is.False);
            Assert.That(result3, Is.True);
            Assert.That(flag, Is.True);
        });
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ShowForm_SetsFormVisibilityToTrue(bool initialValue)
    {
        var flag = initialValue;

        _service.ShowForm(ref flag);

        Assert.That(flag, Is.True);
    }

    [TestCase("Original Tour", "Reset Tour")]
    [TestCase("Test Tour", "Updated Tour")]
    public void ResetForm_WithTour_ResetsToNewInstance(string originalName, string resetName)
    {
        var originalTour = TestData.SampleTour(originalName);
        var tourRef = originalTour;

        _service.ResetForm(ref tourRef, () => TestData.SampleTour(resetName));

        Assert.Multiple(() =>
        {
            Assert.That(tourRef, Is.Not.SameAs(originalTour));
            Assert.That(tourRef.Name, Is.EqualTo(resetName));
            Assert.That(tourRef.Id, Is.EqualTo(TestData.TestGuid));
        });
    }

    [TestCase("Original Domain", "Reset Domain")]
    [TestCase("Test Domain", "Updated Domain")]
    public void ResetForm_WithTourDomain_ResetsCorrectly(string originalName, string resetName)
    {
        var originalDomain = TestData.SampleTourDomain(originalName);
        var domainRef = originalDomain;

        _service.ResetForm(ref domainRef, () => TestData.SampleTourDomain(resetName));

        Assert.Multiple(() =>
        {
            Assert.That(domainRef, Is.Not.SameAs(originalDomain));
            Assert.That(domainRef.Name, Is.EqualTo(resetName));
            Assert.That(domainRef.Id, Is.EqualTo(TestData.TestGuid));
        });
    }

    [Test]
    public void ResetForm_WithString_ResetsToNewValue()
    {
        var stringRef = TestData.ValidSearchText;

        _service.ResetForm(ref stringRef, () => TestData.InvalidSearchText);

        Assert.That(stringRef, Is.EqualTo(TestData.InvalidSearchText));
    }

    [TestCase(1, 1, 5, 5)]
    [TestCase(2, 3, 4, 4)]
    [TestCase(3, 2, 1, 3)]
    public void ResetForm_WithTourLog_ResetsAllProperties(int originalRating, int originalDifficulty, int newRating,
        int newDifficulty)
    {
        var originalLog = TestData.SampleTourLogDto(originalRating, originalDifficulty);
        originalLog.Comment = "Original Comment";

        _service.ResetForm(ref originalLog, () => TestData.SampleTourLogDto(newRating, newDifficulty));

        Assert.Multiple(() =>
        {
            Assert.That(originalLog.Rating, Is.EqualTo(newRating));
            Assert.That(originalLog.Difficulty, Is.EqualTo(newDifficulty));
            Assert.That(originalLog.Comment, Is.EqualTo("Sample tour log comment"));
            Assert.That(originalLog.TourId, Is.EqualTo(TestData.TestGuid));
        });
    }

    [Test]
    public void ResetForm_FactoryReturnsNull_SetsToNull()
    {
        var tourRef = TestData.SampleTour();

        _service.ResetForm(ref tourRef, () => null!);

        Assert.That(tourRef, Is.Null);
    }

    [Test]
    public void ResetForm_FactoryExecutedEachTime_CreatesNewInstances()
    {
        var tourRef1 = TestData.SampleTour();
        var tourRef2 = TestData.SampleTour();

        _service.ResetForm(ref tourRef1, () => TestData.SampleTour("First Tour"));
        _service.ResetForm(ref tourRef2, () => TestData.SampleTour("Second Tour"));

        Assert.Multiple(() =>
        {
            Assert.That(tourRef1, Is.Not.SameAs(tourRef2));
            Assert.That(tourRef1.Name, Is.EqualTo("First Tour"));
            Assert.That(tourRef2.Name, Is.EqualTo("Second Tour"));
        });
    }

    [Test]
    public void ResetForm_WithComplexFactory_ExecutesLambdaCorrectly()
    {
        var tourRef = TestData.SampleTour();

        _service.ResetForm(ref tourRef, () =>
        {
            var newTour = TestData.SampleTour("Lambda Created");
            newTour.Distance = 999.9;
            return newTour;
        });

        Assert.Multiple(() =>
        {
            Assert.That(tourRef.Name, Is.EqualTo("Lambda Created"));
            Assert.That(tourRef.Distance, Is.EqualTo(999.9));
        });
    }
}