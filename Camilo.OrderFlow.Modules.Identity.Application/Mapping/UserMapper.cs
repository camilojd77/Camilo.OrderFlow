using Camilo.OrderFlow.Modules.Identity.Application.Dtos;
using Camilo.OrderFlow.Modules.Identity.Domain.Entities;
using Camilo.OrderFlow.Modules.Identity.Domain.ValueObjects;
using Riok.Mapperly.Abstractions;

namespace Camilo.OrderFlow.Modules.Identity.Application.Mapping;

[Mapper]
public partial class UserMapper
{
    public partial UserDto ToDto(User user);
    public partial UserProfileDto ToDto(UserProfile profile);
}
