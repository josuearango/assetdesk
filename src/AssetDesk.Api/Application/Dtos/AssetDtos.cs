using System.ComponentModel.DataAnnotations;
using AssetDesk.Api.Domain.Entities;
using AssetDesk.Api.Domain.Enums;

namespace AssetDesk.Api.Application.Dtos;

// ---------- Requests ----------
// Las DataAnnotations validan la FORMA del request y producen 400 Bad Request.
// Las reglas de negocio viven en el dominio y producen 409 Conflict. Son preguntas
// distintas: "esta bien escrito" no es lo mismo que "el sistema lo permite ahora".

public record CreateAssetRequest(
    [Required, MaxLength(32)] string AssetTag,
    [Required, MaxLength(200)] string Name,
    [Required] AssetCategory Category,
    [MaxLength(100)] string? SerialNumber,
    DateOnly? PurchasedOn,
    DateOnly? WarrantyExpiresOn);

public record AssignAssetRequest(
    [Required, MaxLength(128)] string UserId,
    [MaxLength(500)] string? Note);

public record ChangeAssetStatusRequest(
    [Required] AssetStatus Status,
    [MaxLength(500)] string? Note);

public record ReturnAssetRequest([MaxLength(500)] string? Note);

public record DecommissionAssetRequest([MaxLength(500)] string? Note);

// ---------- Responses ----------

public record AssetResponse(
    int Id,
    string AssetTag,
    string Name,
    AssetCategory Category,
    string? SerialNumber,
    AssetStatus Status,
    string? AssignedToUserId,
    DateOnly? PurchasedOn,
    DateOnly? WarrantyExpiresOn,
    bool AcceptsNewTickets,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc)
{
    public static AssetResponse From(Asset a) => new(
        a.Id, a.AssetTag, a.Name, a.Category, a.SerialNumber, a.Status, a.AssignedToUserId,
        a.PurchasedOn, a.WarrantyExpiresOn, a.AcceptsNewTickets, a.CreatedAtUtc, a.UpdatedAtUtc);
}

public record AssetHistoryEntryResponse(
    int Id,
    AssetChangeType ChangeType,
    string? FromValue,
    string? ToValue,
    string PerformedBy,
    DateTime OccurredAtUtc,
    string? Note)
{
    public static AssetHistoryEntryResponse From(AssetHistoryEntry h) => new(
        h.Id, h.ChangeType, h.FromValue, h.ToValue, h.PerformedBy, h.OccurredAtUtc, h.Note);
}

public record AssetDetailResponse(AssetResponse Asset, IReadOnlyList<AssetHistoryEntryResponse> History)
{
    public static AssetDetailResponse From(Asset a) => new(
        AssetResponse.From(a),
        [.. a.History.OrderByDescending(h => h.OccurredAtUtc).Select(AssetHistoryEntryResponse.From)]);
}
