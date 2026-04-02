using UI.View.TourLogComponents;
using UI.ViewModel;

namespace Tests.UI.View;

[TestFixture]
[Parallelizable(ParallelScope.All)]
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public sealed class TourLogFormComponentTests : BunitTestBase
{
    protected override void OnSetup() => Services.WithValidTourLogForm();

    private IRenderedComponent<TourLogFormComponent> Render() =>
        RenderComponent<TourLogFormComponent>(p =>
            p.Add(x => x.ViewModel, Services.ViewModel<TourLogViewModel>()));

    [Test]
    public void RendersAllFields()
    {
        var cut = Render();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(cut.Find("#comment"), Is.Not.Null);
            Assert.That(cut.Find("#difficulty"), Is.Not.Null);
            Assert.That(cut.Find("#totalDistance"), Is.Not.Null);
            Assert.That(cut.Find("#totalTime"), Is.Not.Null);
            Assert.That(cut.Find("#rating"), Is.Not.Null);
        }
    }

    [TestCase("Some comment")]
    [TestCase("")]
    public void BindsComment(string text)
    {
        Render().Find("#comment").Change(text);
        Assert.That(Services.ViewModel<TourLogViewModel>().SelectedTourLog.Comment, Is.EqualTo(text));
    }

    [Test]
    public void DisablesSaveWhenInvalid()
    {
        Services.WithEmptyTourLogForm();
        Assert.That(Render().Find("button[type='submit']").HasAttribute("disabled"), Is.True);
    }

    [Test]
    public async Task SavesWhenValid()
    {
        var vm = Services.ViewModel<TourLogViewModel>();
        vm.SelectedTourLog = TourLogTestData.SampleTourLog(id: Guid.Empty, tourId: TestConstants.TestGuid);
        vm.SelectedTourId = TestConstants.TestGuid;
        Services.SetupMockPostTourLog();
        Services.SetupMockGetTourLogs(TestConstants.TestGuid, 1);
        var cut = Render();
        Assert.That(vm.IsFormValid, Is.True, "Form should be valid before submit");
        await cut.Find("button[type='submit']").ClickAsync(new MouseEventArgs());
        Services.VerifyMockPostTourLog(Times.Once());
    }
}
