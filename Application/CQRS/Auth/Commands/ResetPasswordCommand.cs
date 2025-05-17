using Domain.Interfaces;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using Domain.Models;
using Microsoft.AspNetCore.Identity;

namespace Application.CQRS.Auth.Commands;

public class ResetPasswordCommand : IRequest<ResetPasswordCommandResponse>
{
    public string Email { get; set; }
    public string Token { get; set; }
    public string NewPassword { get; set; }
}

public class ResetPasswordCommandResponse
{
    public bool Succeeded { get; set; }
    public string Error { get; set; }
}

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, ResetPasswordCommandResponse>
{
    private readonly IIdentityService _identityService;
    private readonly UserManager<ApplicationUser> _userManager;

    public ResetPasswordCommandHandler(IIdentityService identityService, UserManager<ApplicationUser> userManager)
    {
        _identityService = identityService;
        _userManager = userManager;
    }

    public async Task<ResetPasswordCommandResponse> Handle(
        ResetPasswordCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var user = await _identityService.GetUserByEmailAsync(request.Email);
            if (user == null)
            {
                return new ResetPasswordCommandResponse
                {
                    Succeeded = false,
                    Error = "Invalid email address."
                };
            }

            var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
            
            if (!result.Succeeded)
            {
                return new ResetPasswordCommandResponse
                {
                    Succeeded = false,
                    Error = string.Join(", ", result.Errors.Select(e => e.Description))
                };
            }

            return new ResetPasswordCommandResponse { Succeeded = true };
        }
        catch (Exception ex)
        {
            return new ResetPasswordCommandResponse
            {
                Succeeded = false,
                Error = ex.Message
            };
        }
    }
}
