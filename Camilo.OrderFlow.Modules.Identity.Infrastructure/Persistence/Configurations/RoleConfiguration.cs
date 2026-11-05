using Camilo.OrderFlow.Modules.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Camilo.OrderFlow.Modules.Identity.Infrastructure.Persistence.Configurations
{
    internal sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
    {
        public void Configure(EntityTypeBuilder<Role> b)
        {
            b.ToTable("Roles", "identity");

            b.HasKey(x => x.Id);

            b.Property(x => x.Name).HasMaxLength(100).IsRequired();
            b.Property(x => x.Description).HasMaxLength(250);

            b.HasIndex(x => x.Name).IsUnique();
        }
    }
}
