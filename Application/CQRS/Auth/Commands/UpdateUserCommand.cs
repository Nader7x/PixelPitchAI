using Domain.Interfaces;
using Domain.Models;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Application.CQRS.Auth.Commands;

public class UpdateUserCommand : IRequest<UpdateUserCommandResponse>
{
    public string Id { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? CurrentPassword { get; set; }
    public string? NewPassword { get; set; }
    public string? ImageUrl { get; set; }

    public int? Age { get; set; }
    public string? Gender { get; set; }
}

public class UpdateUserCommandResponse
{
    public bool Succeeded { get; set; }
    public bool NotFound { get; set; }
    public string Error { get; set; }
    public string? ImageUrl { get; set; }
}

public class UpdateUserCommandHandler(IUnitOfWork unitOfWork, UserManager<ApplicationUser> usermanager)
    : IRequestHandler<UpdateUserCommand, UpdateUserCommandResponse>
{
    private readonly UserManager<ApplicationUser> _usermanager = usermanager;

    public async Task<UpdateUserCommandResponse> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await unitOfWork.ApplicationUser.GetByIdAsync(request.Id);
            if (user == null)
                return new UpdateUserCommandResponse
                {
                    Succeeded = false,
                    NotFound = true,
                    Error = $"User with ID {request.Id} not found"
                };
            if (!string.IsNullOrEmpty(request.CurrentPassword) && !string.IsNullOrEmpty(request.NewPassword))
                await _usermanager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);

            if (!string.IsNullOrEmpty(request.FirstName)) user.FirstName = request.FirstName;
            if (!string.IsNullOrEmpty(request.LastName)) user.LastName = request.LastName;
            if (!string.IsNullOrEmpty(request.Email)) user.Email = request.Email;
            if (request.Age != 0)
                if (request.Age != null)
                    user.Age = request.Age.Value;
            if (!string.IsNullOrEmpty(request.PhoneNumber)) user.PhoneNumber = request.PhoneNumber;
            if (!string.IsNullOrEmpty(request.Gender)) user.Gender = request.Gender;
            if (!string.IsNullOrEmpty(request.ImageUrl)) user.ImageUrl = request.ImageUrl;

            await unitOfWork.SaveChangesAsync(cancellationToken);

            if (user.ImageUrl != null)
                return new UpdateUserCommandResponse
                {
                    Succeeded = true,
                    ImageUrl = user.ImageUrl
                };
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