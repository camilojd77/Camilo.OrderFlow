namespace Company.Product.Modules.Identity.Domain.Entities
{
    public sealed class Permission
    {
        public Guid Id { get; private set; }
        public string Code { get; private set; } = default!;   // ej: "users.read"
        public string Name { get; private set; } = default!;   // ej: "Ver usuarios"

        public ICollection<RolePermission> RolePermissions { get; private set; } = new List<RolePermission>();

        private Permission() { } // EF

        public Permission(Guid id, string code, string name)
        {
            Id = id;
            Code = code.Trim().ToLowerInvariant();
            Name = name.Trim();
        }
    }
}
