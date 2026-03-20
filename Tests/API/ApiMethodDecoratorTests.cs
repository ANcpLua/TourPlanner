using System.Reflection;
using API.AOP;

namespace Tests.API;

[TestFixture]
public class ApiMethodDecoratorTests
{
    [SetUp]
    public void SetUp()
    {
        _decorator = new ApiMethodDecorator();
        _mockLogger = new Mock<ILogger>();
        Log.Logger = _mockLogger.Object;
    }

    private ApiMethodDecorator _decorator = null!;
    private Mock<ILogger> _mockLogger = null!;

    [Test]
    public void Init_WithMethodHavingDeclaringType_LogsFullTypeName()
    {
        var testObject = new TestApiClass();
        var method = typeof(TestApiClass).GetMethod(nameof(TestApiClass.TestMethod))!;
        var args = new object[] { "arg1", 42 };

        _decorator.Init(testObject, method, args);

        _mockLogger.Verify(
            l => l.Information(
                "Entering {MethodName} ({ArgCount} args)",
                $"{typeof(TestApiClass).FullName}.TestMethod",
                args.Length),
            Times.Once);
    }

    [Test]
    public void Init_WithMethodHavingNullDeclaringType_HandlesNullGracefully()
    {
        var testObject = new TestApiClass();
        var mockMethod = new Mock<MethodInfo>();
        mockMethod.Setup(static m => m.Name).Returns("MockMethod");
        mockMethod.Setup(static m => m.DeclaringType).Returns((Type?)null);
        var args = new object[] { "test" };

        _decorator.Init(testObject, mockMethod.Object, args);

        _mockLogger.Verify(
            l => l.Information(
                "Entering {MethodName} ({ArgCount} args)",
                ".MockMethod",
                args.Length),
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
            static l => l.Information(
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
                "Exception in {MethodName} ({ArgCount} args) after {Duration}ms",
                $"{typeof(TestApiClass).FullName}.TestMethod",
                args.Length,
                It.IsAny<long>()),
            Times.Once);
    }

    [Test]
    public void OnEntry_ExecutesWithoutException()
    {
        var method = typeof(TestApiClass).GetMethod(nameof(TestApiClass.TestMethod))!;
        _decorator.Init(new object(), method, []);

        _decorator.OnEntry();
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
        using (Assert.EnterMultipleScope())
        {
            _mockLogger.Verify(
                static l => l.Information(
                    "Entering {MethodName} ({ArgCount} args)",
                    It.IsAny<string>(),
                    It.IsAny<int>()),
                Times.Once);

            _mockLogger.Verify(
                static l => l.Information(
                    "Exiting {MethodName} after {Duration}ms",
                    It.IsAny<string>(),
                    It.IsAny<long>()),
                Times.Once);

            _mockLogger.Verify(
                static l => l.Error(
                    It.IsAny<Exception>(),
                    "Exception in {MethodName} ({ArgCount} args) after {Duration}ms",
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<long>()),
                Times.Once);
        }
    }

    private sealed class TestApiClass
    {
        public static void TestMethod()
        {
        }
    }
}