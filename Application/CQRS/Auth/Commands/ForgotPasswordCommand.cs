using Application.Services;
using Domain.Interfaces;
using Domain.Models;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace Application.CQRS.Auth.Commands;

public class ForgotPasswordCommand : IRequest<ForgotPasswordCommandResponse>
{
    public string Email { get; set; }
}

public class ForgotPasswordCommandResponse
{
    public bool Succeeded { get; set; }
    public string Error { get; set; }
}

public class ForgotPasswordCommandHandler(
    IIdentityService identityService,
    IEmailService emailService,
    IConfiguration configuration,
    UserManager<ApplicationUser> userManager)
    : IRequestHandler<ForgotPasswordCommand, ForgotPasswordCommandResponse>
{
    public async Task<ForgotPasswordCommandResponse> Handle(
        ForgotPasswordCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var user = await identityService.GetUserByEmailAsync(request.Email);
            if (user == null)
                // Don't reveal that the user does not exist
                return new ForgotPasswordCommandResponse { Succeeded = true };

            // Check if email is confirmed
            var isEmailConfirmed = await userManager.IsEmailConfirmedAsync(user);
            if (!isEmailConfirmed)
                return new ForgotPasswordCommandResponse
                {
                    Succeeded = false,
                    Error = "Email is not confirmed. Please confirm your email before resetting your password."
                };

            // Generate password reset token
            var token = await userManager.GeneratePasswordResetTokenAsync(user);

            // Create reset link
            var appUrl = configuration["AppUrl"] ?? "https://Footex.AI";
            var resetLink =
                $"{appUrl}/reset-password?email={Uri.EscapeDataString(user.Email)}&token={Uri.EscapeDataString(token)}";

            // Send email
            await emailService.SendEmailAsync(
                user.Email,
                "Reset Your Password",
                $"Please reset your password by clicking <a href='{resetLink}'>here</a>.");

            return new ForgotPasswordCommandResponse { Succeeded = true };
        }
        catch (Exception ex)
        {
            return new ForgotPasswordCommandResponse
            {
                Succeeded = false,
                Error = ex.Message
            };
        }
    }
}