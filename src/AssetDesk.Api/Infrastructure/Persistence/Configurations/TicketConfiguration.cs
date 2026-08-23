using AssetDesk.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssetDesk.Api.Infrastructure.Persistence.Configurations;

public class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.ToTable("Tickets");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Title).IsRequired().HasMaxLength(200);
        builder.Property(t => t.Description).HasMaxLength(4000);
        builder.Property(t => t.AssignedToUserId).HasMaxLength(128);
        builder.Property(t => t.CreatedBy).IsRequired().HasMaxLength(128);

        builder.Property(t => t.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(t => t.Priority).HasConversion<string>().HasMaxLength(32).IsRequired();

        builder.Property(t => t.RowVersion).IsRowVersion();

        // Un activo no se borra nunca (se da de baja), pero si alguien lo intentara,
        // Restrict evita dejar tickets huerfanos apuntando a nada.
        builder.HasOne(t => t.Asset)
            .WithMany()
            .HasForeignKey(t => t.AssetId)
            .OnDelete(DeleteBehavior.Restrict);

        // Auto-referencia padre/subtareas. Restrict y no Cascade: borrar un padre no debe
        // llevarse las subtareas de arrastre, y SQL Server tampoco acepta ciclos de cascada.
        builder.HasMany(t => t.Subtasks)
            .WithOne(t => t.ParentTicket)
            .HasForeignKey(t => t.ParentTicketId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(t => t.Subtasks)
            .HasField("_subtasks")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(t => t.Events)
            .WithOne()
            .HasForeignKey(e => e.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(t => t.Events)
            .HasField("_events")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(t => t.Status);
        builder.HasIndex(t => t.AssignedToUserId);
        builder.HasIndex(t => t.AssetId);
        builder.HasIndex(t => t.ParentTicketId);
    }
}
