namespace Company.Product.Modules.Identity.Application.Dtos;

public sealed record RoleDto(
    Guid Id,
    string Name,
    string? Description);
