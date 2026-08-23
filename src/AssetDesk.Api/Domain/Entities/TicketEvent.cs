using AssetDesk.Api.Domain.Enums;

namespace AssetDesk.Api.Domain.Entities;

/// <summary>
/// Renglon inmutable de la bitacora de un ticket. Misma forma que AssetHistoryEntry
/// a proposito: una sola manera de leer "quien cambio que y cuando" en todo el sistema.
/// </summary>
public class TicketEvent
{
    private TicketEvent() { }

    internal TicketEvent(
        TicketChangeType changeType,
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
    public int TicketId { get; private set; }
    public TicketChangeType ChangeType { get; private set; }
    public string? FromValue { get; private set; }
    public string? ToValue { get; private set; }
    public string PerformedBy { get; private set; } = null!;
    public DateTime OccurredAtUtc { get; private set; }
    public string? Note { get; private set; }
}
