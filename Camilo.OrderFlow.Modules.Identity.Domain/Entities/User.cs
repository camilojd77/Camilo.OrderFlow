using Camilo.OrderFlow.Modules.Identity.Domain.ValueObjects;

namespace Camilo.OrderFlow.Modules.Identity.Domain.Entities
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

        public UserProfile? Profile { get; private set; }

        public int FailedLoginAttempts { get; private set; }
        public DateTime? LockoutEndUtc { get; private set; }

        public bool IsLockedOut => LockoutEndUtc.HasValue && LockoutEndUtc.Value > DateTime.UtcNow;

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

        public void Activate()
        {
            IsActive = true;
            UpdatedAtUtc = DateTime.UtcNow;
        }

        public void UpdatePasswordHash(string passwordHash)
        {
            PasswordHash = passwordHash;
            UpdatedAtUtc = DateTime.UtcNow;
        }

        public void UpdateProfile(UserProfile newProfile)
        {
            ArgumentNullException.ThrowIfNull(newProfile);
            Profile = newProfile;
            UpdatedAtUtc = DateTime.UtcNow;
        }

        public void RegisterFailedLogin(int maxAttempts, TimeSpan lockoutDuration)
        {
            FailedLoginAttempts++;
            if (FailedLoginAttempts >= maxAttempts)
                LockoutEndUtc = DateTime.UtcNow.Add(lockoutDuration);
            UpdatedAtUtc = DateTime.UtcNow;
        }

        public void ResetFailedLoginAttempts()
        {
            FailedLoginAttempts = 0;
            LockoutEndUtc = null;
            UpdatedAtUtc = DateTime.UtcNow;
        }
    }
}
