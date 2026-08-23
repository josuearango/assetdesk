using AssetDesk.Api.Domain.Enums;

namespace AssetDesk.Api.Domain.Entities;

/// <summary>
/// Renglon inmutable del historial de un activo. Responde "quien hizo que y cuando",
/// que es la pregunta que siempre llega en una auditoria de inventario.
/// </summary>
public class AssetHistoryEntry
{
    private AssetHistoryEntry() { }

    internal AssetHistoryEntry(
        AssetChangeType changeType,
        string? fromValue,
        string? toValue,
        string performedBy,
        DateTime occurredAtUtc,
        string? note)
    {
        ChangeType = changeType;
        FromValue = fromValue;
        ToValue = toValue;
        PerformedBy = performedBy;
        OccurredAtUtc = occurredAtUtc;
        Note = note;
    }

    public int Id { get; private set; }
    public int AssetId { get; private set; }
    public AssetChangeType ChangeType { get; private set; }
    public string? FromValue { get; private set; }
    public string? ToValue { get; private set; }
    public string PerformedBy { get; private set; } = null!;
    public DateTime OccurredAtUtc { get; private set; }
    public string? Note { get; private set; }
}
