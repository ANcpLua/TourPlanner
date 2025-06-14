using System.Reflection;
using Moq;
using Serilog;
using UI.Decorator;
using UI.Service.Interface;
using UI.ViewModel.Base;

namespace Test.UI;

public class TestViewModelWithToast : BaseViewModel
{
    public TestViewModelWithToast(IToastServiceWrapper? toastService)
        : base(Mock.Of<IHttpService>(),
            toastService ?? Mock.Of<IToastServiceWrapper>(), Log.Logger)
    {
    }
}

[TestFixture]
public class UiMethodDecoratorTests
{
    private UiMethodDecorator _decorator;
    private MethodInfo _testMethod;
    private Mock<IToastServiceWrapper> _mockToastService;

    [SetUp]
    public void SetUp()
    {
        _decorator = new UiMethodDecorator();
        _testMethod = GetType().GetMethod(nameof(SetUp))!;
        _mockToastService = new Mock<IToastServiceWrapper>();
    }

    [Test]
    public void Init_WithBaseViewModelHavingToastService_SetsToastService()
    {
        var viewModel = new TestViewModelWithToast(_mockToastService.Object);
        
        _decorator.Init(viewModel, _testMethod, ["arg1", 42]);
        
        var exception = new InvalidOperationException("Test exception");
        _decorator.OnException(exception);
        
        _mockToastService.Verify(
            ts => ts.ShowError(It.Is<string>(msg => 
                msg.Contains("An error occurred in") && 
                msg.Contains("Test exception"))), 
            Times.Once);
    }

    [Test]
    public void Init_WithBaseViewModelHavingNullToastService_HandlesGracefully()
    {
        var viewModel = new TestViewModelWithToast(null);
        
        Assert.DoesNotThrow(() => _decorator.Init(viewModel, _testMethod, []));
        Assert.DoesNotThrow(() => _decorator.OnException(new Exception("test")));
    }

    [Test]
    public void Init_WithNonBaseViewModelInstance_DoesNotSetToastService()
    {
        var regularObject = new object();
        
        _decorator.Init(regularObject, _testMethod, []);
        
        Assert.DoesNotThrow(() => _decorator.OnException(new Exception("test")));
    }

    [Test]
    public void OnEntry_DoesNotThrow()
    {
        _decorator.Init(new object(), _testMethod, []);
        
        Assert.DoesNotThrow(() => _decorator.OnEntry());
    }

    [Test]
    public void OnExit_DoesNotThrow()
    {
        _decorator.Init(new object(), _testMethod, []);
        
        Assert.DoesNotThrow(() => _decorator.OnExit());
    }

    [Test]
    public void OnException_WithoutToastService_DoesNotThrow()
    {
        var regularObject = new object();
        _decorator.Init(regularObject, _testMethod, []);
        var exception = new InvalidOperationException("Test exception");
        
        Assert.DoesNotThrow(() => _decorator.OnException(exception));
    }

    [Test]
    public void CompleteLifecycle_WithBaseViewModel_ExecutesAllMethods()
    {
        var viewModel = new TestViewModelWithToast(_mockToastService.Object);
        var exception = new ArgumentException("Test exception");
        
        _decorator.Init(viewModel, _testMethod, ["test", 123]);
        _decorator.OnEntry();
        _decorator.OnExit();
        _decorator.OnException(exception);
        
        _mockToastService.Verify(
            ts => ts.ShowError(It.IsAny<string>()), 
            Times.Once);
    }

    [Test]
    public void CompleteLifecycle_WithRegularObject_ExecutesAllMethods()
    {
        var regularObject = new { Name = "Test" };
        var exception = new ArgumentException("Test exception");
        
        Assert.DoesNotThrow(() => {
            _decorator.Init(regularObject, _testMethod, ["test", 123]);
            _decorator.OnEntry();
            _decorator.OnExit();
            _decorator.OnException(exception);
        });
    }
    
    [Test]
    public void Init_WithMethodHavingDeclaringType_SetsMethodNameWithFullTypeName()
    {
        var viewModel = new TestViewModelWithToast(_mockToastService.Object);
        var testMethod = typeof(TestViewModelWithToast).GetConstructor(
            BindingFlags.Public | BindingFlags.Instance, 
            null, 
            [typeof(IToastServiceWrapper)], 
            null)!;

        _decorator.Init(viewModel, testMethod, []);
        _decorator.OnException(new Exception("test"));

        _mockToastService.Verify(
            ts => ts.ShowError(It.Is<string>(msg => 
                msg.Contains($"{typeof(TestViewModelWithToast).FullName}..ctor"))), 
            Times.Once);
    }

    [Test]
    public void Init_WithMethodHavingNullDeclaringType_HandlesNullGracefully()
    {
        var viewModel = new TestViewModelWithToast(_mockToastService.Object);
        var mockMethod = new Mock<MethodInfo>();
        mockMethod.Setup(m => m.Name).Returns("TestMethod");
        mockMethod.Setup(m => m.DeclaringType).Returns((Type?)null);
    
        _decorator.Init(viewModel, mockMethod.Object, []);
        _decorator.OnException(new Exception("test"));
    
        _mockToastService.Verify(
            ts => ts.ShowError(It.Is<string>(msg => 
                msg.Contains(".TestMethod") && 
                !msg.Contains("null"))), 
            Times.Once);
    }
}