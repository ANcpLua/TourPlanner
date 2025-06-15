using UI.Model;
using UI.Service.Interface;
using UI.View.TourLogComponents;
using UI.ViewModel;

namespace Test.UI.View;

[TestFixture]
[Parallelizable(ParallelScope.All)]
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public sealed class TourLogFormComponentTests : BunitTestBase
{
    protected override void OnSetup()
    {
        var log = TestData.SampleTourLog();
        Services.ViewModel<TourLogViewModel>().SelectedTourLog = log;
        Services.ViewModel<TourLogViewModel>().SelectedTourId = log.TourId;
    }

    [Test]
    public void RendersAllFields()
    {
        var cut = RenderComponent<TourLogFormComponent>(p =>
            p.Add(x => x.ViewModel, Services.ViewModel<TourLogViewModel>()));
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
        var cut = RenderComponent<TourLogFormComponent>(p =>
            p.Add(x => x.ViewModel, Services.ViewModel<TourLogViewModel>()));
        cut.Find("#comment").Change(text);
        Assert.That(Services.ViewModel<TourLogViewModel>().SelectedTourLog.Comment, Is.EqualTo(text));
    }

    [Test]
    public void DisablesSaveWhenInvalid()
    {
        Services.ViewModel<TourLogViewModel>().SelectedTourLog = new TourLog();
        var cut = RenderComponent<TourLogFormComponent>(p =>
            p.Add(x => x.ViewModel, Services.ViewModel<TourLogViewModel>()));

        Assert.That(cut.Find("button[type='submit']").HasAttribute("disabled"), Is.True);
    }

    [Test]
    public async Task SavesWhenValid()
    {
        var vm = Services.ViewModel<TourLogViewModel>();
        vm.SelectedTourId = TestData.TestGuid;
        var newLog = TestData.SampleTourLog(tourId: TestData.TestGuid);
        newLog.Id = Guid.Empty;
        vm.SelectedTourLog = newLog;

        Services.Mock<IHttpService>()
            .Setup(s => s.PostAsync<TourLog>("api/tourlog", It.IsAny<TourLog>()))
            .ReturnsAsync(TestData.SampleTourLog());
        Services.Mock<IHttpService>()
            .Setup(s => s.GetListAsync<TourLog>($"api/tourlog/bytour/{TestData.TestGuid}"))
            .ReturnsAsync(TestData.SampleTourLogList(1, TestData.TestGuid));

        var cut = RenderComponent<TourLogFormComponent>(p => p.Add(x => x.ViewModel, vm));
        await cut.Find("button[type='submit']").ClickAsync(new MouseEventArgs());

        Services.Mock<IHttpService>().Verify(
            s => s.PostAsync<TourLog>("api/tourlog", It.IsAny<TourLog>()),
            Times.Once
        );
    }
}