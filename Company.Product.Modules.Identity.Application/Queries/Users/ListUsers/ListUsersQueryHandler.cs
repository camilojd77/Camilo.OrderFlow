using Company.Product.BuildingBlocks.SharedKernel.Messaging;
using Company.Product.BuildingBlocks.SharedKernel.Pagination;
using Company.Product.Modules.Identity.Application.Abstractions;
using Company.Product.Modules.Identity.Application.Dtos;
using Company.Product.Modules.Identity.Application.Mapping;

namespace Company.Product.Modules.Identity.Application.Queries.Users.ListUsers;

public sealed class ListUsersQueryHandler : IRequestHandler<ListUsersQuery, PagedResult<UserDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly UserMapper _userMapper;

    public ListUsersQueryHandler(IUserRepository userRepository, UserMapper userMapper)
    {
        _userRepository = userRepository;
        _userMapper = userMapper;
    }

    public async Task<PagedResult<UserDto>> Handle(ListUsersQuery request, CancellationToken ct)
    {
        var items = await _userRepository.GetPagedAsync(request.Search, request.Skip, request.SafePageSize, ct);
        var total = await _userRepository.CountAsync(request.Search, ct);

        return new PagedResult<UserDto>(
            items.Select(_userMapper.ToDto).ToList(),
            total,
            request.SafePage,
            request.SafePageSize);
    }
}
