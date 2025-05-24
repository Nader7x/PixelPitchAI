using Microsoft.AspNetCore.Http;

namespace Application.Dtos;


public class UserDto
{
    public string Email { get; set; }
    public string UserName { get; set; }
    public string PhoneNumber { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string ImageUrl { get; set; }
    public string Age { get; set; }
}
public class UserLoginDto
{
    public string Email { get; set; }
    public string Password { get; set; }
}

public class UpdateUserDto
{
    public string Id { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public IFormFile? Image { get; set; }
    public int? Age { get; set; }
    public string? CurrentPassword { get; set; }
    public string? NewPassword { get; set; }
    public string? Gender { get; set; }
}

public class RegisterUserDto
{
    public required string FirstName { get; set; }
    public string? LastName { get; set; }
    public string UserName { get; set; }
    public required string Email { get; set; }
    public required string Password { get; set; }
    public string? Gender { get; set; }
    public int Age { get; set; }
    public int? FavoriteTeamId { get; set; }
    public IFormFile? Image { get; set; }
    public string? PhoneNumber { get; set; }
    
}
