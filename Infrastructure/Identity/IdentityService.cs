using Domain.Interfaces;
using Domain.Models;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Identity;

public class IdentityService(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager
) : IIdentityService
{
    public async Task<(bool Succeeded, string UserId, IdentityResult result)> CreateUserAsync(
        ApplicationUser user,
        string password
    )
    {
        var result = await userManager.CreateAsync(user, password);
        return (result.Succeeded, user.Id, result);
    }

    public async Task<bool> DeleteUserAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user == null)
            return false;

        var result = await userManager.DeleteAsync(user);
        return result.Succeeded;
    }

    public async Task<ApplicationUser> GetUserByIdAsync(string userId)
    {
        return await userManager.FindByIdAsync(userId);
    }

    public async Task<ApplicationUser> GetUserByEmailAsync(string email)
    {
        return await userManager.FindByEmailAsync(email);
    }

    public async Task<bool> CheckPasswordAsync(ApplicationUser user, string password)
    {
        return await userManager.CheckPasswordAsync(user, password);
    }

    public async Task<bool> AddUserToRoleAsync(ApplicationUser user, string role)
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));

        var result = await userManager.AddToRoleAsync(user, role);
        return result.Succeeded;
    }

    public async Task<bool> IsInRoleAsync(string userId, string role)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user == null)
            return false;

        return await userManager.IsInRoleAsync(user, role);
    }
}
