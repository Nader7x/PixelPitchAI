using Domain.Interfaces;
using MediatR;

namespace Application.CQRS.Auth.Commands;

public class RefreshTokenCommand : IRequest<RefreshTokenCommandResponse>
{
    public string RefreshToken { get; set; }
    public string IpAddress { get; set; }
}

public class RefreshTokenCommandResponse
{
    public bool Succeeded { get; set; }
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
    public DateTime TokenExpires { get; set; }
    public string Error { get; set; }
}

public class RefreshTokenCommandHandler(ITokenService tokenService, IUnitOfWork unitOfWork)
    : IRequestHandler<RefreshTokenCommand, RefreshTokenCommandResponse>
{
    public async Task<RefreshTokenCommandResponse> Handle(
        RefreshTokenCommand request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // Refresh token
            var (accessToken, refreshToken) = await tokenService.RefreshTokenAsync(
                request.RefreshToken,
                request.IpAddress
            );
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return new RefreshTokenCommandResponse
            {
                Succeeded = true,
                AccessToken = accessToken,
                RefreshToken = refreshToken.Token,
                TokenExpires = DateTime.Now.AddMinutes(int.Parse("60")), // Should match JWT expiration
            };
        }
        catch (Exception ex)
        {
            return new RefreshTokenCommandResponse { Succeeded = false, Error = ex.Message };
        }
    }
}
