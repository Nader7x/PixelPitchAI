using Domain.Models;
using System.Threading.Tasks;

namespace Domain.Interfaces;

public interface IIdentityService
{
    Task<(bool Succeeded, string UserId)> CreateUserAsync(ApplicationUser user, string password);
    Task<bool> DeleteUserAsync(string userId);
    Task<ApplicationUser> GetUserByIdAsync(string userId);
    Task<ApplicationUser> GetUserByEmailAsync(string email);
    Task<bool> CheckPasswordAsync(ApplicationUser user, string password);
    Task<bool> AddUserToRoleAsync(ApplicationUser user, string role);
    Task<bool> IsInRoleAsync(string userId, string role);
}
