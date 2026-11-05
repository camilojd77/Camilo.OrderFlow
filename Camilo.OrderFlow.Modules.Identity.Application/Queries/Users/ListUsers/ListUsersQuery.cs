using Camilo.OrderFlow.BuildingBlocks.SharedKernel.Messaging;
using Camilo.OrderFlow.BuildingBlocks.SharedKernel.Pagination;
using Camilo.OrderFlow.Modules.Identity.Application.Dtos;

namespace Camilo.OrderFlow.Modules.Identity.Application.Queries.Users.ListUsers;

public sealed record ListUsersQuery(int Page, int PageSize, string? Search)
    : PagedRequest(Page, PageSize), IRequest<PagedResult<UserDto>>;
