using AssetDesk.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssetDesk.Api.Infrastructure.Persistence.Configurations;

public class AssetConfiguration : IEntityTypeConfiguration<Asset>
{
    public void Configure(EntityTypeBuilder<Asset> builder)
    {
        builder.ToTable("Assets");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.AssetTag).IsRequired().HasMaxLength(32);
        builder.HasIndex(a => a.AssetTag).IsUnique();

        builder.Property(a => a.Name).IsRequired().HasMaxLength(200);
        builder.Property(a => a.SerialNumber).HasMaxLength(100);
        builder.Property(a => a.AssignedToUserId).HasMaxLength(128);

        // Los enums se guardan como texto, no como int. Cuesta unos bytes mas y gana dos
        // cosas: la tabla se puede leer sin diccionario, y reordenar el enum en C# no
        // reinterpreta silenciosamente las filas que ya estan grabadas.
        builder.Property(a => a.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(a => a.Category).HasConversion<string>().HasMaxLength(32).IsRequired();

        // rowversion de SQL Server: si dos peticiones editan el mismo activo, la segunda
        // falla con DbUpdateConcurrencyException en lugar de sobrescribir a ciegas.
        builder.Property(a => a.RowVersion).IsRowVersion();

        builder.HasMany(a => a.History)
            .WithOne()
            .HasForeignKey(h => h.AssetId)
            .OnDelete(DeleteBehavior.Cascade);

        // La propiedad History devuelve un envoltorio nuevo en cada llamada, asi que EF
        // tiene que leer y escribir el campo _history directamente, no el getter.
        builder.Navigation(a => a.History)
            .HasField("_history")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(a => a.Status);
        builder.HasIndex(a => a.AssignedToUserId);
    }
}
