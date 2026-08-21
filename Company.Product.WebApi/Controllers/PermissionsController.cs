using Asp.Versioning;
using Company.Product.BuildingBlocks.SharedKernel.Messaging;
using Company.Product.Modules.Identity.Application.Commands.Permissions.CreatePermission;
using Company.Product.Modules.Identity.Application.Commands.Permissions.DeletePermission;
using Company.Product.Modules.Identity.Application.Commands.Permissions.UpdatePermission;
using Company.Product.Modules.Identity.Application.Queries.Permissions.ListPermissions;
using Company.Product.Modules.Identity.Infrastructure.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Company.Product.WebApi.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Route("api/permissions")]
public sealed class PermissionsController : ControllerBase
{
    private readonly ISender _sender;

    public PermissionsController(ISender sender) => _sender = sender;

    [HttpGet]
    [AuthorizePermission(IdentityPermissions.PermissionsRead)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ListPermissionsQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [AuthorizePermission(IdentityPermissions.PermissionsWrite)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreatePermissionCommand command,
        CancellationToken cancellationToken)
    {
        var dto = await _sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetAll), null, dto);
    }

    [HttpPut("{id:guid}")]
    [AuthorizePermission(IdentityPermissions.PermissionsWrite)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdatePermissionBody body,
        CancellationToken cancellationToken)
    {
        var dto = await _sender.Send(new UpdatePermissionCommand(id, body.Name), cancellationToken);
        return Ok(dto);
    }

    [HttpDelete("{id:guid}")]
    [AuthorizePermission(IdentityPermissions.PermissionsWrite)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new DeletePermissionCommand(id), cancellationToken);
        return NoContent();
    }
}

public sealed record UpdatePermissionBody(string Name);
