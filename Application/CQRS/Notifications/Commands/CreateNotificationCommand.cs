using Domain.Models;
using Domain.Repositories;
using MediatR;

namespace Application.CQRS.Notifications.Commands;

public class CreateNotificationCommand : IRequest<CreateNotificationCommandResponse>
{
    public required Notification Notification { get; set; }
}

public class CreateNotificationCommandResponse
{
    public bool Succeeded { get; set; }
    public Notification? Notification { get; set; }
    public string? Error { get; set; }
}

public class CreateNotificationCommandHandler(INotificationRepository notificationRepository)
    : IRequestHandler<CreateNotificationCommand, CreateNotificationCommandResponse>
{
    public async Task<CreateNotificationCommandResponse> Handle(CreateNotificationCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var createdNotification = await notificationRepository.AddAsync(request.Notification);
            return new CreateNotificationCommandResponse
            {
                Succeeded = true,
                Notification = createdNotification.Entity
            };
        }
        catch (Exception ex)
        {
            return new CreateNotificationCommandResponse
            {
                Succeeded = false,
                Error = ex.Message
            };
        }
    }
}