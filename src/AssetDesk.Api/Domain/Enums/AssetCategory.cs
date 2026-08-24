using System.Text.Json.Serialization;

namespace AssetDesk.Api.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter<AssetCategory>))]
public enum AssetCategory
{
    Laptop = 0,
    Desktop = 1,
    Server = 2,
    NetworkDevice = 3,
    Mobile = 4,
    Peripheral = 5
}
