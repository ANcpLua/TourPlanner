using System.ComponentModel.DataAnnotations;

namespace Contracts.Auth;

public class RegisterRequest
{
    [Required]
    [EmailAddress]
    public required string Email { get; set; }

    [Required]
    [StringLength(128, MinimumLength = 6)]
    public required string Password { get; set; }
}
