using System.Text.Json.Serialization;

namespace AssetDesk.Api.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter<TicketChangeType>))]
public enum TicketChangeType
{
    Created = 0,
    StatusChanged = 1,
    PriorityChanged = 2,
    Assigned = 3,
    Escalated = 4
}
