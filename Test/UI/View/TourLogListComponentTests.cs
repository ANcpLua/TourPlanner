using UI.Model;
using UI.Service.Interface;
using UI.View.TourLogComponents;
using UI.ViewModel;

namespace Test.UI.View;

[TestFixture]
[Parallelizable(ParallelScope.All)]
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public sealed class TourLogListComponentTests : BunitTestBase
{
    [Test]
    public void RendersAllTourLogs()
    {
        Services.ViewModel<TourLogViewModel>().TourLogs =
            new ObservableCollection<TourLog>(TestData.SampleTourLogList());

        var cut = RenderComponent<TourLogListComponent>(p =>
            p.Add(x => x.ViewModel, Services.ViewModel<TourLogViewModel>()));

        Assert.That(cut.FindAll(".tour-card"), Has.Count.EqualTo(2));
    }

    [Test]
    public void ShowsEmptyMessageWhenNoLogs()
    {
        var cut = RenderComponent<TourLogListComponent>(p =>
            p.Add(x => x.ViewModel, Services.ViewModel<TourLogViewModel>()));

        Assert.That(cut.Find(".text-center").TextContent, Is.EqualTo("No logs available for this tour."));
    }

    [Test]
    public async Task EditButton_LoadsLogForEditing()
    {
        var log = TestData.SampleTourLog();
        Services.ViewModel<TourLogViewModel>().TourLogs = [log];
        Services.Mock<IHttpService>().Setup(s => s.GetAsync<TourLog>($"api/tourlog/{log.Id}"))
            .ReturnsAsync(log);

        var cut = RenderComponent<TourLogListComponent>(p =>
            p.Add(x => x.ViewModel, Services.ViewModel<TourLogViewModel>()));
        await cut.Find(".btn-success").ClickAsync(new MouseEventArgs());
        using (Assert.EnterMultipleScope())
        {
            Assert.That(Services.ViewModel<TourLogViewModel>().IsLogFormVisible, Is.True);
            Assert.That(Services.ViewModel<TourLogViewModel>().IsEditing, Is.True);
        }
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task DeleteButton_RespectsUserConfirmation(bool userConfirms)
    {
        var log = TestData.SampleTourLog();
        Services.ViewModel<TourLogViewModel>().TourLogs = [log];

        JSInterop.Setup<bool>("confirm", _ => true).SetResult(userConfirms);

        if (userConfirms)
        {
            Services.Mock<IHttpService>().Setup(s => s.DeleteAsync($"api/tourlog/{log.Id}"))
                .Returns(Task.CompletedTask);
            Services.Mock<IHttpService>().Setup(s => s.GetListAsync<TourLog>($"api/tourlog/bytour/{log.TourId}"))
                .ReturnsAsync(new List<TourLog>());
        }

        var cut = RenderComponent<TourLogListComponent>(p =>
            p.Add(x => x.ViewModel, Services.ViewModel<TourLogViewModel>()));
        await cut.Find(".btn-danger").ClickAsync(new MouseEventArgs());

        Services.Mock<IHttpService>().Verify(
            s => s.DeleteAsync($"api/tourlog/{log.Id}"),
            userConfirms ? Times.Once : Times.Never);
    }
}