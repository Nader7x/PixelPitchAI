using System.Security.Claims;

namespace Application.Helpers;

public static class  ClaimsExtentions
{
    public static string? GetUsername(this ClaimsPrincipal user)
    {
        return user.Claims.SingleOrDefault(c => c.Type.Equals("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname"))?.Value;
    }

    public static string? GetNameId(this ClaimsPrincipal user)
    {
        return user.Claims.SingleOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
    }

    public static string? GetEmail(this ClaimsPrincipal user)
    {
        return user.Claims.SingleOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
    }

    public static string? GetRole(this ClaimsPrincipal user)
    {
        return user.Claims.SingleOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
    }
}