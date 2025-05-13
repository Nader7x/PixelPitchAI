using Domain.Interfaces;
using MediatR;


namespace Application.CQRS.Auth.Commands;

public class LoginUserCommand : IRequest<LoginUserCommandResponse>
{
    public string Email { get; set; }
    public string Password { get; set; }
    public string IpAddress { get; set; }
}

public class LoginUserCommandResponse
{
    public bool Succeeded { get; set; }
    public string UserId { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
    public DateTime? TokenExpires { get; set; }
    public string Error { get; set; }
}

public class LoginUserCommandHandler(
    IApplicationUserRepository userRepository,
    ITokenService tokenService,
    IUnitOfWork unitOfWork)
    : IRequestHandler<LoginUserCommand, LoginUserCommandResponse>
{
    public async Task<LoginUserCommandResponse> Handle(LoginUserCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Find user by email
            var user = await userRepository.GetByEmailAsync(request.Email);
            if (user == null)
            {
                return new LoginUserCommandResponse
                {
                    Succeeded = false,
                    Error = "Invalid email or password"
                };
            }

            // Verify password
            var isPasswordValid = await userRepository.CheckPasswordAsync(user, request.Password);
            if (!isPasswordValid)
            {
                return new LoginUserCommandResponse
                {
                    Succeeded = false,
                    Error = "Invalid email or password"
                };
            }

            // Update last login time
            user.LastLogin = DateTime.UtcNow;
            // Generate JWT token and refresh token
            var (accessToken, refreshToken) = await tokenService.GenerateTokensAsync(user, request.IpAddress);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            // Return successful response
            return new LoginUserCommandResponse
            {
                Succeeded = true,
                UserId = user.Id,
                Username = user.UserName,
                Email = user.Email,
                AccessToken = accessToken,
                RefreshToken = refreshToken.Token,
                TokenExpires = DateTime.Now.AddMinutes(int.Parse("60")) // Should match JWT expiration
            };
        }
        catch (Exception ex)
        {
            return new LoginUserCommandResponse
            {
                Succeeded = false,
                Error = ex.Message
            };
        }
    }
}