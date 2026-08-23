using AssetDesk.Api.Application.Abstractions;

namespace AssetDesk.Api.Infrastructure.Persistence;

/// <summary>
/// El DbContext ya es un unit of work; esta clase solo lo expone detras de una interfaz
/// para que la capa de servicios no tenga que referenciar EF Core.
/// </summary>
public class UnitOfWork(AppDbContext db) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
