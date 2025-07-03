using Application.Dtos;
using Domain.Repositories;
using MediatR;

namespace Application.CQRS.Notifications.Queries;

public class GetUserNotificationsQuery : IRequest<GetUserNotificationsQueryResponse>
{
    public required string UserId { get; set; }
}

public class GetUserNotificationsQueryResponse
{
    public bool Succeeded { get; set; }
    public string? Error { get; set; }
    public IReadOnlyList<NotificationDto>? Notifications { get; set; }
}

public class GetUserNotificationsQueryHandler(INotificationRepository notificationRepository)
    : IRequestHandler<GetUserNotificationsQuery, GetUserNotificationsQueryResponse>
{
    public async Task<GetUserNotificationsQueryResponse> Handle(
        GetUserNotificationsQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var notifications = await notificationRepository.GetNotificationsAsync(request.UserId);
            var notificationDtos = notifications
                .Select(n => new NotificationDto
                {
                    Id = n.Id,
                    Content = n.Content,
                    Time = n.Time,
                    IsRead = n.IsRead,
                    Type = n.Type,
                })
                .ToList();

            return new GetUserNotificationsQueryResponse
            {
                Succeeded = true,
                Notifications = notificationDtos,
            };
        }
        catch (Exception ex)
        {
            return new GetUserNotificationsQueryResponse { Succeeded = false, Error = ex.Message };
        }
    }
}
