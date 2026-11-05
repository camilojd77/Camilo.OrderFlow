namespace Camilo.OrderFlow.Modules.Identity.Domain.Entities
{
    public sealed class RolePermission
    {
        public Guid RoleId { get; private set; }
        public Guid PermissionId { get; private set; }

        public Role Role { get; private set; } = default!;
        public Permission Permission { get; private set; } = default!;

        private RolePermission() { } // EF

        public RolePermission(Guid roleId, Guid permissionId)
        {
            RoleId = roleId;
            PermissionId = permissionId;
        }
    }
}
