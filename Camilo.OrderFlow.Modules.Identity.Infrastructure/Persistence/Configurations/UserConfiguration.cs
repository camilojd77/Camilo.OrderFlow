using Camilo.OrderFlow.Modules.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Camilo.OrderFlow.Modules.Identity.Infrastructure.Persistence.Configurations
{
    internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> b)
        {
            b.ToTable("Users", "identity");

            b.HasKey(x => x.Id);

            b.Property(x => x.Email).HasMaxLength(256).IsRequired();
            b.Property(x => x.UserName).HasMaxLength(100).IsRequired();
            b.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();

            b.Property(x => x.IsActive).IsRequired();

            b.Property(x => x.CreatedAtUtc).IsRequired();
            b.Property(x => x.UpdatedAtUtc);

            b.Property(x => x.FailedLoginAttempts).IsRequired().HasDefaultValue(0);
            b.Property(x => x.LockoutEndUtc);

            b.HasIndex(x => x.Email).IsUnique();
            b.HasIndex(x => x.UserName).IsUnique();

            b.OwnsOne(x => x.Profile, p =>
            {
                p.Property(x => x.FirstName).HasMaxLength(100).HasColumnName("FirstName");
                p.Property(x => x.LastName).HasMaxLength(100).HasColumnName("LastName");
                p.Property(x => x.PhoneNumber).HasMaxLength(20).HasColumnName("PhoneNumber");
                p.Property(x => x.AvatarUrl).HasMaxLength(500).HasColumnName("AvatarUrl");
            });

            b.Navigation(x => x.Profile).IsRequired(false);
        }
    }
}
