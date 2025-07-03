using Domain.Models;

namespace Domain.Interfaces;

public interface ITokenService
{
    Task<string> CreateTokenAsync(ApplicationUser user);
    RefreshToken? GenerateRefreshToken(string ipAddress);
    Task<(string Token, RefreshToken RefreshToken)> GenerateTokenAsync(
        ApplicationUser user,
        string ipAddress
    );
    Task<(string Token, RefreshToken RefreshToken)> RefreshTokenAsync(
        string token,
        string ipAddress
    );
    Task RevokeTokenAsync(string token, string ipAddress);
}
