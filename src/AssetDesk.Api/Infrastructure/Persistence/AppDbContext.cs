using AssetDesk.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AssetDesk.Api.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<Ticket> Tickets => Set<Ticket>();

    // Solo lectura en la practica: las bitacoras se escriben a traves de la entidad raiz,
    // nunca directo. Se exponen para poder consultarlas sin cargar el agregado completo.
    public DbSet<AssetHistoryEntry> AssetHistory => Set<AssetHistoryEntry>();
    public DbSet<TicketEvent> TicketEvents => Set<TicketEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Toma todas las clases IEntityTypeConfiguration de este assembly, para no tener
        // que acordarse de registrarlas una por una cada vez que se agrega una entidad.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
