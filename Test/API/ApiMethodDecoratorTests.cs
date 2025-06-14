using System.Reflection;
using API.AOP;
using Moq;
using Serilog;

namespace Test.API;

[TestFixture]
public class ApiMethodDecoratorTests
{
    private ApiMethodDecorator _decorator;
    private Mock<ILogger> _mockLogger;
    private MethodInfo _testMethod;

    [SetUp]
    public void SetUp()
    {
        _decorator = new ApiMethodDecorator();
        _mockLogger = new Mock<ILogger>();
        Log.Logger = _mockLogger.Object;
        _testMethod = GetType().GetMethod(nameof(SetUp))!;
    }

    [TearDown]
    public void TearDown()
    {
        Log.CloseAndFlush();
    }

    [Test]
    public void Init_WithMethodHavingDeclaringType_LogsFullTypeName()
    {
        var testObject = new TestApiClass();
        var method = typeof(TestApiClass).GetMethod(nameof(TestApiClass.TestMethod))!;
        var args = new object[] { "arg1", 42 };

        _decorator.Init(testObject, method, args);

        _mockLogger.Verify(
            l => l.Information(
                "Entering {MethodName} with arguments: {@Arguments}",
                $"{typeof(TestApiClass).FullName}.TestMethod",
                args),
            Times.Once);
    }

    [Test]
    public void Init_WithMethodHavingNullDeclaringType_HandlesNullGracefully()
    {
        var testObject = new TestApiClass();
        var mockMethod = new Mock<MethodInfo>();
        mockMethod.Setup(m => m.Name).Returns("MockMethod");
        mockMethod.Setup(m => m.DeclaringType).Returns((Type?)null);
        var args = new object[] { "test" };

        _decorator.Init(testObject, mockMethod.Object, args);

        _mockLogger.Verify(
            l => l.Information(
                "Entering {MethodName} with arguments: {@Arguments}",
                ".MockMethod",
                args),
            Times.Once);
    }

    [Test]
    public void OnExit_LogsMethodExitWithDuration()
    {
        var testObject = new TestApiClass();
        var method = typeof(TestApiClass).GetMethod(nameof(TestApiClass.TestMethod))!;
        _decorator.Init(testObject, method, []);

        _decorator.OnExit();

        _mockLogger.Verify(
            l => l.Information(
                "Exiting {MethodName} after {Duration}ms",
                It.IsAny<string>(),
                It.IsAny<long>()),
            Times.Once);
    }

    [Test]
    public void OnException_LogsExceptionWithDetails()
    {
        var testObject = new TestApiClass();
        var method = typeof(TestApiClass).GetMethod(nameof(TestApiClass.TestMethod))!;
        var args = new object[] { "arg1", 42 };
        var exception = new InvalidOperationException("Test exception");
        
        _decorator.Init(testObject, method, args);

        _decorator.OnException(exception);

        _mockLogger.Verify(
            l => l.Error(
                exception,
                "Exception in {MethodName} with arguments: {@Arguments} after {Duration}ms",
                $"{typeof(TestApiClass).FullName}.TestMethod",
                args,
                It.IsAny<long>()),
            Times.Once);
    }

    [Test]
    public void OnEntry_DoesNotThrow()
    {
        _decorator.Init(new object(), _testMethod, []);

        Assert.DoesNotThrow(() => _decorator.OnEntry());
    }

    [Test]
    public void CompleteLifecycle_ExecutesAllMethodsSuccessfully()
    {
        var testObject = new TestApiClass();
        var method = typeof(TestApiClass).GetMethod(nameof(TestApiClass.TestMethod))!;
        var args = new object[] { "test", 123 };
        var exception = new ArgumentException("Test exception");

        _decorator.Init(testObject, method, args);
        _decorator.OnEntry();
        _decorator.OnExit();
        _decorator.OnException(exception);

        _mockLogger.Verify(
            l => l.Information(
                "Entering {MethodName} with arguments: {@Arguments}",
                It.IsAny<string>(),
                It.IsAny<object[]>()),
            Times.Once);
        _mockLogger.Verify(
            l => l.Information(
                "Exiting {MethodName} after {Duration}ms",
                It.IsAny<string>(),
                It.IsAny<long>()),
            Times.Once);
        _mockLogger.Verify(
            l => l.Error(
                It.IsAny<Exception>(),
                "Exception in {MethodName} with arguments: {@Arguments} after {Duration}ms",
                It.IsAny<string>(),
                It.IsAny<object[]>(),
                It.IsAny<long>()),
            Times.Once);
    }

    private class TestApiClass
    {
        public static void TestMethod()
        {
        }
    }
}