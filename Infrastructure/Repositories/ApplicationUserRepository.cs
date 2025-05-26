using System.IdentityModel.Tokens.Jwt;
using Domain.Interfaces;
using Domain.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class ApplicationUserRepository(
    FootballDbContext context,
    UserManager<ApplicationUser> userManager)
    : Repository<ApplicationUser>(context), IApplicationUserRepository
{
    private readonly FootballDbContext _context = context;

    public async Task<ApplicationUser?> GetByIdAsync(string userId)
    {
        return await userManager.FindByIdAsync(userId);
    }

    public async Task<ApplicationUser?> GetByEmailAsync(string email)
    {
        return await userManager.FindByEmailAsync(email);
    }

    public async Task<ApplicationUser?> GetByUsernameAsync(string username)
    {
        return await userManager.FindByNameAsync(username);
    }

    public async Task<IEnumerable<string>> GetUserRolesAsync(ApplicationUser? user)
    {
        if (user != null) return await userManager.GetRolesAsync(user);
        return [];
    }

    public async Task<bool> CheckPasswordAsync(ApplicationUser? user, string password)
    {
        return user != null && await userManager.CheckPasswordAsync(user, password);
    }

    public async Task<bool> AddToRoleAsync(ApplicationUser? user, string role)
    {
        if (user == null) return false;
        var result = await userManager.AddToRoleAsync(user, role);
        return result.Succeeded;
    }

    public async Task<RefreshToken?> GetRefreshTokenAsync(string token)
    {
        return await _context.RefreshTokens
            .Include(r => r.User)
            .SingleOrDefaultAsync(t => t.Token == token);
    }

    public async Task AddRefreshTokenAsync(ApplicationUser? user, RefreshToken? refreshToken)
    {
        if (refreshToken != null)
        {
            refreshToken.UserId = user?.Id;
            refreshToken.User = user;
            refreshToken.Created = System.DateTime.UtcNow;
            refreshToken.Expires = System.DateTime.UtcNow.AddDays(7);
            if (user != null)
                refreshToken.JwtId = await userManager.GetClaimsAsync(user)
                    .ContinueWith(t => t.Result.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value);
            _context.RefreshTokens.Add(refreshToken);
        }

        await _context.SaveChangesAsync();
    }

    public async Task RevokeRefreshTokenAsync(string token, string ipAddress)
    {
        var refreshToken = await _context.RefreshTokens.SingleOrDefaultAsync(t => t.Token == token);

        if (refreshToken != null)
        {
            refreshToken.Revoked = System.DateTime.UtcNow;
            refreshToken.RevokedByIp = ipAddress;

            _context.RefreshTokens.Update(refreshToken);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> HasRefreshTokensAsync(string userId)
    {
        var refreshTokens = await _context.RefreshTokens
            .Where(t => t.UserId == userId)
            .ToListAsync();

        return refreshTokens.Count != 0;
    }

    public async Task<ApplicationUser?> GetByIdAsyncWithTeam(string userId)
    {
        return await _context.Users
            .Include(u => u.FavoriteTeam)
            .AsSplitQuery()
            .FirstOrDefaultAsync(u => u.Id == userId);
    }
}