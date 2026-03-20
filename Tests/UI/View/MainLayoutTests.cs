using UI.View.Layout;

namespace Tests.UI.View;

[TestFixture]
[Parallelizable(ParallelScope.All)]
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public sealed class MainLayoutTests : BunitTestBase
{
    protected override void OnSetup()
    {
        Context.ComponentFactories.AddStub<SearchComponent>();
    }

    [Test]
    public void NavigationStructure_IsCorrect()
    {
        var cut = RenderComponent<MainLayout>();
        var links = cut.FindAll("a.nav-link");

        Assert.That(links.Select(static l => l.GetAttribute("href")),
            Is.EqualTo(new[] { "/", "/log/list", "/reports" }));
    }

    [Test]
    public void RendersCorrectStructure()
    {
        var cut = RenderComponent<MainLayout>();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(cut.Find("nav.navbar"), Is.Not.Null);
            Assert.That(cut.Find("main.container-fluid"), Is.Not.Null);
            Assert.That(cut.FindComponent<Stub<SearchComponent>>(), Is.Not.Null);
        }
    }
}