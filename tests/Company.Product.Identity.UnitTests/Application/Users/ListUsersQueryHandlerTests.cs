using Company.Product.BuildingBlocks.SharedKernel.Pagination;
using Company.Product.Modules.Identity.Application.Abstractions;
using Company.Product.Modules.Identity.Application.Dtos;
using Company.Product.Modules.Identity.Application.Mapping;
using Company.Product.Modules.Identity.Application.Queries.Users.ListUsers;
using Company.Product.Modules.Identity.Domain.Entities;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Company.Product.Identity.UnitTests.Application.Users;

public sealed class ListUsersQueryHandlerTests
{
    private readonly IUserRepository _repo = Substitute.For<IUserRepository>();
    private readonly UserMapper _mapper = new();

    private ListUsersQueryHandler CreateHandler() => new(_repo, _mapper);

    [Fact]
    public async Task Handle_ReturnsPagedResult()
    {
        var users = new List<User>
        {
            new(Guid.NewGuid(), "a@test.com", "usera", "hash"),
            new(Guid.NewGuid(), "b@test.com", "userb", "hash"),
        };
        _repo.GetPagedAsync(null, 0, 20, Arg.Any<CancellationToken>()).Returns(users);
        _repo.CountAsync(null, Arg.Any<CancellationToken>()).Returns(2);

        var result = await CreateHandler().Handle(new ListUsersQuery(1, 20, null), CancellationToken.None);

        result.ShouldBeOfType<PagedResult<UserDto>>();
        result.Items.Count.ShouldBe(2);
        result.TotalCount.ShouldBe(2);
        result.Page.ShouldBe(1);
        result.PageSize.ShouldBe(20);
    }

    [Fact]
    public async Task Handle_PageSizeClamped_To100()
    {
        _repo.GetPagedAsync(null, 0, 100, Arg.Any<CancellationToken>()).Returns(new List<User>());
        _repo.CountAsync(null, Arg.Any<CancellationToken>()).Returns(0);

        var result = await CreateHandler().Handle(new ListUsersQuery(1, 9999, null), CancellationToken.None);

        result.PageSize.ShouldBe(100);
        await _repo.Received(1).GetPagedAsync(null, 0, 100, Arg.Any<CancellationToken>());
    }
}
