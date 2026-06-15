using Domain.Models;
using Domain.Repositories;
using Application.CQRS;

namespace Application.CQRS.Notifications.Commands;

public class CreateNotificationCommand : IRequest<CreateNotificationCommandResponse>
{
    public required Notification Notification { get; init; }
}

public class CreateNotificationCommandResponse
{
    public bool Succeeded { get; init; }
    public Notification? Notification { get; init; }
    public string? Error { get; init; }
}

public class CreateNotificationCommandHandler(INotificationRepository notificationRepository)
    : IRequestHandler<CreateNotificationCommand, CreateNotificationCommandResponse>
{
    public async Task<CreateNotificationCommandResponse> Handle(
        CreateNotificationCommand request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var createdNotification = await notificationRepository.AddAsync(request.Notification);
            return new CreateNotificationCommandResponse
            {
                Succeeded = true,
                Notification = createdNotification.Entity,
            };
        }
        catch (Exception ex)
        {
            return new CreateNotificationCommandResponse { Succeeded = false, Error = ex.Message };
        }
    }
}
