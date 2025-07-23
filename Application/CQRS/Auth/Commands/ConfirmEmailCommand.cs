using Domain.Interfaces;
using Domain.Models;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Application.CQRS.Auth.Commands;

public class ConfirmEmailCommand : IRequest<ConfirmEmailCommandResponse>
{
    public string UserId { get; set; }
    public string Token { get; set; }
}

public class ConfirmEmailCommandResponse
{
    public bool Succeeded { get; set; }
    public string? Error { get; set; }
}

public class ConfirmEmailCommandHandler
    : IRequestHandler<ConfirmEmailCommand, ConfirmEmailCommandResponse>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IApplicationUserRepository _userRepository;

    public ConfirmEmailCommandHandler(
        IApplicationUserRepository userRepository,
        IIdentityService identityService,
        UserManager<ApplicationUser> userManager
    )
    {
        _userRepository = userRepository;
        _userManager = userManager;
    }

    public async Task<ConfirmEmailCommandResponse> Handle(
        ConfirmEmailCommand request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(request.UserId);
            if (user == null)
                return new ConfirmEmailCommandResponse
                {
                    Succeeded = false,
                    Error = "User not found.",
                };

            var result = await _userManager.ConfirmEmailAsync(user, request.Token);

            if (!result.Succeeded)
                return new ConfirmEmailCommandResponse
                {
                    Succeeded = false,
                    Error = string.Join(", ", result.Errors.Select(e => e.Description)),
                };

            return new ConfirmEmailCommandResponse { Succeeded = true };
        }
        catch (Exception ex)
        {
            return new ConfirmEmailCommandResponse { Succeeded = false, Error = ex.Message };
        }
    }
}
