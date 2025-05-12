using Application.CQRS.Auth.Commands;
using Application.CQRS.Auth.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Footex.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IMediator mediator) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<RegisterUserCommandResponse>> Register([FromBody] RegisterUserCommand command)
    {
        var result = await mediator.Send(command);
        
        if (!result.Succeeded)
            return BadRequest(result);
            
        return Ok(result);
    }
    
    [HttpPost("login")]
    public async Task<ActionResult<LoginUserCommandResponse>> Login([FromBody] LoginUserCommand command)
    {
        // Get client IP address
        command.IpAddress = GetIpAddress();
        
        var result = await mediator.Send(command);
        
        if (!result.Succeeded)
            return BadRequest(result);
            
        // Set refresh token in cookie
        SetRefreshTokenCookie(result.RefreshToken);
            
        return Ok(result);
    }
    
    [HttpPost("refresh-token")]
    public async Task<ActionResult<RefreshTokenCommandResponse>> RefreshToken()
    {
        var refreshToken = Request.Cookies["refreshToken"];
        
        if (string.IsNullOrEmpty(refreshToken))
            return BadRequest(new { message = "Refresh token is required" });
            
        var command = new RefreshTokenCommand
        {
            RefreshToken = refreshToken,
            IpAddress = GetIpAddress()
        };
        
        var result = await mediator.Send(command);
        
        if (!result.Succeeded)
            return BadRequest(result);
            
        // Set refresh token in cookie
        SetRefreshTokenCookie(result.RefreshToken);
            
        return Ok(result);
    }
    
    [HttpPost("revoke-token")]
    [Authorize]
    public async Task<IActionResult> RevokeToken([FromBody] RevokeTokenCommand command)
    {
        // Accept refresh token from request body or cookie
        var refreshToken = command.RefreshToken ?? Request.Cookies["refreshToken"];
        
        if (string.IsNullOrEmpty(refreshToken))
            return BadRequest(new { message = "Refresh token is required" });
            
        command.RefreshToken = refreshToken;
        command.IpAddress = GetIpAddress();
        
        var result = await mediator.Send(command);
        
        if (!result.Succeeded)
            return BadRequest(result);
            
        return Ok(result);
    }
    
    [HttpGet("profile")]
    [Authorize]
    public async Task<ActionResult<GetUserProfileQueryResponse>> GetProfile()
    {
        var userId = User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
        
        if (string.IsNullOrEmpty(userId))
            return BadRequest(new { message = "User ID not found in token" });
            
        var query = new GetUserProfileQuery { UserId = userId };
        var result = await mediator.Send(query);
        
        if (!result.Succeeded)
            return BadRequest(result);
            
        return Ok(result);
    }
    
    // Helper methods
    private string GetIpAddress()
    {
        if (Request.Headers.ContainsKey("X-Forwarded-For"))
            return Request.Headers["X-Forwarded-For"];
            
        return HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "unknown";
    }
    
    private void SetRefreshTokenCookie(string token)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Expires = System.DateTime.UtcNow.AddDays(7),
            SameSite = SameSiteMode.Strict,
            Secure = true // Set to true in production
        };
        
        Response.Cookies.Append("refreshToken", token, cookieOptions);
    }
}
