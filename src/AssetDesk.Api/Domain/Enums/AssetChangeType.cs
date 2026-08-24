using System.Text.Json.Serialization;

namespace AssetDesk.Api.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter<AssetChangeType>))]
public enum AssetChangeType
{
    Created = 0,
    Assigned = 1,
    Returned = 2,
    StatusChanged = 3,
    Decommissioned = 4
}
