using Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Configuration;
using Application.Services;
using Domain.Models;
using Microsoft.AspNetCore.Identity;

namespace Application.CQRS.Auth.Commands;

public class ResendEmailConfirmationCommand : IRequest<ResendEmailConfirmationCommandResponse>
{
    public string Email { get; set; }
}

public class ResendEmailConfirmationCommandResponse
{
    public bool Succeeded { get; set; }
    public string Error { get; set; }
}

public class ResendEmailConfirmationCommandHandler : IRequestHandler<ResendEmailConfirmationCommand, ResendEmailConfirmationCommandResponse>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IIdentityService _identityService;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;

    public ResendEmailConfirmationCommandHandler(
        IIdentityService identityService,
        IEmailService emailService,
        IConfiguration configuration, UserManager<ApplicationUser> userManager)
    {
        _identityService = identityService;
        _emailService = emailService;
        _configuration = configuration;
        _userManager = userManager;
    }

    public async Task<ResendEmailConfirmationCommandResponse> Handle(
        ResendEmailConfirmationCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var user = await _identityService.GetUserByEmailAsync(request.Email);
            Console.WriteLine(user.Email);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                return new ResendEmailConfirmationCommandResponse { Succeeded = true };
            }

            // Check if email is already confirmed
            var isEmailConfirmed = await _userManager.IsEmailConfirmedAsync(user);
            if (isEmailConfirmed)
            {
                return new ResendEmailConfirmationCommandResponse
                {
                    Succeeded = false,
                    Error = "Email is already confirmed."
                };
            }

            // Generate email confirmation token
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            Console.WriteLine(token);
            
            // Create confirmation link
            var appUrl = _configuration["AppUrl"] ?? "https://Footex.AI";
            var confirmationLink = $"{appUrl}/confirm-email?userId={user.Id}&token={Uri.EscapeDataString(token)}";
            
            // Send email
            if (user.Email != null)
                await _emailService.SendEmailAsync(
                    user.Email,
                    "Confirm Your Email",
                    $"Please confirm your email by clicking <a href='{confirmationLink}'>here</a>.");

            return new ResendEmailConfirmationCommandResponse { Succeeded = true };
        }
        catch (Exception ex)
        {
            return new ResendEmailConfirmationCommandResponse
            {
                Succeeded = false,
                Error = ex.Message
            };
        }
    }
}
