using System.Reflection;
using UI.Decorator;
using UI.Service.Interface;
using UI.ViewModel.Base;

namespace Test.UI;

[TestFixture]
public class UiMethodDecoratorTests
{
    private UiMethodDecorator _decorator = null!;
    private MethodInfo _testMethod = null!;
    private Mock<IToastServiceWrapper> _mockToastService = null!;
    private Mock<ILogger> _mockLogger = null!;

    [SetUp]
    public void SetUp()
    {
        _decorator = new UiMethodDecorator();
        _testMethod = GetType().GetMethod(nameof(SetUp))!;
        _mockToastService = new Mock<IToastServiceWrapper>();
        _mockLogger = new Mock<ILogger>();
        Log.Logger = _mockLogger.Object;
    }

    [Test]
    public void Init_WithBaseViewModelHavingToastService_SetsToastService()
    {
        var mockViewModel = new Mock<BaseViewModel>(
            Mock.Of<IHttpService>(),
            _mockToastService.Object,
            Mock.Of<ILogger>()
        ) { CallBase = true };

        _decorator.Init(mockViewModel.Object, _testMethod, ["arg1", 42]);
        _decorator.OnException(new InvalidOperationException("Test exception"));

        _mockToastService.Verify(
            ts => ts.ShowError(It.Is<string>(msg =>
                msg.Contains("An error occurred in") &&
                msg.Contains("Test exception"))),
            Times.Once);
    }

    [Test]
    public void Init_WithBaseViewModelHavingNullToastService_HandlesGracefully()
    {
        IToastServiceWrapper? nullToastService = null;
        var mockViewModel = new Mock<BaseViewModel>(
            Mock.Of<IHttpService>(),
            nullToastService!,
            Mock.Of<ILogger>()
        ) { CallBase = true };

        _decorator.Init(mockViewModel.Object, _testMethod, []);
        _decorator.OnException(new Exception("test"));
    }

    [Test]
    public void Init_WithNonBaseViewModelInstance_DoesNotSetToastService()
    {
        var regularObject = new object();

        _decorator.Init(regularObject, _testMethod, []);
        _decorator.OnException(new Exception("test"));
    }

    [Test]
    public void OnEntry_ExecutesWithoutException()
    {
        _decorator.Init(new object(), _testMethod, []);
        _decorator.OnEntry();
    }

    [Test]
    public void OnExit_ExecutesWithoutException()
    {
        _decorator.Init(new object(), _testMethod, []);
        _decorator.OnExit();
    }

    [Test]
    public void OnException_WithoutToastService_HandlesGracefully()
    {
        var regularObject = new object();
        _decorator.Init(regularObject, _testMethod, []);

        _decorator.OnException(new InvalidOperationException("Test exception"));
    }

    [Test]
    public void CompleteLifecycle_WithBaseViewModel_ExecutesAllMethods()
    {
        var mockViewModel = new Mock<BaseViewModel>(
            Mock.Of<IHttpService>(),
            _mockToastService.Object,
            Mock.Of<ILogger>()
        ) { CallBase = true };

        _decorator.Init(mockViewModel.Object, _testMethod, ["test", 123]);
        _decorator.OnEntry();
        _decorator.OnExit();
        _decorator.OnException(new ArgumentException("Test exception"));

        _mockToastService.Verify(
            ts => ts.ShowError(It.IsAny<string>()),
            Times.Once);
    }

    [Test]
    public void CompleteLifecycle_WithRegularObject_ExecutesAllMethods()
    {
        var regularObject = new { Name = "Test" };

        _decorator.Init(regularObject, _testMethod, ["test", 123]);
        _decorator.OnEntry();
        _decorator.OnExit();
        _decorator.OnException(new ArgumentException("Test exception"));
    }

    [Test]
    public void Init_WithMethodHavingDeclaringType_SetsMethodNameWithFullTypeName()
    {
        var mockViewModel = new Mock<BaseViewModel>(
            Mock.Of<IHttpService>(),
            _mockToastService.Object,
            Mock.Of<ILogger>()
        ) { CallBase = true };

        var mockMethod = new Mock<MethodInfo>();
        mockMethod.Setup(m => m.Name).Returns("TestMethod");
        mockMethod.Setup(m => m.DeclaringType).Returns(typeof(UiMethodDecoratorTests));

        _decorator.Init(mockViewModel.Object, mockMethod.Object, []);
        _decorator.OnException(new Exception("test"));

        _mockToastService.Verify(
            ts => ts.ShowError(It.Is<string>(msg =>
                msg.Contains("UiMethodDecoratorTests.TestMethod") &&
                !msg.Contains("null"))),
            Times.Once);
    }

    [Test]
    public void Init_WithMethodHavingNullDeclaringType_HandlesNullGracefully()
    {
        var mockViewModel = new Mock<BaseViewModel>(
            Mock.Of<IHttpService>(),
            _mockToastService.Object,
            Mock.Of<ILogger>()
        ) { CallBase = true };

        var mockMethod = new Mock<MethodInfo>();
        mockMethod.Setup(m => m.Name).Returns("TestMethod");
        mockMethod.Setup(m => m.DeclaringType).Returns((Type?)null);

        _decorator.Init(mockViewModel.Object, mockMethod.Object, []);
        _decorator.OnException(new Exception("test"));

        _mockToastService.Verify(
            ts => ts.ShowError(It.Is<string>(msg =>
                msg.Contains(".TestMethod") &&
                !msg.Contains("null"))),
            Times.Once);
    }

    [Test]
    public void OnException_LogsAndShowsError()
    {
        var method = typeof(BaseViewModel).GetMethod(nameof(BaseViewModel.OnPropertyChanged))!;
        var args = new object[] { "TestProperty" };
        var exception = new InvalidOperationException("Test exception message");

        var mockViewModel = new Mock<BaseViewModel>(
            Mock.Of<IHttpService>(),
            _mockToastService.Object,
            Mock.Of<ILogger>()
        ) { CallBase = true };

        _decorator.Init(mockViewModel.Object, method, args);
        _decorator.OnException(exception);
        using (Assert.EnterMultipleScope())
        {
            _mockLogger.Verify(
                l => l.Error(
                    exception,
                    "Exception in {MethodName} with arguments: {@Arguments} after {Duration}ms",
                    It.Is<string>(s => s.Contains("OnPropertyChanged")),
                    args,
                    It.IsAny<long>()
                ),
                Times.Once
            );

            _mockToastService.Verify(
                t => t.ShowError(It.Is<string>(s => s.Contains("Test exception message"))),
                Times.Once
            );
        }
    }
}