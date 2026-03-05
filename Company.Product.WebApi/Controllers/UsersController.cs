using Asp.Versioning;
using Company.Product.BuildingBlocks.SharedKernel.Exceptions;
using Company.Product.Modules.Identity.Infrastructure.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Company.Product.WebApi.Controllers;

[ApiVersion("1.0")]
[ApiController]
public sealed class UsersController : ControllerBase
{
    [AuthorizePermission("users.read")]
    [HttpGet("api/users")]
    public IActionResult GetUsers()
    {
        return Ok(new { message = "Authorized", user = User.Identity?.Name });
    }

    [HttpGet("api/test/notfound")]
    public IActionResult TestNotFound()
    => throw new NotFoundException("User", Guid.Parse("c3d4e5f6-e003-0000-0000-000000000001"));

    [HttpGet("api/test/conflict")]
    public IActionResult TestConflict()
    => throw new ConflictException("Email already registered.");
}
