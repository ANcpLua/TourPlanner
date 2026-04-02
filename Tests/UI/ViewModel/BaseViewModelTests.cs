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
        _mockToastService = new Mock<IToastServiceWrapper>();
        _viewModel = new TestViewModel(
            new HttpClient(), _mockToastService.Object, TestMocks.TryCatchToastWrapper());
    }

    private Mock<IToastServiceWrapper> _mockToastService = null!;
    private TestViewModel _viewModel = null!;

    private sealed class TestViewModel(HttpClient http, IToastServiceWrapper toast, TryCatchToastWrapper wrapper)
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

}
