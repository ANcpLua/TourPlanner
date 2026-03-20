using UI.Decorator;
using UI.Service.Interface;

namespace Tests.UI;

[TestFixture]
public class TryCatchToastWrapperTests
{
    [SetUp]
    public void SetUp()
    {
        _mockToastService = new Mock<IToastServiceWrapper>();
        _mockLogger = new Mock<ILogger>();
        _wrapper = new TryCatchToastWrapper(_mockToastService.Object, _mockLogger.Object);
    }

    private Mock<IToastServiceWrapper> _mockToastService = null!;
    private Mock<ILogger> _mockLogger = null!;
    private TryCatchToastWrapper _wrapper = null!;

    [Test]
    public async Task ExecuteAsync_Generic_Success_ReturnsResult()
    {
        var result = await _wrapper.ExecuteAsync(
            static () => Task.FromResult("hello"),
            "Test error"
        );

        Assert.That(result, Is.EqualTo("hello"));
    }

    [Test]
    public async Task ExecuteAsync_Generic_Exception_LogsAndShowsToast()
    {
        var result = await _wrapper.ExecuteAsync<string>(
            static () => throw new InvalidOperationException("boom"),
            "Test error"
        );

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.Null);
            _mockLogger.Verify(
                static l => l.Error(
                    It.IsAny<Exception>(),
                    "Operation failed: {ErrorContext}",
                    "Test error"),
                Times.Once);
            _mockToastService.Verify(
                static t => t.ShowError(It.Is<string>(static s => s.Contains("Test error"))),
                Times.Once);
        }
    }

    [Test]
    public async Task ExecuteAsync_NonGeneric_Success_Completes()
    {
        var executed = false;

        await _wrapper.ExecuteAsync(
            () => { executed = true; return Task.CompletedTask; },
            "Test error"
        );

        Assert.That(executed, Is.True);
    }

    [Test]
    public async Task ExecuteAsync_NonGeneric_Exception_LogsAndShowsToast()
    {
        await _wrapper.ExecuteAsync(
            static () => throw new InvalidOperationException("boom"),
            "Test error"
        );

        using (Assert.EnterMultipleScope())
        {
            _mockLogger.Verify(
                static l => l.Error(
                    It.IsAny<Exception>(),
                    "Operation failed: {ErrorContext}",
                    "Test error"),
                Times.Once);
            _mockToastService.Verify(
                static t => t.ShowError(It.Is<string>(static s => s.Contains("Test error"))),
                Times.Once);
        }
    }
}
