using Company.Product.BuildingBlocks.SharedKernel.Messaging;
using Company.Product.BuildingBlocks.SharedKernel.Pagination;
using Company.Product.Modules.Identity.Application.Dtos;

namespace Company.Product.Modules.Identity.Application.Queries.Users.ListUsers;

public sealed record ListUsersQuery(int Page, int PageSize, string? Search)
    : PagedRequest(Page, PageSize), IRequest<PagedResult<UserDto>>;
