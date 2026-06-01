using System.ComponentModel.DataAnnotations;

namespace Application.Dtos;

public class ForgotPasswordDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; }
}

public class ResetPasswordDto
{
    public string NewPassword { get; set; }

    [Compare("NewPassword", ErrorMessage = "The passwords do not match.")]
    public string ConfirmPassword { get; set; }
}
