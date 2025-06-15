using ToastService = UI.Service.ToastService;

namespace Test.UI.Services;

[TestFixture]
public class ToastServiceTests
{
    private Mock<IToastService> _mockBlazorToastService = null!;
    private ToastService _sut = null!;

    [SetUp]
    public void Setup()
    {
        _mockBlazorToastService = new Mock<IToastService>();
        _sut = new ToastService(_mockBlazorToastService.Object);
    }

    [Test]
    public void ShowSuccess_WithMessage_CallsUnderlyingToastService()
    {
        const string message = "Tour saved successfully";

        _sut.ShowSuccess(message);

        _mockBlazorToastService.Verify(
            t => t.ShowSuccess(message, null),
            Times.Once);
    }

    [Test]
    public void ShowError_WithMessage_CallsUnderlyingToastService()
    {
        const string message = "Failed to save tour";

        _sut.ShowError(message);

        _mockBlazorToastService.Verify(
            t => t.ShowError(message, null),
            Times.Once);
    }

    [Test]
    public void ShowSuccess_WithEmptyMessage_CallsToastService()
    {
        const string message = "";

        _sut.ShowSuccess(message);

        _mockBlazorToastService.Verify(
            t => t.ShowSuccess(message, null),
            Times.Once);
    }

    [Test]
    public void ShowError_WithEmptyMessage_CallsToastService()
    {
        const string message = "";

        _sut.ShowError(message);

        _mockBlazorToastService.Verify(
            t => t.ShowError(message, null),
            Times.Once);
    }

    [Test]
    public void ShowSuccess_WithValidationMessage_CallsToastService()
    {
        const string message = TestData.ValidSearchText;

        _sut.ShowSuccess(message);

        _mockBlazorToastService.Verify(
            t => t.ShowSuccess(message, null),
            Times.Once);
    }

    [Test]
    public void ShowError_WithValidationMessage_CallsToastService()
    {
        const string message = TestData.InvalidSearchText;

        _sut.ShowError(message);

        _mockBlazorToastService.Verify(
            t => t.ShowError(message, null),
            Times.Once);
    }
}