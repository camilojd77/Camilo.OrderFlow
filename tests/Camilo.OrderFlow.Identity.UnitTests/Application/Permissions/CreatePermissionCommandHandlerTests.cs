using Camilo.OrderFlow.BuildingBlocks.SharedKernel.Exceptions;
using Camilo.OrderFlow.Modules.Identity.Application.Abstractions;
using Camilo.OrderFlow.Modules.Identity.Application.Commands.Permissions.CreatePermission;
using Camilo.OrderFlow.Modules.Identity.Application.Mapping;
using Camilo.OrderFlow.Modules.Identity.Domain.Entities;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Camilo.OrderFlow.Identity.UnitTests.Application.Permissions;

public sealed class CreatePermissionCommandHandlerTests
{
    private readonly IPermissionRepository _repo = Substitute.For<IPermissionRepository>();
    private readonly IIdentityExecutionContext _executionContext = Substitute.For<IIdentityExecutionContext>();
    private readonly PermissionMapper _mapper = new();

    private CreatePermissionCommandHandler CreateHandler() => new(_repo, _executionContext, _mapper);

    [Fact]
    public async Task Handle_UniqueCode_ReturnsPermissionDto()
    {
        _repo.CodeExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _executionContext.CommitAsync(Arg.Any<CancellationToken>()).Returns(1);

        var result = await CreateHandler().Handle(
            new CreatePermissionCommand("orders.read", "Read orders"), CancellationToken.None);

        result.ShouldNotBeNull();
        result.Code.ShouldBe("orders.read");
        result.Name.ShouldBe("Read orders");
        _repo.Received(1).Add(Arg.Any<Permission>());
        await _executionContext.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DuplicateCode_ThrowsConflictException()
    {
        _repo.CodeExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);

        await Should.ThrowAsync<ConflictException>(() =>
            CreateHandler().Handle(new CreatePermissionCommand("users.read", "X"), CancellationToken.None));

        _repo.DidNotReceive().Add(Arg.Any<Permission>());
    }
}
