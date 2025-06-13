using Moq;
using Serilog;
using UI.Service.Interface;
using UI.ViewModel.Base;

namespace Test.UI.ViewModel;

[TestFixture]
public class BaseViewModelTests
{
    [SetUp]
    public void Setup()
    {
        _httpService = new Mock<IHttpService>();
        _toastService = new Mock<IToastServiceWrapper>();
        _logger = new Mock<ILogger>();
        _viewModel = new TestViewModel(_httpService.Object, _toastService.Object, _logger.Object);
    }

    private Mock<IHttpService> _httpService = null!;
    private Mock<IToastServiceWrapper> _toastService = null!;
    private Mock<ILogger> _logger = null!;
    private TestViewModel _viewModel = null!;

    private class TestViewModel : BaseViewModel
    {
        public TestViewModel(IHttpService http, IToastServiceWrapper toast, ILogger logger)
            : base(http, toast, logger)
        {
        }

        public new void OnPropertyChanged(string? propertyName = null)
        {
            base.OnPropertyChanged(propertyName);
        }
    }

    [Test]
    public void Constructor_InitializesProperties()
    {
        Assert.Multiple(() =>
        {
            Assert.That(_viewModel.HttpService, Is.EqualTo(_httpService.Object));
            Assert.That(_viewModel.ToastServiceWrapper, Is.EqualTo(_toastService.Object));
            Assert.That(_viewModel.IsProcessing, Is.False);
        });
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

        var result = _viewModel.Process(string.Empty.ToString);

        Assert.That(result, Is.Null);
    }

    [Test]
    public void Process_Exception_HitsFinallyBlock()
    {
        Assert.Throws<InvalidOperationException>(() =>
            _viewModel.Process<string>(() => throw new InvalidOperationException()));

        Assert.That(_viewModel.IsProcessing, Is.False);
    }
}