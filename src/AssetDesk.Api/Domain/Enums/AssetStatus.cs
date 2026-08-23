namespace AssetDesk.Api.Domain.Enums;

/// <summary>Ciclo de vida de un activo. Decommissioned es terminal.</summary>
public enum AssetStatus
{
    InStock = 0,
    Assigned = 1,
    InRepair = 2,
    Decommissioned = 3
}
