using Application.CQRS;
using Application.CQRS.Notifications.Queries;
using Application.Interfaces;
using Application.Services;
using Domain.Interfaces;
using Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace Footex.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotificationsController(
    IUnitOfWork unitOfWork,
    IHubContext<NotificationService, INotificationService> hubContext,
    ICacheService cacheService
) : ControllerBase
{
    private readonly ICacheService _cacheService = cacheService;

    private readonly IHubContext<NotificationService, INotificationService> _hubContext =
        hubContext;

    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    [HttpGet("user/{userId}")]
    [ProducesResponseType(typeof(GetUserNotificationsQueryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize(Roles = "User,Admin")]
    public async Task<IActionResult> GetAllUserNotifications(
        string userId,
        [FromServices] IRequestHandler<GetUserNotificationsQuery, GetUserNotificationsQueryResponse> handler
    )
    {
        if (string.IsNullOrEmpty(userId))
            return BadRequest("User ID cannot be null or empty.");
        // Check if the user exists
        var user = await _unitOfWork.ApplicationUser.GetByIdAsync(userId);
        if (user == null)
            return NotFound($"User with ID '{userId}' not found.");
        var query = new GetUserNotificationsQuery { UserId = userId };
        var result = await handler.Handle(query, HttpContext.RequestAborted);

        return Ok(result);
    }

    [HttpGet("user/{userId}/unread-count")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize(Roles = "User,Admin")]
    public async Task<IActionResult> GetUnreadNotificationsCount(string userId)
    {
        return Ok(await _unitOfWork.Notifications.GetUnreadNotificationsCountAsync(userId));
    }

    [HttpPost("mark-as-read/{notificationId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize(Roles = "User,Admin")]
    public async Task<IActionResult> MarkNotificationsAsRead(string notificationId)
    {
        var notification = await _unitOfWork.Notifications.GetByIdAsync(notificationId);
        if (notification == null)
            return NotFound();
        notification.IsRead = true;
        _unitOfWork.Notifications.Update(notification);
        await _unitOfWork.SaveChangesAsync();
        return Ok();
    }

    [HttpPost("user/{userId}/mark-all-read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize(Roles = "User,Admin")]
    public async Task<IActionResult> MarkAllNotificationsAsRead(string userId)
    {
        var notifications = await _unitOfWork.Notifications.GetNotificationsAsync(userId);
        foreach (var notification in notifications)
        {
            notification.IsRead = true;
            _unitOfWork.Notifications.Update(notification);
        }

        await _unitOfWork.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{notificationId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize(Roles = "User,Admin")]
    public async Task<IActionResult> DeleteNotification(string notificationId)
    {
        var notification = await _unitOfWork.Notifications.GetByIdAsync(notificationId);
        if (notification == null)
            return NotFound();
        _unitOfWork.Notifications.Delete(notification);
        await _unitOfWork.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("user/{userId}/all")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize(Roles = "User,Admin")]
    public async Task<IActionResult> DeleteAllUserNotifications(string userId)
    {
        var notifications = await _unitOfWork.Notifications.GetNotificationsAsync(userId);
        var notificationsEnumerate = notifications as Notification[] ?? notifications.ToArray();
        if (notificationsEnumerate.Length == 0)
            return NotFound();
        foreach (var notification in notificationsEnumerate)
            _unitOfWork.Notifications.Delete(notification);
        await _unitOfWork.SaveChangesAsync();
        return NoContent();
    }
}
