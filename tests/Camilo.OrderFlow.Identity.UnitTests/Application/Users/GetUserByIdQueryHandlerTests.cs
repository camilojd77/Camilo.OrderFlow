using Camilo.OrderFlow.BuildingBlocks.SharedKernel.Exceptions;
using Camilo.OrderFlow.Modules.Identity.Application.Abstractions;
using Camilo.OrderFlow.Modules.Identity.Application.Mapping;
using Camilo.OrderFlow.Modules.Identity.Application.Queries.Users.GetUserById;
using Camilo.OrderFlow.Modules.Identity.Domain.Entities;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Camilo.OrderFlow.Identity.UnitTests.Application.Users;

public sealed class GetUserByIdQueryHandlerTests
{
    private readonly IUserRepository _repo = Substitute.For<IUserRepository>();
    private readonly UserMapper _mapper = new();

    private GetUserByIdQueryHandler CreateHandler() => new(_repo, _mapper);

    [Fact]
    public async Task Handle_ExistingUser_ReturnsDto()
    {
        var id = Guid.NewGuid();
        var user = new User(id, "test@test.com", "testuser", "hash");
        _repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(user);

        var result = await CreateHandler().Handle(new GetUserByIdQuery(id), CancellationToken.None);

        result.Id.ShouldBe(id);
        result.Email.ShouldBe("test@test.com");
    }

    [Fact]
    public async Task Handle_UnknownId_ThrowsNotFoundException()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        await Should.ThrowAsync<NotFoundException>(() =>
            CreateHandler().Handle(new GetUserByIdQuery(Guid.NewGuid()), CancellationToken.None));
    }
}
