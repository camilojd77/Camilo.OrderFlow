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

        public bool IsLockedOut(DateTime utcNow) => LockoutEndUtc.HasValue && LockoutEndUtc.Value > utcNow;

        // Navegación
        public ICollection<UserRole> UserRoles { get; private set; } = new List<UserRole>();

        private User() { } // EF

        public User(Guid id, string email, string userName, string passwordHash, DateTime createdAtUtc)
        {
            Id = id;
            Email = email.Trim().ToLowerInvariant();
            UserName = userName.Trim();
            PasswordHash = passwordHash;
            CreatedAtUtc = createdAtUtc;
            IsActive = true;
        }

        public void Deactivate(DateTime utcNow)
        {
            IsActive = false;
            UpdatedAtUtc = utcNow;
        }

        public void Activate(DateTime utcNow)
        {
            IsActive = true;
            UpdatedAtUtc = utcNow;
        }

        public void UpdatePasswordHash(string passwordHash, DateTime utcNow)
        {
            PasswordHash = passwordHash;
            UpdatedAtUtc = utcNow;
        }

        public void UpdateProfile(UserProfile newProfile, DateTime utcNow)
        {
            ArgumentNullException.ThrowIfNull(newProfile);
            Profile = newProfile;
            UpdatedAtUtc = utcNow;
        }

        public void RegisterFailedLogin(int maxAttempts, TimeSpan lockoutDuration, DateTime utcNow)
        {
            FailedLoginAttempts++;
            if (FailedLoginAttempts >= maxAttempts)
                LockoutEndUtc = utcNow.Add(lockoutDuration);
            UpdatedAtUtc = utcNow;
        }

        public void ResetFailedLoginAttempts(DateTime utcNow)
        {
            FailedLoginAttempts = 0;
            LockoutEndUtc = null;
            UpdatedAtUtc = utcNow;
        }
    }
}
