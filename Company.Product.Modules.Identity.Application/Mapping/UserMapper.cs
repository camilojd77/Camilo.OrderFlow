using Company.Product.Modules.Identity.Application.Dtos;
using Company.Product.Modules.Identity.Domain.Entities;
using Company.Product.Modules.Identity.Domain.ValueObjects;
using Riok.Mapperly.Abstractions;

namespace Company.Product.Modules.Identity.Application.Mapping;

[Mapper]
public partial class UserMapper
{
    public partial UserDto ToDto(User user);
    public partial UserProfileDto ToDto(UserProfile profile);
}
