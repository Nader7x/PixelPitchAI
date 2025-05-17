using Application.CQRS.Auth.Commands;
using Application.CQRS.Auth.Queries;
using Application.Dtos;
using Domain.Models;
using Riok.Mapperly.Abstractions;

namespace Application.Mappers;

[Mapper]
public partial class UserMapper
{
    // Map from ApplicationUser to UserProfileDto (or similar DTO)
    public partial UserDto ToProfileDto(ApplicationUser user);
    
    // Map from UserLoginDto to LoginUserCommand
    public partial LoginUserCommand ToLoginCommand(UserLoginDto dto);
    
    // Map from UpdateUserDto to UpdateUserCommand (if applicable)
    public partial UpdateUserCommand ToUpdateCommand(UpdateUserDto dto);
    
    // Map from GetUserProfileQuery parameter
    public partial GetUserProfileQuery ToProfileQuery(string userId);
    
    // Map from ForgotPasswordDto to ForgotPasswordCommand
    public partial ForgotPasswordCommand ToForgotPasswordCommand(ForgotPasswordDto dto);
    
    // Map from ResetPasswordDto to ResetPasswordCommand
    public partial ResetPasswordCommand ToResetPasswordCommand(ResetPasswordDto dto);
    
    // Map from ConfirmEmailDto to ConfirmEmailCommand
    public partial ConfirmEmailCommand ToConfirmEmailCommand(ConfirmEmailDto dto);
    
    // Map from ResendEmailConfirmationDto to ResendEmailConfirmationCommand
    public partial ResendEmailConfirmationCommand ToResendConfirmationCommand(ResendEmailConfirmationDto dto);

    public partial ApplicationUser ToUserFromRegister(RegisterUserCommand request);
    
    public partial RegisterUserCommand ToRegisterCommandFromDto(RegisterUserDto dto);
}
