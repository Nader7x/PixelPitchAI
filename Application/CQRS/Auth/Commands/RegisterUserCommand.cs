using Application.Mappers;
using Domain.Interfaces;
using Domain.Models;
using MediatR;

namespace Application.CQRS.Auth.Commands;

public class RegisterUserCommand : IRequest<RegisterUserCommandResponse>
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string UserName { get; set; }
    public string Password { get; set; }
    public string? Gender { get; set; }
    public int Age { get; set; }
    public int? FavoriteTeamId { get; set; }
    public string? PhoneNumber { get; set; }
    public string? ImageUrl { get; set; }
}

public class RegisterUserCommandResponse
{
    public bool Succeeded { get; set; }
    public string UserId { get; set; }
    public string Error { get; set; }
}

public class RegisterUserCommandHandler(
    IIdentityService identityService,
    IUnitOfWork unitOfWork,
    UserMapper userMapper)
    : IRequestHandler<RegisterUserCommand, RegisterUserCommandResponse>
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly UserMapper _userMapper = userMapper;

    public async Task<RegisterUserCommandResponse> Handle(RegisterUserCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Create application user
            var user = _userMapper.ToUserFromRegister(request);
            Console.WriteLine(user);

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