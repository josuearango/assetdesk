using System.Text.Json.Serialization;

namespace AssetDesk.Api.Domain.Enums;

/// <summary>Ciclo de vida de un activo. Decommissioned es terminal.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<AssetStatus>))]
public enum AssetStatus
{
    InStock = 0,
    Assigned = 1,
    InRepair = 2,
    Decommissioned = 3
}
