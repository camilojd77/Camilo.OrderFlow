using Company.Product.Modules.Identity.Application.Abstractions;
using Company.Product.Modules.Identity.Domain.Entities;
using Company.Product.Modules.Identity.Domain.ValueObjects;
using Company.Product.Modules.Identity.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace Company.Product.Modules.Identity.Infrastructure.Persistence.Seed;

public sealed class IdentityDataSeeder
{
    private static readonly Guid UsersReadPermissionId = new("a1b2c3d4-e001-0000-0000-000000000001");
    private static readonly Guid UsersWritePermissionId = new("a1b2c3d4-e001-0000-0000-000000000002");
    private static readonly Guid RolesReadPermissionId = new("a1b2c3d4-e001-0000-0000-000000000003");
    private static readonly Guid RolesWritePermissionId = new("a1b2c3d4-e001-0000-0000-000000000004");
    private static readonly Guid PermissionsReadPermissionId = new("a1b2c3d4-e001-0000-0000-000000000005");
    private static readonly Guid PermissionsWritePermissionId = new("a1b2c3d4-e001-0000-0000-000000000006");
    private static readonly Guid AdminRoleId = new("b2c3d4e5-e002-0000-0000-000000000001");
    private static readonly Guid AdminUserId = new("c3d4e5f6-e003-0000-0000-000000000001");

    private readonly IdentityDbContext _context;
    private readonly IHostEnvironment _environment;
    private readonly IPasswordHasher _passwordHasher;

    public IdentityDataSeeder(
        IdentityDbContext context,
        IHostEnvironment environment,
        IPasswordHasher passwordHasher)
    {
        _context = context;
        _environment = environment;
        _passwordHasher = passwordHasher;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var permissionIds = await SeedPermissionsAsync(cancellationToken);
        var roleId = await SeedRoleAsync(cancellationToken);
        await SeedRolePermissionsAsync(roleId, permissionIds, cancellationToken);
        var userId = await SeedAdminUserAsync(cancellationToken);
        await SeedUserRoleAsync(userId, roleId, cancellationToken);
    }

    private async Task<IReadOnlyDictionary<string, Guid>> SeedPermissionsAsync(
        CancellationToken cancellationToken)
    {
        var definitions = new (Guid Id, string Code, string Name)[]
        {
            (UsersReadPermissionId,        "users.read",        "Read users"),
            (UsersWritePermissionId,       "users.write",       "Write users"),
            (RolesReadPermissionId,        "roles.read",        "Read roles"),
            (RolesWritePermissionId,       "roles.write",       "Write roles"),
            (PermissionsReadPermissionId,  "permissions.read",  "Read permissions"),
            (PermissionsWritePermissionId, "permissions.write", "Write permissions"),
        };

        var codes = definitions.Select(d => d.Code).ToArray();
        var existing = await _context.Permissions
            .Where(p => codes.Contains(p.Code))
            .ToDictionaryAsync(p => p.Code, p => p.Id, cancellationToken);

        foreach (var (id, code, name) in definitions)
        {
            if (!existing.ContainsKey(code))
            {
                _context.Permissions.Add(new Permission(id, code, name));
                existing[code] = id;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return existing;
    }

    private async Task<Guid> SeedRoleAsync(CancellationToken cancellationToken)
    {
        var roleId = await _context.Roles
            .Where(r => r.Name == "Admin")
            .Select(r => r.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (roleId == Guid.Empty)
        {
            _context.Roles.Add(new Role(AdminRoleId, "Admin", "System administrator"));
            await _context.SaveChangesAsync(cancellationToken);
            roleId = AdminRoleId;
        }

        return roleId;
    }

    private async Task SeedRolePermissionsAsync(
        Guid roleId,
        IReadOnlyDictionary<string, Guid> permissionIds,
        CancellationToken cancellationToken)
    {
        var existing = (await _context.RolePermissions
            .Where(rp => rp.RoleId == roleId)
            .Select(rp => rp.PermissionId)
            .ToListAsync(cancellationToken))
            .ToHashSet();

        foreach (var permissionId in permissionIds.Values)
        {
            if (!existing.Contains(permissionId))
                _context.RolePermissions.Add(new RolePermission(roleId, permissionId));
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task<Guid> SeedAdminUserAsync(CancellationToken cancellationToken)
    {
        const string devPassword = "Admin123!";
        const string placeholderHash = "PLACEHOLDER_HASH";

        var adminUser = await _context.Users
            .Where(u => u.Email == "admin@company.com")
            .FirstOrDefaultAsync(cancellationToken);

        if (adminUser is null)
        {
            var passwordHash = _environment.IsDevelopment() || _environment.IsEnvironment("Testing")
                ? _passwordHasher.Hash(devPassword)
                : placeholderHash;

            var user = new User(AdminUserId, "admin@company.com", "admin", passwordHash);
            user.UpdateProfile(new UserProfile("Admin", "User"));
            _context.Users.Add(user);

            await _context.SaveChangesAsync(cancellationToken);
            return AdminUserId;
        }

        if ((_environment.IsDevelopment() || _environment.IsEnvironment("Testing")) && adminUser.PasswordHash == placeholderHash)
        {
            adminUser.UpdatePasswordHash(_passwordHasher.Hash(devPassword));
            await _context.SaveChangesAsync(cancellationToken);
        }

        if (adminUser.Profile is null)
        {
            adminUser.UpdateProfile(new UserProfile("Admin", "User"));
            await _context.SaveChangesAsync(cancellationToken);
        }

        return adminUser.Id;
    }

    private async Task SeedUserRoleAsync(
        Guid userId,
        Guid roleId,
        CancellationToken cancellationToken)
    {
        var exists = await _context.UserRoles
            .AnyAsync(ur => ur.UserId == userId && ur.RoleId == roleId, cancellationToken);

        if (!exists)
        {
            _context.UserRoles.Add(new UserRole(userId, roleId));
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
