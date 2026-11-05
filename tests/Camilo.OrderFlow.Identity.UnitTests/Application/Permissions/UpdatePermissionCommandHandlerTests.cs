using Camilo.OrderFlow.BuildingBlocks.SharedKernel.Exceptions;
using Camilo.OrderFlow.Modules.Identity.Application.Abstractions;
using Camilo.OrderFlow.Modules.Identity.Application.Commands.Permissions.UpdatePermission;
using Camilo.OrderFlow.Modules.Identity.Application.Mapping;
using Camilo.OrderFlow.Modules.Identity.Domain.Entities;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Camilo.OrderFlow.Identity.UnitTests.Application.Permissions;

public sealed class UpdatePermissionCommandHandlerTests
{
    private readonly IPermissionRepository _repo = Substitute.For<IPermissionRepository>();
    private readonly IIdentityExecutionContext _executionContext = Substitute.For<IIdentityExecutionContext>();
    private readonly PermissionMapper _mapper = new();

    private UpdatePermissionCommandHandler CreateHandler() => new(_repo, _executionContext, _mapper);

    [Fact]
    public async Task Handle_ExistingPermission_UpdatesNameAndReturnsDto()
    {
        var id = Guid.NewGuid();
        var permission = new Permission(id, "users.read", "Old name");
        _repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(permission);
        _executionContext.CommitAsync(Arg.Any<CancellationToken>()).Returns(1);

        var result = await CreateHandler().Handle(new UpdatePermissionCommand(id, "New name"), CancellationToken.None);

        result.Name.ShouldBe("New name");
        result.Code.ShouldBe("users.read");
        _repo.Received(1).Update(permission);
        await _executionContext.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnknownId_ThrowsNotFoundException()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Permission?)null);

        await Should.ThrowAsync<NotFoundException>(() =>
            CreateHandler().Handle(new UpdatePermissionCommand(Guid.NewGuid(), "X"), CancellationToken.None));
    }
}
