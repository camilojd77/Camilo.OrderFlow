using Company.Product.Modules.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Company.Product.Modules.Identity.Infrastructure.Persistence.Configurations
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

            b.HasIndex(x => x.Email).IsUnique();
            b.HasIndex(x => x.UserName).IsUnique();
        }
    }
}
