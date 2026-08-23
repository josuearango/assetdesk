using AssetDesk.Api.Application.Abstractions;
using AssetDesk.Api.Application.Dtos;
using AssetDesk.Api.Domain.Entities;
using AssetDesk.Api.Domain.Exceptions;

namespace AssetDesk.Api.Application.Services;

/// <summary>
/// Orquesta: carga, delega la decision a la entidad, confirma. No decide reglas de negocio
/// por su cuenta; si aparece un "if" de negocio aca, va mal ubicado y pertenece a Asset.
/// </summary>
public class AssetService(
    IAssetRepository assets,
    IUnitOfWork uow,
    ICurrentUser currentUser,
    TimeProvider clock) : IAssetService
{
    private DateTime NowUtc => clock.GetUtcNow().UtcDateTime;

    public async Task<AssetResponse> CreateAsync(CreateAssetRequest request, CancellationToken ct = default)
    {
        // Unicidad del tag: hay indice unico en la base, pero comprobarlo aca da un 409 con
        // mensaje entendible en lugar de una excepcion de SQL Server.
        if (await assets.AssetTagExistsAsync(request.AssetTag, ct))
            throw new DomainRuleException($"Ya existe un activo con el tag {request.AssetTag.Trim().ToUpperInvariant()}.");

        var asset = Asset.Create(
            request.AssetTag,
            request.Name,
            request.Category,
            request.SerialNumber,
            request.PurchasedOn,
            request.WarrantyExpiresOn,
            currentUser.UserId,
            NowUtc);

        assets.Add(asset);
        await uow.SaveChangesAsync(ct);

        return AssetResponse.From(asset);
    }

    public async Task<AssetDetailResponse> GetAsync(int id, CancellationToken ct = default)
    {
        var asset = await assets.GetWithHistoryAsync(id, ct)
                    ?? throw new NotFoundException(nameof(Asset), id);
        return AssetDetailResponse.From(asset);
    }

    public async Task<PagedResult<AssetResponse>> SearchAsync(AssetQuery query, CancellationToken ct = default)
    {
        var page = await assets.SearchAsync(query, ct);
        return new PagedResult<AssetResponse>(
            [.. page.Items.Select(AssetResponse.From)], page.TotalCount, page.Page, page.PageSize);
    }

    public async Task<AssetResponse> AssignAsync(int id, AssignAssetRequest request, CancellationToken ct = default)
    {
        var asset = await assets.GetForUpdateAsync(id, ct)
                    ?? throw new NotFoundException(nameof(Asset), id);

        asset.Assign(request.UserId, currentUser.UserId, NowUtc, request.Note);
        await uow.SaveChangesAsync(ct);

        return AssetResponse.From(asset);
    }

    public async Task<AssetResponse> ReturnAsync(int id, string? note, CancellationToken ct = default)
    {
        var asset = await assets.GetForUpdateAsync(id, ct)
                    ?? throw new NotFoundException(nameof(Asset), id);

        asset.Return(currentUser.UserId, NowUtc, note);
        await uow.SaveChangesAsync(ct);

        return AssetResponse.From(asset);
    }

    public async Task<AssetResponse> ChangeStatusAsync(int id, ChangeAssetStatusRequest request, CancellationToken ct = default)
    {
        var asset = await assets.GetForUpdateAsync(id, ct)
                    ?? throw new NotFoundException(nameof(Asset), id);

        asset.ChangeStatus(request.Status, currentUser.UserId, NowUtc, request.Note);
        await uow.SaveChangesAsync(ct);

        return AssetResponse.From(asset);
    }

    public async Task<AssetResponse> DecommissionAsync(int id, DecommissionAssetRequest request, CancellationToken ct = default)
    {
        var asset = await assets.GetForUpdateAsync(id, ct)
                    ?? throw new NotFoundException(nameof(Asset), id);

        asset.Decommission(currentUser.UserId, NowUtc, request.Note);
        await uow.SaveChangesAsync(ct);

        return AssetResponse.From(asset);
    }
}
