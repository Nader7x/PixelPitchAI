using Application.Interfaces;
using Domain.Interfaces;
using MediatR;

namespace Application.CQRS.Auth.Commands;

public class LoginUserCommand : IRequest<LoginUserCommandResponse>
{
    public required string Email { get; set; }
    public required string Password { get; set; }
    public string? IpAddress { get; set; }
}

public class LoginUserCommandResponse
{
    public bool Succeeded { get; init; }
    public string? UserId { get; init; }
    public string? Username { get; init; }
    public string? Email { get; init; }
    public string? AccessToken { get; init; }
    public string? RefreshToken { get; init; }
    public IEnumerable<string>? Roles { get; init; }
    public DateTime? TokenExpires { get; init; }
    public string? Error { get; init; }
}

public class LoginUserCommandHandler(
    IApplicationUserRepository userRepository,
    ITokenService tokenService,
    IUnitOfWork unitOfWork,
    IUserMapper userMapper
) : IRequestHandler<LoginUserCommand, LoginUserCommandResponse>
{
    private readonly IUserMapper _userMapper = userMapper;

    public async Task<LoginUserCommandResponse> Handle(
        LoginUserCommand request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // Find user by email
            var user = await userRepository.GetByEmailAsync(request.Email);
            if (user == null)
                return new LoginUserCommandResponse
                {
                    Succeeded = false,
                    Error = "Invalid email or password",
                };

            // Verify password
            var isPasswordValid = await userRepository.CheckPasswordAsync(user, request.Password);
            if (!isPasswordValid)
                return new LoginUserCommandResponse
                {
                    Succeeded = false,
                    Error = "Invalid email or password",
                };

            // Update last login time
            user.LastLogin = DateTime.UtcNow;
            // Generate JWT token and refresh token
            var (accessToken, refreshToken) = await tokenService.GenerateTokenAsync(
                user,
                request.IpAddress ?? "0.0.0.0"
            );
            await userRepository.AddRefreshTokenAsync(user, refreshToken);
            userRepository.Update(user);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            // get usr roles
            var roles = await userRepository.GetUserRolesAsync(user);
            // Return successful response
            return new LoginUserCommandResponse
            {
                Succeeded = true,
                UserId = user.Id,
                Username = user.UserName,
                Email = user.Email,
                AccessToken = accessToken,
                RefreshToken = refreshToken.Token,
                Roles = roles,
                TokenExpires = tokenService.GetTokenExpirationTime(accessToken),
            };
        }
        catch (Exception ex)
        {
            return new LoginUserCommandResponse { Succeeded = false, Error = ex.Message };
        }
    }
}
