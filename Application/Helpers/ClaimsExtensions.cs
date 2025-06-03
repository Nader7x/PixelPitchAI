using System.Security.Claims;

namespace Application.Helpers;

public static class  ClaimsExtensions
{
    public static string? GetUsername(this ClaimsPrincipal user)
    {
        return user.FindFirstValue(ClaimTypes.GivenName);
    }

    public static string? GetNameId(this ClaimsPrincipal user)
    {
        return user.FindFirstValue( ClaimTypes.NameIdentifier);
    }

    public static string? GetEmail(this ClaimsPrincipal user)
    {
        return user.FindFirstValue( ClaimTypes.Email);
    }

    public static string? GetRole(this ClaimsPrincipal user)
    {
        return user.FindFirstValue( ClaimTypes.Role);
    }
}