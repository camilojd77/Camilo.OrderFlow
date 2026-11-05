using Camilo.OrderFlow.Modules.Identity.Application.Abstractions;
using Camilo.OrderFlow.Modules.Identity.Application.Mapping;
using Camilo.OrderFlow.Modules.Identity.Application.Queries.Permissions.ListPermissions;
using Camilo.OrderFlow.Modules.Identity.Domain.Entities;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Camilo.OrderFlow.Identity.UnitTests.Application.Permissions;

public sealed class ListPermissionsQueryHandlerTests
{
    private readonly IPermissionRepository _repo = Substitute.For<IPermissionRepository>();
    private readonly PermissionMapper _mapper = new();

    private ListPermissionsQueryHandler CreateHandler() => new(_repo, _mapper);

    [Fact]
    public async Task Handle_PermissionsExist_ReturnsMappedDtos()
    {
        var permissions = new List<Permission>
        {
            new(Guid.NewGuid(), "users.read",  "Read users"),
            new(Guid.NewGuid(), "users.write", "Write users"),
        };
        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(permissions);

        var result = await CreateHandler().Handle(new ListPermissionsQuery(), CancellationToken.None);

        result.Count.ShouldBe(2);
        result[0].Code.ShouldBe("users.read");
        result[1].Code.ShouldBe("users.write");
    }

    [Fact]
    public async Task Handle_NoPermissions_ReturnsEmptyList()
    {
        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Permission>());

        var result = await CreateHandler().Handle(new ListPermissionsQuery(), CancellationToken.None);

        result.ShouldBeEmpty();
    }
}
