using System.Text.Json.Serialization;

namespace AssetDesk.Api.Domain.Enums;

/// <summary>
/// Estados de un ticket. Closed y Cancelled son terminales: no se sale de ellos.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<TicketStatus>))]
public enum TicketStatus
{
    New = 0,
    InProgress = 1,
    OnHold = 2,
    Resolved = 3,
    Closed = 4,
    Cancelled = 5
}
