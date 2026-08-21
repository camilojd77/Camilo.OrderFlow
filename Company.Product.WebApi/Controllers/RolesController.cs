using Asp.Versioning;
using Company.Product.BuildingBlocks.SharedKernel.Messaging;
using Company.Product.Modules.Identity.Application.Commands.Roles.AssignPermissions;
using Company.Product.Modules.Identity.Application.Commands.Roles.CreateRole;
using Company.Product.Modules.Identity.Application.Commands.Roles.DeleteRole;
using Company.Product.Modules.Identity.Application.Commands.Roles.UpdateRole;
using Company.Product.Modules.Identity.Application.Queries.Roles.GetRoleById;
using Company.Product.Modules.Identity.Application.Queries.Roles.ListRoles;
using Company.Product.Modules.Identity.Infrastructure.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Company.Product.WebApi.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Route("api/roles")]
public sealed class RolesController : ControllerBase
{
    private readonly ISender _sender;

    public RolesController(ISender sender) => _sender = sender;

    [HttpGet]
    [AuthorizePermission(IdentityPermissions.RolesRead)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ListRolesQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [AuthorizePermission(IdentityPermissions.RolesRead)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetRoleByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [AuthorizePermission(IdentityPermissions.RolesWrite)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateRoleCommand command,
        CancellationToken cancellationToken)
    {
        var dto = await _sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    [HttpPut("{id:guid}")]
    [AuthorizePermission(IdentityPermissions.RolesWrite)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateRoleBody body,
        CancellationToken cancellationToken)
    {
        var dto = await _sender.Send(new UpdateRoleCommand(id, body.Name, body.Description), cancellationToken);
        return Ok(dto);
    }

    [HttpDelete("{id:guid}")]
    [AuthorizePermission(IdentityPermissions.RolesWrite)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new DeleteRoleCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:guid}/permissions")]
    [AuthorizePermission(IdentityPermissions.RolesWrite)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignPermissions(
        Guid id,
        [FromBody] AssignPermissionsBody body,
        CancellationToken cancellationToken)
    {
        await _sender.Send(new AssignPermissionsToRoleCommand(id, body.PermissionIds), cancellationToken);
        return NoContent();
    }
}

public sealed record UpdateRoleBody(string Name, string? Description);
public sealed record AssignPermissionsBody(IReadOnlyList<Guid> PermissionIds);
