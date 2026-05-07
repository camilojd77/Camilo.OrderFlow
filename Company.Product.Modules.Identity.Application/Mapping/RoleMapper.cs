using Company.Product.Modules.Identity.Application.Dtos;
using Company.Product.Modules.Identity.Domain.Entities;
using Riok.Mapperly.Abstractions;

namespace Company.Product.Modules.Identity.Application.Mapping;

[Mapper]
public partial class RoleMapper
{
    public partial RoleDto ToDto(Role role);
}
