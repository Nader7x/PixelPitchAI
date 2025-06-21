using Application.Interfaces;
using Domain.Interfaces;
using MediatR;

namespace Application.CQRS.Auth.Commands;

public class RegisterUserCommand : IRequest<RegisterUserCommandResponse>
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string UserName { get; set; }
    public required string Email { get; set; }
    public required string Password { get; set; }
    public string? Gender { get; set; }
    public int Age { get; set; }
    public int? FavoriteTeamId { get; set; }
    public string? PhoneNumber { get; set; }
    public string? ImageUrl { get; set; }
}

public class RegisterUserCommandResponse
{
    public bool Succeeded { get; set; }
    public string? UserId { get; set; }
    public string? Error { get; set; }
}

public class RegisterUserCommandHandler(
    IIdentityService identityService,
    IUnitOfWork unitOfWork,
    IUserMapper userMapper)
    : IRequestHandler<RegisterUserCommand, RegisterUserCommandResponse>
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IUserMapper _userMapper = userMapper;

    public async Task<RegisterUserCommandResponse> Handle(RegisterUserCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Create application user
            var user = _userMapper.ToUserFromRegister(request);

            // Register user
            var (succeeded, userId, result) = await identityService.CreateUserAsync(user, request.Password);

            if (!succeeded)
                return new RegisterUserCommandResponse
                {
                    Succeeded = false,
                    Error = result.ToString()
                };

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
