using Blazored.Toast.Services;
using Moq;
using ToastService = UI.Service.ToastService;

namespace Test.UI.Services;

[TestFixture]
public class ToastServiceTests
{
    [SetUp]
    public void Setup()
    {
        _mockBlazorToastService = new Mock<IToastService>();
        _toastService = new ToastService(_mockBlazorToastService.Object);
    }

    private Mock<IToastService> _mockBlazorToastService;
    private ToastService _toastService;

    [Test]
    public void ShowSuccess_WithMessage_CallsUnderlyingToastService()
    {
        const string message = "Tour saved successfully";

        _toastService.ShowSuccess(message);

        _mockBlazorToastService.Verify(
            t => t.ShowSuccess(message, null),
            Times.Once);
    }

    [Test]
    public void ShowError_WithMessage_CallsUnderlyingToastService()
    {
        const string message = "Failed to save tour";

        _toastService.ShowError(message);

        _mockBlazorToastService.Verify(
            t => t.ShowError(message, null),
            Times.Once);
    }

    [Test]
    public void ShowSuccess_WithEmptyMessage_CallsToastService()
    {
        const string message = "";

        _toastService.ShowSuccess(message);

        _mockBlazorToastService.Verify(
            t => t.ShowSuccess(message, null),
            Times.Once);
    }

    [Test]
    public void ShowError_WithEmptyMessage_CallsToastService()
    {
        const string message = "";

        _toastService.ShowError(message);

        _mockBlazorToastService.Verify(
            t => t.ShowError(message, null),
            Times.Once);
    }

    [Test]
    public void ShowSuccess_WithValidationMessage_CallsToastService()
    {
        const string message = TestData.ValidSearchText;

        _toastService.ShowSuccess(message);

        _mockBlazorToastService.Verify(
            t => t.ShowSuccess(message, null),
            Times.Once);
    }

    [Test]
    public void ShowError_WithValidationMessage_CallsToastService()
    {
        const string message = TestData.InvalidSearchText;

        _toastService.ShowError(message);

        _mockBlazorToastService.Verify(
            t => t.ShowError(message, null),
            Times.Once);
    }
}