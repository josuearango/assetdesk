using AssetDesk.Api.Domain.Enums;
using AssetDesk.Api.Domain.Exceptions;

namespace AssetDesk.Api.Domain.Entities;

/// <summary>
/// Activo de TI. Las reglas viven aca adentro, no en el servicio: un Asset no puede
/// existir en un estado invalido porque los setters son privados y toda mutacion pasa
/// por un metodo que valida y deja rastro en el historial.
/// </summary>
public class Asset
{
    private readonly List<AssetHistoryEntry> _history = [];

    // EF Core necesita un constructor sin parametros. Queda privado para que el resto
    // del codigo este obligado a usar Create().
    private Asset() { }

    public int Id { get; private set; }
    public string AssetTag { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public AssetCategory Category { get; private set; }
    public string? SerialNumber { get; private set; }
    public AssetStatus Status { get; private set; }
    public string? AssignedToUserId { get; private set; }
    public DateOnly? PurchasedOn { get; private set; }
    public DateOnly? WarrantyExpiresOn { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    /// <summary>Mapeado a rowversion de SQL Server: concurrencia optimista.</summary>
    public byte[] RowVersion { get; private set; } = [];

    public IReadOnlyCollection<AssetHistoryEntry> History => _history.AsReadOnly();

    /// <summary>
    /// Regla de negocio: un activo dado de baja no acepta tickets nuevos.
    /// TicketService consulta esto antes de crear un ticket.
    /// </summary>
    public bool AcceptsNewTickets => Status != AssetStatus.Decommissioned;

    public static Asset Create(
        string assetTag,
        string name,
        AssetCategory category,
        string? serialNumber,
        DateOnly? purchasedOn,
        DateOnly? warrantyExpiresOn,
        string performedBy,
        DateTime nowUtc)
    {
        if (string.IsNullOrWhiteSpace(assetTag))
            throw new DomainRuleException("El asset tag es obligatorio.");
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainRuleException("El nombre del activo es obligatorio.");
        if (warrantyExpiresOn is not null && purchasedOn is not null && warrantyExpiresOn < purchasedOn)
            throw new DomainRuleException("La garantia no puede vencer antes de la fecha de compra.");

        var asset = new Asset
        {
            AssetTag = assetTag.Trim().ToUpperInvariant(),
            Name = name.Trim(),
            Category = category,
            SerialNumber = string.IsNullOrWhiteSpace(serialNumber) ? null : serialNumber.Trim(),
            Status = AssetStatus.InStock,
            PurchasedOn = purchasedOn,
            WarrantyExpiresOn = warrantyExpiresOn,
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc
        };

        asset._history.Add(new AssetHistoryEntry(
            AssetChangeType.Created, null, AssetStatus.InStock.ToString(), performedBy, nowUtc, null));

        return asset;
    }

    public void Assign(string userId, string performedBy, DateTime nowUtc, string? note = null)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new DomainRuleException("Hay que indicar a quien se asigna el activo.");
        if (Status == AssetStatus.Decommissioned)
            throw new DomainRuleException("Un activo dado de baja no se puede asignar.");
        if (Status == AssetStatus.InRepair)
            throw new DomainRuleException("Un activo en reparacion no se puede asignar; primero sacalo de reparacion.");
        if (AssignedToUserId == userId)
            throw new DomainRuleException($"El activo ya esta asignado a {userId}.");

        // Reasignar directo es valido, pero deja las dos huellas: devolucion y nueva entrega.
        if (AssignedToUserId is not null)
        {
            _history.Add(new AssetHistoryEntry(
                AssetChangeType.Returned, AssignedToUserId, null, performedBy, nowUtc,
                "Devolucion implicita por reasignacion."));
        }

        _history.Add(new AssetHistoryEntry(
            AssetChangeType.Assigned, AssignedToUserId, userId, performedBy, nowUtc, note));

        AssignedToUserId = userId;
        Status = AssetStatus.Assigned;
        UpdatedAtUtc = nowUtc;
    }

    public void Return(string performedBy, DateTime nowUtc, string? note = null)
    {
        if (Status != AssetStatus.Assigned || AssignedToUserId is null)
            throw new DomainRuleException("El activo no esta asignado a nadie.");

        _history.Add(new AssetHistoryEntry(
            AssetChangeType.Returned, AssignedToUserId, null, performedBy, nowUtc, note));

        AssignedToUserId = null;
        Status = AssetStatus.InStock;
        UpdatedAtUtc = nowUtc;
    }

    /// <summary>
    /// Mueve el activo entre InStock e InRepair. Assigned y Decommissioned no se alcanzan
    /// por aca a proposito: tienen su propio metodo porque arrastran mas efectos.
    /// </summary>
    public void ChangeStatus(AssetStatus newStatus, string performedBy, DateTime nowUtc, string? note = null)
    {
        if (newStatus is AssetStatus.Assigned)
            throw new DomainRuleException("Para asignar un activo usa la operacion de asignacion.");
        if (newStatus is AssetStatus.Decommissioned)
            throw new DomainRuleException("Para dar de baja un activo usa la operacion de baja.");
        if (Status == AssetStatus.Decommissioned)
            throw new DomainRuleException("Un activo dado de baja no cambia de estado: la baja es terminal.");
        if (Status == newStatus)
            throw new DomainRuleException($"El activo ya esta en estado {newStatus}.");

        if (Status == AssetStatus.Assigned && AssignedToUserId is not null)
        {
            // Mandar a reparacion algo asignado implica que el usuario lo entrego.
            _history.Add(new AssetHistoryEntry(
                AssetChangeType.Returned, AssignedToUserId, null, performedBy, nowUtc,
                "Devolucion implicita por cambio de estado."));
            AssignedToUserId = null;
        }

        _history.Add(new AssetHistoryEntry(
            AssetChangeType.StatusChanged, Status.ToString(), newStatus.ToString(), performedBy, nowUtc, note));

        Status = newStatus;
        UpdatedAtUtc = nowUtc;
    }

    public void Decommission(string performedBy, DateTime nowUtc, string? note = null)
    {
        if (Status == AssetStatus.Decommissioned)
            throw new DomainRuleException("El activo ya esta dado de baja.");

        if (AssignedToUserId is not null)
        {
            _history.Add(new AssetHistoryEntry(
                AssetChangeType.Returned, AssignedToUserId, null, performedBy, nowUtc,
                "Devolucion implicita por baja del activo."));
            AssignedToUserId = null;
        }

        _history.Add(new AssetHistoryEntry(
            AssetChangeType.Decommissioned, Status.ToString(), AssetStatus.Decommissioned.ToString(),
            performedBy, nowUtc, note));

        Status = AssetStatus.Decommissioned;
        UpdatedAtUtc = nowUtc;
    }
}
