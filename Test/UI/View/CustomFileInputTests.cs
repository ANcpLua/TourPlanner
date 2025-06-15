namespace Test.UI.View;

[TestFixture]
[Parallelizable(ParallelScope.All)]
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public sealed class CustomFileInputTests : BunitTestBase
{
    [Test]
    public void RendersWithDefaultId()
    {
        var cut = RenderComponent<CustomFileInput>();
        var input = cut.Find("input[type='file']");
        var label = cut.Find("label");
        using (Assert.EnterMultipleScope())
        {
            var inputId = input.GetAttribute("id");
            Assert.That(inputId, Is.Not.Null);
            Assert.That(inputId, Is.Not.Empty);
            Assert.That(label.GetAttribute("for"), Is.EqualTo(inputId));
            Assert.That(input.GetAttribute("accept"), Is.EqualTo(".json"));
        }
    }

    [Test]
    public void UsesProvidedId()
    {
        const string customId = "custom-file-input-123";
        var cut = RenderComponent<CustomFileInput>(p => p.Add(x => x.Id, customId));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(cut.Find("input[type='file']").GetAttribute("id"), Is.EqualTo(customId));
            Assert.That(cut.Find("label").GetAttribute("for"), Is.EqualTo(customId));
        }
    }

    [Test]
    public async Task OnFileSelected_InvokesCallback()
    {
        var called = false;
        var cut = RenderComponent<CustomFileInput>(p => p
            .Add(x => x.OnChange, EventCallback.Factory.Create<InputFileChangeEventArgs>(this, _ => called = true)));

        var inputFile = cut.FindComponent<InputFile>();
        await cut.InvokeAsync(() => inputFile.Instance.OnChange.InvokeAsync(
            new InputFileChangeEventArgs([TestData.MockBrowserFile("test.json").Object])));

        Assert.That(called, Is.True);
    }
}