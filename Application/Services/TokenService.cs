using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Domain.Interfaces;
using Domain.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Application.Services;

public class TokenService(
    IConfiguration configuration,
    UserManager<ApplicationUser> userManager,
    IApplicationUserRepository userRepository
) : ITokenService
{
    public async Task<string> CreateTokenAsync(ApplicationUser user)
    {
        var signingCredentials = GetSigningCredentials();
        var claims = await GetClaims(user);
        var tokenOptions = GenerateTokenOptions(signingCredentials, claims);
        var hasClaims = userManager.GetClaimsAsync(user).Result.Any();

        if (hasClaims)
            return new JwtSecurityTokenHandler().WriteToken(tokenOptions);
        // Add claims to user
        var claimsIdentity = new ClaimsIdentity(claims);
        await userManager.AddClaimsAsync(user, claimsIdentity.Claims);

        return new JwtSecurityTokenHandler().WriteToken(tokenOptions);
    }

    public RefreshToken GenerateRefreshToken(string ipAddress)
    {
        using var rng = RandomNumberGenerator.Create();
        var randomBytes = new byte[64];
        rng.GetBytes(randomBytes);

        return new RefreshToken
        {
            Token = Convert.ToBase64String(randomBytes),
            Expires = DateTime.UtcNow.AddDays(7),
            Created = DateTime.UtcNow,
            CreatedByIp = ipAddress,
        };
    }

    public DateTime GetTokenExpirationTime(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        if (!handler.CanReadToken(token))
            throw new ArgumentException("Invalid token");

        var jwtToken = handler.ReadJwtToken(token);
        return jwtToken.ValidTo;
    }

    public async Task<(string Token, RefreshToken RefreshToken)> GenerateTokenAsync(
        ApplicationUser user,
        string ipAddress
    )
    {
        var token = await CreateTokenAsync(user);
        var refreshToken = GenerateRefreshToken(ipAddress);

        // Add refresh token to user
        await userRepository.AddRefreshTokenAsync(user, refreshToken);

        return (token, refreshToken);
    }

    public async Task<(string Token, RefreshToken RefreshToken)> RefreshTokenAsync(
        string token,
        string ipAddress
    )
    {
        var refreshToken = await userRepository.GetRefreshTokenAsync(token);
        Console.WriteLine(refreshToken);
        if (refreshToken is not { IsActive: true })
            throw new SecurityTokenException("Invalid refresh token");

        var user = refreshToken.User;

        // Generate new tokens
        if (user == null)
            return (string.Empty, null)!;
        var newToken = await CreateTokenAsync(user);
        var newRefreshToken = GenerateRefreshToken(ipAddress);

        // Revoke current refresh token
        refreshToken.Revoked = DateTime.UtcNow;
        refreshToken.RevokedByIp = ipAddress;
        refreshToken.ReplacedByToken = newRefreshToken.Token;

        // Save new refresh token
        await userRepository.AddRefreshTokenAsync(user, newRefreshToken);

        return (newToken, newRefreshToken);
    }

    public async Task RevokeTokenAsync(string token, string ipAddress)
    {
        await userRepository.RevokeRefreshTokenAsync(token, ipAddress);
    }

    private SigningCredentials GetSigningCredentials()
    {
        var key = Encoding.UTF8.GetBytes(configuration["JWT:Secret"] ?? string.Empty);
        var secret = new SymmetricSecurityKey(key);

        return new SigningCredentials(secret, SecurityAlgorithms.HmacSha256);
    }

    private async Task<List<Claim>> GetClaims(ApplicationUser user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, user.UserName),
            new(ClaimTypes.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, user.Id),
        };

        var roles = await userManager.GetRolesAsync(user);
        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        // Add custom claims if needed
        if (user.FavoriteTeamId.HasValue)
            claims.Add(new Claim("FavoriteTeamId", user.FavoriteTeamId.Value.ToString()));

        return claims;
    }

    private JwtSecurityToken GenerateTokenOptions(
        SigningCredentials signingCredentials,
        List<Claim> claims
    )
    {
        var isAdmin = claims.Any(c => c is { Type: ClaimTypes.Role, Value: "Admin" });
        var baseExpiration = DateTime.UtcNow.AddMinutes(
            Convert.ToDouble(configuration["JWT:ExpiryInMinutes"])
        );
        var expiration = isAdmin ? baseExpiration.AddDays(10) : baseExpiration;

        var tokenOptions = new JwtSecurityToken(
            configuration["JWT:ValidIssuer"],
            configuration["JWT:ValidAudience"],
            claims,
            expires: expiration,
            signingCredentials: signingCredentials
        );

        return tokenOptions;
    }
}
