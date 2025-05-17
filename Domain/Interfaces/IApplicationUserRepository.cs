using Domain.Models;
namespace Domain.Interfaces;

public interface IApplicationUserRepository : IRepository<ApplicationUser>
{
    Task<ApplicationUser?> GetByIdAsync(string userId);
    Task<ApplicationUser?> GetByEmailAsync(string email);
    Task<ApplicationUser?> GetByUsernameAsync(string username);
    Task<IEnumerable<string>> GetUserRolesAsync(ApplicationUser? user);
    Task<bool> CheckPasswordAsync(ApplicationUser? user, string password);
    Task<bool> AddToRoleAsync(ApplicationUser? user, string role);
    Task<RefreshToken?> GetRefreshTokenAsync(string token);
    Task AddRefreshTokenAsync(ApplicationUser user, RefreshToken? refreshToken);
    Task RevokeRefreshTokenAsync(string token, string ipAddress);
    Task<bool> HasRefreshTokensAsync(string userid);
    Task<ApplicationUser?> GetByIdAsyncWithTeam(string userId);

}
