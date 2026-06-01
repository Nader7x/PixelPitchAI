using System.ComponentModel.DataAnnotations;

namespace Application.Dtos;

public class ConfirmEmailDto
{
    [Required]
    public string UserId { get; set; }

    [Required]
    public string Token { get; set; }
}

public class ResendEmailConfirmationDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; }
}
