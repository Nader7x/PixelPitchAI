using Domain.Models;
using Microsoft.AspNetCore.Identity;

namespace Domain.Interfaces;

public interface IdentityService
{
    Task<(bool Succeeded, string UserId, IdentityResult result)> CreateUserAsync(
        ApplicationUser user,
        string password
    );
    Task<bool> DeleteUserAsync(string userId);
    Task<ApplicationUser> GetUserByIdAsync(string userId);
    Task<ApplicationUser> GetUserByEmailAsync(string email);
    Task<bool> CheckPasswordAsync(ApplicationUser user, string password);
    Task<bool> AddUserToRoleAsync(ApplicationUser user, string role);
    Task<bool> IsInRoleAsync(string userId, string role);
}
