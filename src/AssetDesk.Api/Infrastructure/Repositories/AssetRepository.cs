using AssetDesk.Api.Application.Abstractions;
using AssetDesk.Api.Domain.Entities;
using AssetDesk.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AssetDesk.Api.Infrastructure.Repositories;

public class AssetRepository(AppDbContext db) : IAssetRepository
{
    public Task<Asset?> GetByIdAsync(int id, CancellationToken ct = default) =>
        db.Assets.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, ct);

    public Task<Asset?> GetForUpdateAsync(int id, CancellationToken ct = default) =>
        db.Assets.FirstOrDefaultAsync(a => a.Id == id, ct);

    public Task<Asset?> GetWithHistoryAsync(int id, CancellationToken ct = default) =>
        db.Assets
            .AsNoTracking()
            .Include(a => a.History)
            .FirstOrDefaultAsync(a => a.Id == id, ct);

    public Task<bool> AssetTagExistsAsync(string assetTag, CancellationToken ct = default)
    {
        var normalized = assetTag.Trim().ToUpperInvariant();
        return db.Assets.AsNoTracking().AnyAsync(a => a.AssetTag == normalized, ct);
    }

    public async Task<PagedResult<Asset>> SearchAsync(AssetQuery query, CancellationToken ct = default)
    {
        var q = db.Assets.AsNoTracking().AsQueryable();

        if (query.Status is not null)
            q = q.Where(a => a.Status == query.Status);
        if (query.Category is not null)
            q = q.Where(a => a.Category == query.Category);
        if (!string.IsNullOrWhiteSpace(query.AssignedToUserId))
            q = q.Where(a => a.AssignedToUserId == query.AssignedToUserId);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            q = q.Where(a =>
                EF.Functions.Like(a.AssetTag, $"%{term}%") ||
                EF.Functions.Like(a.Name, $"%{term}%") ||
                (a.SerialNumber != null && EF.Functions.Like(a.SerialNumber, $"%{term}%")));
        }

        // El COUNT se hace antes de paginar: es el total que coincide, no el de la pagina.
        var total = await q.CountAsync(ct);

        var page = Math.Max(1, query.Page);
        var size = Math.Clamp(query.PageSize, 1, 100);

        var items = await q
            .OrderBy(a => a.AssetTag)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync(ct);

        return new PagedResult<Asset>(items, total, page, size);
    }

    public void Add(Asset asset) => db.Assets.Add(asset);
}
