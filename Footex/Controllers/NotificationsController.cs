using Application.CQRS.Notifications.Queries;
using Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Footex.Controllers;
[ApiController]
[Route("api/[controller]")]
public class NotificationsController(IMediator mediator, IUnitOfWork unitOfWork) : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    [HttpGet("user/{userId}")]
    [ProducesResponseType(typeof(GetUserNotificationsQueryResponse),StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize(Roles = "User,Admin")]
    public async Task<IActionResult> GetAllUserNotifications(string userId)
    {
        var query = new GetUserNotificationsQuery { UserId = userId };
        var result = await mediator.Send(query);
        
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
}