namespace Company.Product.Modules.Identity.Domain.Entities
{
    public sealed class User
    {
        public Guid Id { get; private set; }
        public string Email { get; private set; } = default!;
        public string UserName { get; private set; } = default!;
        public string PasswordHash { get; private set; } = default!;
        public bool IsActive { get; private set; } = true;

        public DateTime CreatedAtUtc { get; private set; }
        public DateTime? UpdatedAtUtc { get; private set; }

        // Navegación
        public ICollection<UserRole> UserRoles { get; private set; } = new List<UserRole>();

        private User() { } // EF

        public User(Guid id, string email, string userName, string passwordHash)
        {
            Id = id;
            Email = email.Trim().ToLowerInvariant();
            UserName = userName.Trim();
            PasswordHash = passwordHash;
            CreatedAtUtc = DateTime.UtcNow;
            IsActive = true;
        }

        public void Deactivate()
        {
            IsActive = false;
            UpdatedAtUtc = DateTime.UtcNow;
        }

        public void UpdatePasswordHash(string passwordHash)
        {
            PasswordHash = passwordHash;
            UpdatedAtUtc = DateTime.UtcNow;
        }
    }
}
