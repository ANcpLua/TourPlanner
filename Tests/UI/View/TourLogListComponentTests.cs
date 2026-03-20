using UI.Service.Interface;
using UI.View.TourLogComponents;
using UI.ViewModel;

namespace Tests.UI.View;

[TestFixture]
[Parallelizable(ParallelScope.All)]
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public sealed class TourLogListComponentTests : BunitTestBase
{
    private IRenderedComponent<TourLogListComponent> Render() =>
        RenderComponent<TourLogListComponent>(p =>
            p.Add(static x => x.ViewModel, Services.ViewModel<TourLogViewModel>()));

    [Test]
    public void RendersAllTourLogs()
    {
        Services.WithTourLogs();
        Assert.That(Render().FindAll(".tour-card"), Has.Count.EqualTo(2));
    }

    [Test]
    public void ShowsEmptyMessageWhenNoLogs() =>
        Assert.That(Render().Find(".text-center").TextContent, Is.EqualTo("No logs available for this tour."));

    [Test]
    public async Task EditButton_LoadsLogForEditing()
    {
        Services.WithSingleTourLog();
        var logId = Services.FirstTourLogId();
        Services.SetupMockGetTourLog(logId);
        await Render().Find(".btn-success").ClickAsync(new MouseEventArgs());
        using (Assert.EnterMultipleScope())
        {
            Assert.That(Services.ViewModel<TourLogViewModel>().IsLogFormVisible, Is.True);
            Assert.That(Services.ViewModel<TourLogViewModel>().IsEditing, Is.True);
        }
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task DeleteButton_RespectsUserConfirmation(bool confirms)
    {
        Services.WithSingleTourLog();
        var logId = Services.FirstTourLogId();
        JSInterop.Setup<bool>("confirm", static _ => true).SetResult(confirms);
        if (confirms)
        {
            Services.SetupMockDeleteTourLog(logId);
            Services.SetupMockGetTourLogs(Services.ViewModel<TourLogViewModel>().TourLogs.First().TourId, 0);
        }

        await Render().Find(".btn-danger").ClickAsync(new MouseEventArgs());
        Services.VerifyMockDeleteTourLog(logId, confirms ? Times.Once() : Times.Never());
    }
}
