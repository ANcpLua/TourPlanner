using UI.Decorator;
using UI.Service.Interface;
using UI.ViewModel.Base;

namespace Tests.UI.ViewModel;

[TestFixture]
public class BaseViewModelTests
{
    [SetUp]
    public void Setup()
    {
        _mockHttpService = new Mock<IHttpService>();
        _mockToastService = new Mock<IToastServiceWrapper>();
        _viewModel = new TestViewModel(
            _mockHttpService.Object, _mockToastService.Object, TestData.MockTryCatchToastWrapper());
    }

    private Mock<IHttpService> _mockHttpService = null!;
    private Mock<IToastServiceWrapper> _mockToastService = null!;
    private TestViewModel _viewModel = null!;

    private class TestViewModel(IHttpService http, IToastServiceWrapper toast, TryCatchToastWrapper wrapper)
        : BaseViewModel(http, toast, wrapper)
    {
        public new void OnPropertyChanged(string? propertyName = null)
        {
            base.OnPropertyChanged(propertyName);
        }
    }

    [Test]
    public void Constructor_InitializesProperties()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_viewModel.HttpService, Is.Not.Null);
            Assert.That(_viewModel.ToastServiceWrapper, Is.Not.Null);
            Assert.That(_viewModel.IsProcessing, Is.False);
        }
    }

    [Test]
    public void OnPropertyChanged_RaisesEvent()
    {
        string? raisedProperty = null;
        _viewModel.PropertyChanged += (_, e) => raisedProperty = e.PropertyName;

        _viewModel.OnPropertyChanged("TestProperty");

        Assert.That(raisedProperty, Is.EqualTo("TestProperty"));
    }

    [Test]
    public void OnPropertyChanged_NullPropertyName_RaisesEvent()
    {
        var eventRaised = false;
        _viewModel.PropertyChanged += (_, _) => eventRaised = true;

        _viewModel.OnPropertyChanged();

        Assert.That(eventRaised, Is.True);
    }

    [Test]
    public void Process_WhenAlreadyProcessing_ReturnsDefault()
    {
        _viewModel.IsProcessing = true;

        var result = _viewModel.Process(static () => "test");

        Assert.That(result, Is.Null);
    }

    [Test]
    public void Process_WhenNotProcessing_ExecutesAndReturnsResult()
    {
        var result = _viewModel.Process(static () => "success");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.EqualTo("success"));
            Assert.That(_viewModel.IsProcessing, Is.False);
        }
    }

    [Test]
    public void Process_Exception_ResetsIsProcessing()
    {
        Assert.Throws<InvalidOperationException>(() =>
            _viewModel.Process<string>(static () => throw new InvalidOperationException()));

        Assert.That(_viewModel.IsProcessing, Is.False);
    }

    [Test]
    public void SetProperty_WhenValueChanges_UpdatesAndReturnsTrue()
    {
        var propertyName = string.Empty;
        _viewModel.PropertyChanged += (_, e) => propertyName = e.PropertyName;

        _viewModel.IsProcessing = true;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_viewModel.IsProcessing, Is.True);
            Assert.That(propertyName, Is.EqualTo(nameof(_viewModel.IsProcessing)));
        }
    }

    [Test]
    public async Task HandleApiRequestAsync_Success_ReturnsResult()
    {
        _mockHttpService.Setup(static h => h.GetAsync<string>("test"))
            .ReturnsAsync("result");

        var result = await _viewModel.HandleApiRequestAsync(
            async () => await _viewModel.HttpService.GetAsync<string>("test"),
            "Error message"
        );

        Assert.That(result, Is.EqualTo("result"));
    }
}