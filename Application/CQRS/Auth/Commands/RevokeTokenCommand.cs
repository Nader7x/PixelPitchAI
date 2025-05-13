using Domain.Interfaces;
using MediatR;

namespace Application.CQRS.Auth.Commands;

public class RevokeTokenCommand : IRequest<RevokeTokenCommandResponse>
{
    public string RefreshToken { get; set; }
    public string IpAddress { get; set; }
}

public class RevokeTokenCommandResponse
{
    public bool Succeeded { get; set; }
    public string Error { get; set; }
}

public class RevokeTokenCommandHandler(ITokenService tokenService, IUnitOfWork unitOfWork)
    : IRequestHandler<RevokeTokenCommand, RevokeTokenCommandResponse>
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<RevokeTokenCommandResponse> Handle(RevokeTokenCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Revoke token
            await tokenService.RevokeTokenAsync(request.RefreshToken, request.IpAddress);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return new RevokeTokenCommandResponse
            {
                Succeeded = true
            };
        }
        catch (Exception ex)
        {
            return new RevokeTokenCommandResponse
            {
                Succeeded = false,
                Error = ex.Message
            };
        }
    }
}