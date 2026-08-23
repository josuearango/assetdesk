namespace AssetDesk.Api.Application.Abstractions;

/// <summary>
/// El commit. Los repositorios consultan y agregan al grafo; confirmar la transaccion es
/// una responsabilidad aparte, para que un servicio que toca dos repositorios pueda
/// guardar todo en un solo SaveChanges y no a pedazos.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
