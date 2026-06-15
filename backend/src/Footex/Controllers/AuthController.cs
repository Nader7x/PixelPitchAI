using System.Web;
using Application.CQRS.Auth.Commands;
using Application.CQRS.Auth.Queries;
using Application.Dtos;
using Application.CQRS;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Footex.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(
    IUserMapper userMapper,
    IFileStorageService blobStorageService
) : ControllerBase
{
    private readonly IFileStorageService _blobStorageService = blobStorageService;
    private readonly IUserMapper _userMapper = userMapper;
    private readonly string CONTAINER_NAME = "users";

    [HttpPost("register")]
    public async Task<ActionResult<RegisterUserCommandResponse>> Register(
        [FromForm] RegisterUserDto dto,
        [FromServices] IRequestHandler<RegisterUserCommand, RegisterUserCommandResponse> handler
    )
    {
        var command = _userMapper.ToRegisterCommandFromDto(dto);
        command.ImageUrl =
            dto.Image != null
                ? await _blobStorageService.UploadImageAsync(dto.Image, CONTAINER_NAME)
                : null;
        var result = await handler.Handle(command, HttpContext.RequestAborted);

        if (!result.Succeeded)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginUserCommandResponse>> Login(
        [FromBody] UserLoginDto dto,
        [FromServices] IRequestHandler<LoginUserCommand, LoginUserCommandResponse> handler
    )
    {
        var command = new LoginUserCommand
        {
            Email = dto.Email,
            Password = dto.Password,
            // Get client IP address
            IpAddress = GetIpAddress(),
        };

        var result = await handler.Handle(command, HttpContext.RequestAborted);

        if (!result.Succeeded)
            return BadRequest(result);

        // Set refresh token in cookie
        if (result.RefreshToken != null)
            SetRefreshTokenCookie(result.RefreshToken);

        return Ok(result);
    }

    [HttpPost("refresh-token")]
    [Authorize]
    public async Task<ActionResult<RefreshTokenCommandResponse>> RefreshToken(
        [FromServices] IRequestHandler<RefreshTokenCommand, RefreshTokenCommandResponse> handler
    )
    {
        var refreshToken = Request.Cookies["refreshToken"];

        if (string.IsNullOrEmpty(refreshToken))
            return BadRequest(new { message = "Refresh token is required" });

        var command = new RefreshTokenCommand
        {
            RefreshToken = refreshToken,
            IpAddress = GetIpAddress(),
        };

        var result = await handler.Handle(command, HttpContext.RequestAborted);

        if (!result.Succeeded)
            return BadRequest(result);

        // Set refresh token in cookie
        SetRefreshTokenCookie(result.RefreshToken);

        return Ok(result);
    }

    [HttpPost("forgot-password")]
    [ProducesResponseType(typeof(ForgotPasswordCommandResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ForgotPasswordCommandResponse>> ForgotPassword(
        [FromBody] ForgotPasswordDto dto,
        [FromServices] IRequestHandler<ForgotPasswordCommand, ForgotPasswordCommandResponse> handler
    )
    {
        var command = new ForgotPasswordCommand { Email = dto.Email };

        var result = await handler.Handle(command, HttpContext.RequestAborted);

        if (!result.Succeeded)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("reset-password")]
    [ProducesResponseType(typeof(ResetPasswordCommandResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ResetPasswordCommandResponse>> ResetPassword(
        [FromBody] ResetPasswordDto dto,
        [FromQuery] string email,
        [FromQuery] string token,
        [FromServices] IRequestHandler<ResetPasswordCommand, ResetPasswordCommandResponse> handler
    )
    {
        var command = new ResetPasswordCommand
        {
            Email = HttpUtility.UrlDecode(email),
            Token = HttpUtility.UrlDecode(token),
            NewPassword = dto.NewPassword,
        };

        var result = await handler.Handle(command, HttpContext.RequestAborted);

        if (!result.Succeeded)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("confirm-email")]
    [ProducesResponseType(typeof(ConfirmEmailCommandResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ConfirmEmailCommandResponse>> ConfirmEmail(
        [FromQuery] string userId,
        [FromQuery] string token,
        [FromServices] IRequestHandler<ConfirmEmailCommand, ConfirmEmailCommandResponse> handler
    )
    {
        var command = new ConfirmEmailCommand { UserId = userId, Token = token };

        var result = await handler.Handle(command, HttpContext.RequestAborted);

        if (!result.Succeeded)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("resend-email-confirmation")]
    [ProducesResponseType(typeof(ResendEmailConfirmationCommandResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ResendEmailConfirmationCommandResponse>> ResendEmailConfirmation(
        [FromBody] ResendEmailConfirmationDto dto,
        [FromServices] IRequestHandler<ResendEmailConfirmationCommand, ResendEmailConfirmationCommandResponse> handler
    )
    {
        var command = new ResendEmailConfirmationCommand { Email = dto.Email };

        var result = await handler.Handle(command, HttpContext.RequestAborted);

        if (!result.Succeeded)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("revoke-token")]
    [Authorize]
    public async Task<IActionResult> RevokeToken(
        [FromBody] RevokeTokenCommand command,
        [FromServices] IRequestHandler<RevokeTokenCommand, RevokeTokenCommandResponse> handler
    )
    {
        // Accept refresh token from request body or cookie
        var refreshToken = command.RefreshToken ?? Request.Cookies["refreshToken"];

        if (string.IsNullOrEmpty(refreshToken))
            return BadRequest(new { message = "Refresh token is required" });

        command.RefreshToken = refreshToken;
        command.IpAddress = GetIpAddress();

        var result = await handler.Handle(command, HttpContext.RequestAborted);

        if (!result.Succeeded)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpGet("profile")]
    [Authorize]
    public async Task<ActionResult<GetUserProfileQueryResponse>> GetProfile(
        [FromServices] IRequestHandler<GetUserProfileQuery, GetUserProfileQueryResponse> handler
    )
    {
        var userId = User.FindFirst(
            "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"
        )?.Value;
        Console.WriteLine(userId);

        if (string.IsNullOrEmpty(userId))
            return BadRequest(new { message = "User ID not found in token" });

        var query = new GetUserProfileQuery { UserId = userId };
        var result = await handler.Handle(query, HttpContext.RequestAborted);

        if (!result.Succeeded)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpGet("profile/{id}")]
    [Authorize]
    public async Task<ActionResult<GetUserProfileQueryResponse>> GetProfileById(
        string id,
        [FromServices] IRequestHandler<GetUserProfileQuery, GetUserProfileQueryResponse> handler
    )
    {
        if (string.IsNullOrEmpty(id))
            return BadRequest(new { message = "User ID not found in token" });

        var query = new GetUserProfileQuery { UserId = id };
        var result = await handler.Handle(query, HttpContext.RequestAborted);

        if (!result.Succeeded)
            return BadRequest(result);

        return Ok(result);
    }

    // Helper methods
    private string GetIpAddress()
    {
        // After ForwardedHeadersMiddleware is configured and used,
        // HttpContext.Connection.RemoteIpAddress should be the client's actual IP.
        var ipAddress = HttpContext.Connection.RemoteIpAddress;

        if (ipAddress != null)
            // Map to IPv4 if it's an IPv4-mapped IPv6 address
            return ipAddress.IsIPv4MappedToIPv6
                ? ipAddress.MapToIPv4().ToString()
                : ipAddress.ToString();

        return "unknown";
    }

    private void SetRefreshTokenCookie(string token)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Expires = DateTime.UtcNow.AddDays(7),
            SameSite = SameSiteMode.Strict,
            Secure = true, // Set to true in production
        };

        Response.Cookies.Append("refreshToken", token, cookieOptions);
    }

    /// <summary>
    ///     Public endpoints don't need the [Authorize] attribute
    /// </summary>
    /// <summary>
    ///     Manual refresh token endpoint for Swagger testing
    /// </summary>
    /// <remarks>
    ///     This endpoint allows manual entry of a refresh token for testing in Swagger UI
    /// </remarks>
    /// <param name="token">The refresh token string</param>
    /// <returns>New access token and refresh token</returns>
    [HttpPost("manual-refresh")]
    [ProducesResponseType(typeof(RefreshTokenCommandResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RefreshTokenCommandResponse>> ManualRefreshToken(
        [FromBody] string token,
        [FromServices] IRequestHandler<RefreshTokenCommand, RefreshTokenCommandResponse> handler
    )
    {
        if (string.IsNullOrEmpty(token))
            return BadRequest(new { message = "Refresh token is required" });

        var command = new RefreshTokenCommand { RefreshToken = token, IpAddress = GetIpAddress() };

        var result = await handler.Handle(command, HttpContext.RequestAborted);

        if (!result.Succeeded)
            return BadRequest(result);

        // Set refresh token in cookie
        SetRefreshTokenCookie(result.RefreshToken);

        return Ok(result);
    }

    [HttpPut("update")]
    [Authorize]
    [ProducesResponseType(typeof(UpdateUserCommandResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UpdateUserCommandResponse>> UpdateUser(
        [FromForm] UpdateUserDto dto,
        [FromServices] IRequestHandler<GetUserProfileQuery, GetUserProfileQueryResponse> profileHandler,
        [FromServices] IRequestHandler<UpdateUserCommand, UpdateUserCommandResponse> updateHandler
    )
    {
        var command = _userMapper.ToUpdateCommand(dto);
        if (dto.Image != null)
        {
            // delete old image if it exists
            var existingUser = await profileHandler.Handle(new GetUserProfileQuery { UserId = dto.Id }, HttpContext.RequestAborted);
            if (!existingUser.Succeeded == false && !string.IsNullOrEmpty(existingUser.ImageUrl))
                await _blobStorageService.DeleteImageAsync(existingUser.ImageUrl, CONTAINER_NAME);
            // upload new image
            command.ImageUrl = await _blobStorageService.UploadImageAsync(
                dto.Image,
                CONTAINER_NAME
            );
        }

        var result = await updateHandler.Handle(command, HttpContext.RequestAborted);

        if (!result.Succeeded)
            return BadRequest(result);

        return Ok(result);
    }
}
