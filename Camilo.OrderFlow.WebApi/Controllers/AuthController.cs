using Asp.Versioning;
using Camilo.OrderFlow.BuildingBlocks.SharedKernel.Messaging;
using Camilo.OrderFlow.Modules.Identity.Application.Commands.ChangePassword;
using Camilo.OrderFlow.Modules.Identity.Application.Commands.RegisterUser;
using Camilo.OrderFlow.Modules.Identity.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Camilo.OrderFlow.WebApi.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Route("api/auth")]
[EnableRateLimiting("auth")]
public sealed class AuthController : ControllerBase
{
    private const string RefreshTokenCookie = "refresh_token";

    private readonly IAuthService _authService;
    private readonly ISender _sender;

    public AuthController(IAuthService authService, ISender sender)
    {
        _authService = authService;
        _sender = sender;
    }

    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterUserCommand command,
        CancellationToken cancellationToken)
    {
        var dto = await _sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(Register), new { dto.Id }, dto);
    }

    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var result = await _authService.LoginAsync(request, clientIp, cancellationToken);

        if (result is null)
            return Unauthorized();

        AppendRefreshTokenCookie(result.RawRefreshToken);

        return Ok(new { result.AccessToken, result.ExpiresUtc, result.User });
    }

    [HttpPost("refresh")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        if (!Request.Cookies.TryGetValue(RefreshTokenCookie, out var rawToken) || string.IsNullOrWhiteSpace(rawToken))
            return Unauthorized();

        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var result = await _authService.RefreshAsync(rawToken, clientIp, cancellationToken);

        if (result is null)
            return Unauthorized();

        AppendRefreshTokenCookie(result.RawRefreshToken);

        return Ok(new { result.AccessToken, result.ExpiresUtc });
    }

    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        if (Request.Cookies.TryGetValue(RefreshTokenCookie, out var rawToken) && !string.IsNullOrWhiteSpace(rawToken))
            await _authService.LogoutAsync(rawToken, cancellationToken);

        Response.Cookies.Delete(RefreshTokenCookie);
        return NoContent();
    }

    [Authorize]
    [HttpPost("change-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordCommand command,
        CancellationToken cancellationToken)
    {
        await _sender.Send(command, cancellationToken);
        return NoContent();
    }

    private void AppendRefreshTokenCookie(string rawToken)
    {
        Response.Cookies.Append(RefreshTokenCookie, rawToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddDays(30),
        });
    }
}
