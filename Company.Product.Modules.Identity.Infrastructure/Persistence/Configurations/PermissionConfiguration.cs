using Company.Product.Modules.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Company.Product.Modules.Identity.Infrastructure.Persistence.Configurations
{
    internal sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
    {
        public void Configure(EntityTypeBuilder<Permission> b)
        {
            b.ToTable("Permissions", "identity");

            b.HasKey(x => x.Id);

            b.Property(x => x.Code).HasMaxLength(150).IsRequired();
            b.Property(x => x.Name).HasMaxLength(150).IsRequired();

            b.HasIndex(x => x.Code).IsUnique();
        }
    }
}
