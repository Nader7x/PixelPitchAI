using Application.Interfaces;
using Domain.Interfaces;
using Domain.Models;
using MediatR;

namespace Application.CQRS.Auth.Commands;

public class RegisterUserCommand : IRequest<RegisterUserCommandResponse>
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public int? FavoriteTeamId { get; set; }
}

public class RegisterUserCommandResponse
{
    public bool Succeeded { get; set; }
    public string UserId { get; set; }
    public string Error { get; set; }
}

public class RegisterUserCommandHandler(
    IIdentityService identityService,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RegisterUserCommand, RegisterUserCommandResponse>
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<RegisterUserCommandResponse> Handle(RegisterUserCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Create application user
            var user = new ApplicationUser
            {
                UserName = request.Username ?? request.Email,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                FavoriteTeamId = request.FavoriteTeamId,
                Created = DateTime.UtcNow,
                EmailConfirmed = true // In a real app, you'd likely set this to false and confirm email
            };

            // Register user
            var (succeeded, userId) = await identityService.CreateUserAsync(user, request.Password);

            if (!succeeded)
            {
                return new RegisterUserCommandResponse
                {
                    Succeeded = false,
                    Error = "Failed to create user"
                };
            }

            // Add to default role
            await identityService.AddUserToRoleAsync(user, "User");
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            // Return response
            return new RegisterUserCommandResponse
            {
                Succeeded = true,
                UserId = userId
            };
        }
        catch (Exception ex)
        {
            return new RegisterUserCommandResponse
            {
                Succeeded = false,
                Error = ex.Message
            };
        }
    }
}