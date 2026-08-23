using AssetDesk.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssetDesk.Api.Infrastructure.Persistence.Configurations;

public class TicketEventConfiguration : IEntityTypeConfiguration<TicketEvent>
{
    public void Configure(EntityTypeBuilder<TicketEvent> builder)
    {
        builder.ToTable("TicketEvents");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.ChangeType).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(e => e.FromValue).HasMaxLength(128);
        builder.Property(e => e.ToValue).HasMaxLength(128);
        builder.Property(e => e.PerformedBy).IsRequired().HasMaxLength(128);
        builder.Property(e => e.Note).HasMaxLength(500);

        builder.HasIndex(e => new { e.TicketId, e.OccurredAtUtc });
    }
}
