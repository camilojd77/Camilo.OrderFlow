namespace Company.Product.Modules.Identity.Domain.Entities
{
    public sealed class Role
    {
        public Guid Id { get; private set; }
        public string Name { get; private set; } = default!;
        public string? Description { get; private set; }

        public ICollection<UserRole> UserRoles { get; private set; } = new List<UserRole>();
        public ICollection<RolePermission> RolePermissions { get; private set; } = new List<RolePermission>();

        private Role() { } // EF

        public Role(Guid id, string name, string? description = null)
        {
            Id = id;
            Name = name.Trim();
            Description = description?.Trim();
        }

        public void Update(string name, string? description)
        {
            Name = name.Trim();
            Description = description?.Trim();
        }
    }
}
