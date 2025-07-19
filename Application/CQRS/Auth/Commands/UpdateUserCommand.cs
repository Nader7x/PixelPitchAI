using Application.Interfaces;
using Domain.Interfaces;
using Domain.Models;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Application.CQRS.Auth.Commands;

public class UpdateUserCommand : IRequest<UpdateUserCommandResponse>
{
    public string Id { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? CurrentPassword { get; set; }
    public string? NewPassword { get; set; }
    public string? ImageUrl { get; set; }
    public string? UserName { get; set; }

    public int? Age { get; set; }
    public string? Gender { get; set; }
}

public class UpdateUserCommandResponse
{
    public bool Succeeded { get; init; }
    public bool NotFound { get; init; }
    public string? Error { get; init; }
    public string? ImageUrl { get; init; }
}

public class UpdateUserCommandHandler(
    IUnitOfWork unitOfWork,
    UserManager<ApplicationUser> userManager,
    IUserMapper userMapper
) : IRequestHandler<UpdateUserCommand, UpdateUserCommandResponse>
{
    private readonly UserManager<ApplicationUser> _userManager = userManager;

    public async Task<UpdateUserCommandResponse> Handle(
        UpdateUserCommand request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var user = await unitOfWork.ApplicationUser.GetByIdAsync(request.Id);
            if (user == null)
                return new UpdateUserCommandResponse
                {
                    Succeeded = false,
                    NotFound = true,
                    Error = $"User with ID {request.Id} not found",
                };
            if (
                !string.IsNullOrEmpty(request.CurrentPassword)
                && !string.IsNullOrEmpty(request.NewPassword)
            )
                await _userManager.ChangePasswordAsync(
                    user,
                    request.CurrentPassword,
                    request.NewPassword
                );

            userMapper.UpdateUserFromCommand(request, user);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return user.ImageUrl != null
                ? new UpdateUserCommandResponse { Succeeded = true, ImageUrl = user.ImageUrl }
                : new UpdateUserCommandResponse { Succeeded = true };
        }
        catch (Exception ex)
        {
            return new UpdateUserCommandResponse { Succeeded = false, Error = ex.Message };
        }
    }
}
