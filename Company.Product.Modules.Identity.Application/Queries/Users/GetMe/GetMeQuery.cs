using Company.Product.BuildingBlocks.SharedKernel.Messaging;
using Company.Product.Modules.Identity.Application.Dtos;

namespace Company.Product.Modules.Identity.Application.Queries.Users.GetMe;

public sealed record GetMeQuery : IRequest<UserDto>;
