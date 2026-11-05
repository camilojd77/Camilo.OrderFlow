namespace Camilo.OrderFlow.Modules.Identity.Application.Dtos;

public sealed record PermissionDto(
    Guid Id,
    string Code,
    string Name);
