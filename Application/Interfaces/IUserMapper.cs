using Application.CQRS.Auth.Commands;
using Application.CQRS.Auth.Queries;
using Application.Dtos;
using Domain.Models;

namespace Application.Interfaces;

public interface IUserMapper
{
    UserDto ToProfileDto(ApplicationUser user);
    LoginUserCommand ToLoginCommand(UserLoginDto dto);
    UpdateUserCommand ToUpdateCommand(UpdateUserDto dto);
    GetUserProfileQuery ToProfileQuery(string userId);
    ForgotPasswordCommand ToForgotPasswordCommand(ForgotPasswordDto dto);
    ResetPasswordCommand ToResetPasswordCommand(ResetPasswordDto dto);
    ConfirmEmailCommand ToConfirmEmailCommand(ConfirmEmailDto dto);
    ResendEmailConfirmationCommand ToResendConfirmationCommand(ResendEmailConfirmationDto dto);
    ApplicationUser ToUserFromRegister(RegisterUserCommand request);
    RegisterUserCommand ToRegisterCommandFromDto(RegisterUserDto dto);
}
