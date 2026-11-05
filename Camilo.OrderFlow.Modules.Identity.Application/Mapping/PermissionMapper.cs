using Camilo.OrderFlow.Modules.Identity.Application.Dtos;
using Camilo.OrderFlow.Modules.Identity.Domain.Entities;
using Riok.Mapperly.Abstractions;

namespace Camilo.OrderFlow.Modules.Identity.Application.Mapping;

[Mapper]
internal partial class PermissionMapper
{
    public partial PermissionDto ToDto(Permission permission);
}
