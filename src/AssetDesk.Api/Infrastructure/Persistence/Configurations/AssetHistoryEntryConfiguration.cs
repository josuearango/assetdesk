using AssetDesk.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssetDesk.Api.Infrastructure.Persistence.Configurations;

public class AssetHistoryEntryConfiguration : IEntityTypeConfiguration<AssetHistoryEntry>
{
    public void Configure(EntityTypeBuilder<AssetHistoryEntry> builder)
    {
        builder.ToTable("AssetHistory");
        builder.HasKey(h => h.Id);

        builder.Property(h => h.ChangeType).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(h => h.FromValue).HasMaxLength(128);
        builder.Property(h => h.ToValue).HasMaxLength(128);
        builder.Property(h => h.PerformedBy).IsRequired().HasMaxLength(128);
        builder.Property(h => h.Note).HasMaxLength(500);

        // El caso de uso real es "dame el historial de este activo, lo mas nuevo arriba".
        builder.HasIndex(h => new { h.AssetId, h.OccurredAtUtc });
    }
}
