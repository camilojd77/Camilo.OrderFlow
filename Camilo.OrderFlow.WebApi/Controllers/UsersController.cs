using Asp.Versioning;
using Camilo.OrderFlow.BuildingBlocks.SharedKernel.Messaging;
using Camilo.OrderFlow.Modules.Identity.Application.Commands.Users.ActivateUser;
using Camilo.OrderFlow.Modules.Identity.Application.Commands.Users.AssignRolesToUser;
using Camilo.OrderFlow.Modules.Identity.Application.Commands.Users.CreateUser;
using Camilo.OrderFlow.Modules.Identity.Application.Commands.Users.DeactivateUser;
using Camilo.OrderFlow.Modules.Identity.Application.Commands.Users.UpdateUserProfile;
using Camilo.OrderFlow.Modules.Identity.Application.Queries.Users.GetMe;
using Camilo.OrderFlow.Modules.Identity.Application.Queries.Users.GetMyPermissions;
using Camilo.OrderFlow.Modules.Identity.Application.Queries.Users.GetUserById;
using Camilo.OrderFlow.Modules.Identity.Application.Queries.Users.ListUsers;
using Camilo.OrderFlow.Modules.Identity.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Camilo.OrderFlow.WebApi.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Route("api/users")]
public sealed class UsersController : ControllerBase
{
    private readonly ISender _sender;

    public UsersController(ISender sender) => _sender = sender;

    [HttpGet]
    [AuthorizePermission(IdentityPermissions.UsersRead)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new ListUsersQuery(page, pageSize, search), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [AuthorizePermission(IdentityPermissions.UsersRead)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetUserByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [AuthorizePermission(IdentityPermissions.UsersWrite)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateUserCommand command,
        CancellationToken cancellationToken)
    {
        var dto = await _sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    [HttpPut("{id:guid}/profile")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProfile(
        Guid id,
        [FromBody] UpdateProfileBody body,
        CancellationToken cancellationToken)
    {
        var dto = await _sender.Send(
            new UpdateUserProfileCommand(id, body.FirstName, body.LastName, body.PhoneNumber, body.AvatarUrl),
            cancellationToken);
        return Ok(dto);
    }

    [HttpPost("{id:guid}/deactivate")]
    [AuthorizePermission(IdentityPermissions.UsersWrite)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new DeactivateUserCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/activate")]
    [AuthorizePermission(IdentityPermissions.UsersWrite)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new ActivateUserCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:guid}/roles")]
    [AuthorizePermission(IdentityPermissions.UsersWrite)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignRoles(
        Guid id,
        [FromBody] AssignRolesBody body,
        CancellationToken cancellationToken)
    {
        await _sender.Send(new AssignRolesToUserCommand(id, body.RoleIds), cancellationToken);
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMe(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetMeQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("me/permissions")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyPermissions(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetMyPermissionsQuery(), cancellationToken);
        return Ok(result);
    }
}

public sealed record UpdateProfileBody(
    string FirstName,
    string LastName,
    string? PhoneNumber,
    string? AvatarUrl);

public sealed record AssignRolesBody(IReadOnlyList<Guid> RoleIds);
