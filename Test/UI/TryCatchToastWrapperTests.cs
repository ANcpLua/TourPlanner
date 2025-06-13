using Moq;
using Serilog;
using UI.Decorator;
using UI.Service.Interface;

namespace Test.UI;

[TestFixture]
public class TryCatchToastWrapperTests
{
    private Mock<IToastServiceWrapper> _mockToastService;
    private Mock<ILogger> _mockLogger;
    private TryCatchToastWrapper _wrapper;

    [SetUp]
    public void SetUp()
    {
        _mockToastService = new Mock<IToastServiceWrapper>();
        _mockLogger = new Mock<ILogger>();
        _wrapper = new TryCatchToastWrapper(_mockToastService.Object, _mockLogger.Object);
    }

    [Test]
    public async Task ExecuteAsync_WithGeneric_WhenExceptionAndErrorHandler_CallsErrorHandler()
    {
        var exception = new InvalidOperationException("Test exception");
        var errorHandlerCalled = false;

        var result = await _wrapper.ExecuteAsync<string>(
            () => throw exception,
            "Test error",
            ErrorHandler
        );

        using (Assert.EnterMultipleScope())
        {
            Assert.That(errorHandlerCalled, Is.True);
            Assert.That(result, Is.Null);
        }

        return;

        void ErrorHandler(Exception _) => errorHandlerCalled = true;
    }

    [Test]
    public async Task ExecuteAsync_NonGeneric_WhenExceptionAndErrorHandler_CallsErrorHandler()
    {
        var exception = new InvalidOperationException("Test exception");
        var errorHandlerCalled = false;

        await _wrapper.ExecuteAsync(
            () => throw exception,
            "Test error",
            ErrorHandler
        );

        Assert.That(errorHandlerCalled, Is.True);
        return;

        void ErrorHandler(Exception _) => errorHandlerCalled = true;
    }
}
