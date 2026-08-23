using AssetDesk.Api.Domain.Entities;
using AssetDesk.Api.Domain.Enums;

namespace AssetDesk.Api.Application.Abstractions;

/// <summary>Filtros de busqueda de activos, con paginado.</summary>
public record AssetQuery(
    AssetStatus? Status = null,
    AssetCategory? Category = null,
    string? AssignedToUserId = null,
    string? Search = null,
    int Page = 1,
    int PageSize = 20);

public record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public interface IAssetRepository
{
    /// <summary>Sin tracking: para responder consultas, no para mutar.</summary>
    Task<Asset?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>Con tracking: el activo que devuelve se puede mutar y guardar.</summary>
    Task<Asset?> GetForUpdateAsync(int id, CancellationToken ct = default);

    Task<Asset?> GetWithHistoryAsync(int id, CancellationToken ct = default);

    Task<bool> AssetTagExistsAsync(string assetTag, CancellationToken ct = default);

    Task<PagedResult<Asset>> SearchAsync(AssetQuery query, CancellationToken ct = default);

    void Add(Asset asset);
}
