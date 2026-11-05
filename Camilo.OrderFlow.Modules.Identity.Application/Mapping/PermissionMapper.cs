using Camilo.OrderFlow.Modules.Identity.Application.Dtos;
using Camilo.OrderFlow.Modules.Identity.Domain.Entities;
using Riok.Mapperly.Abstractions;

namespace Camilo.OrderFlow.Modules.Identity.Application.Mapping;

[Mapper]
public partial class PermissionMapper
{
    public partial PermissionDto ToDto(Permission permission);
}
