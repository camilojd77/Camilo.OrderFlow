using Camilo.OrderFlow.Modules.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Camilo.OrderFlow.Modules.Identity.Infrastructure.Persistence.Configurations;

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> b)
    {
        b.ToTable("RefreshTokens", "identity");

        b.HasKey(x => x.Id);

        b.Property(x => x.TokenHash).HasMaxLength(64).IsRequired();
        b.Property(x => x.CreatedByIp).HasMaxLength(45).IsRequired();
        b.Property(x => x.ExpiresAtUtc).IsRequired();
        b.Property(x => x.RevokedAtUtc);
        b.Property(x => x.ReplacedByTokenId);

        b.HasIndex(x => x.TokenHash).IsUnique();

        b.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
