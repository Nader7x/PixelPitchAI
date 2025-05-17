using Application.Mappers;
using Domain.Models;
using Microsoft.AspNetCore.Identity;

namespace Application.CQRS.Auth.Commands;

using MediatR;
using Domain.Interfaces;

public class UpdateUserCommand : IRequest<UpdateUserCommandResponse>
{
    public string Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string PhoneNumber { get; set; }
    public string? CurrentPassword { get; set; }
    public string? NewPassword { get; set; }
    public string? ImageUrl { get; set; }

    public int Age { get; set; }
}

public class UpdateUserCommandResponse
{
    public bool Succeeded { get; set; }
    public bool NotFound { get; set; }
    public string Error { get; set; }
}

public class UpdateUserCommandHandler(IUnitOfWork unitOfWork, UserManager<ApplicationUser> usermanager)
    : IRequestHandler<UpdateUserCommand, UpdateUserCommandResponse>
{
    private readonly UserManager<ApplicationUser> _usermanager = usermanager;

    public async Task<UpdateUserCommandResponse> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await unitOfWork.ApplicationUserRepository.GetByIdAsync(request.Id);
            if (user == null)
            {
                return new UpdateUserCommandResponse
                {
                    Succeeded = false,
                    NotFound = true,
                    Error = $"User with ID {request.Id} not found"
                };
            }
            if (!string.IsNullOrEmpty(request.CurrentPassword)&&!string.IsNullOrEmpty(request.NewPassword))
            {
                await _usermanager.ChangePasswordAsync(user,request.CurrentPassword, request.NewPassword);
            }

            user.FirstName = request.FirstName;
            user.LastName = request.LastName;
            user.Email = request.Email;
            user.PhoneNumber = request.PhoneNumber;
            user.Age = request.Age;
            user.ImageUrl = request.ImageUrl;
            

            unitOfWork.ApplicationUserRepository.UpdateAsync(user);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new UpdateUserCommandResponse
            {
                Succeeded = true
            };
        }
        catch (Exception ex)
        {
            return new UpdateUserCommandResponse
            {
                Succeeded = false,
                Error = ex.Message
            };
        }
    }
}