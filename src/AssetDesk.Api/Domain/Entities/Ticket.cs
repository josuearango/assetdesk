using AssetDesk.Api.Domain.Enums;
using AssetDesk.Api.Domain.Exceptions;

namespace AssetDesk.Api.Domain.Entities;

/// <summary>
/// Ticket de soporte. Modela tres reglas que en la vida real se rompen todo el tiempo
/// cuando el sistema no las obliga:
/// <list type="number">
///   <item>No se cierra un ticket que tiene subtareas abiertas.</item>
///   <item>Un activo dado de baja no acepta tickets nuevos.</item>
///   <item>Todo cambio de estado queda registrado con autor y momento.</item>
/// </list>
/// </summary>
public class Ticket
{
    public const int MaxEscalationLevel = 3;

    /// <summary>Transiciones permitidas. Closed y Cancelled no aparecen como origen: son terminales.</summary>
    private static readonly Dictionary<TicketStatus, TicketStatus[]> AllowedTransitions = new()
    {
        [TicketStatus.New] = [TicketStatus.InProgress, TicketStatus.Cancelled],
        [TicketStatus.InProgress] = [TicketStatus.OnHold, TicketStatus.Resolved, TicketStatus.Cancelled],
        [TicketStatus.OnHold] = [TicketStatus.InProgress, TicketStatus.Cancelled],
        [TicketStatus.Resolved] = [TicketStatus.Closed, TicketStatus.InProgress],
        [TicketStatus.Closed] = [],
        [TicketStatus.Cancelled] = []
    };

    private readonly List<TicketEvent> _events = [];
    private readonly List<Ticket> _subtasks = [];

    private Ticket() { }

    public int Id { get; private set; }
    public string Title { get; private set; } = null!;
    public string? Description { get; private set; }
    public TicketStatus Status { get; private set; }
    public TicketPriority Priority { get; private set; }
    public int? AssetId { get; private set; }
    public Asset? Asset { get; private set; }
    public string? AssignedToUserId { get; private set; }
    public int? ParentTicketId { get; private set; }
    public Ticket? ParentTicket { get; private set; }
    public int EscalationLevel { get; private set; }
    public string CreatedBy { get; private set; } = null!;
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public DateTime? ResolvedAtUtc { get; private set; }
    public DateTime? ClosedAtUtc { get; private set; }

    /// <summary>Mapeado a rowversion de SQL Server: concurrencia optimista.</summary>
    public byte[] RowVersion { get; private set; } = [];

    public IReadOnlyCollection<TicketEvent> Events => _events.AsReadOnly();
    public IReadOnlyCollection<Ticket> Subtasks => _subtasks.AsReadOnly();

    public bool IsTerminal => Status is TicketStatus.Closed or TicketStatus.Cancelled;

    /// <summary>
    /// Subtareas que todavia no llegaron a un estado terminal.
    /// OJO: depende de que la coleccion Subtasks venga cargada. El repositorio la incluye
    /// siempre en GetForUpdateAsync justamente para que esta regla no se evalue en falso.
    /// </summary>
    public int OpenSubtaskCount => _subtasks.Count(s => !s.IsTerminal);

    public static Ticket Create(
        string title,
        string? description,
        TicketPriority priority,
        Asset? asset,
        Ticket? parentTicket,
        string createdBy,
        DateTime nowUtc)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainRuleException("El titulo del ticket es obligatorio.");

        // Regla 2: un activo dado de baja no acepta tickets nuevos.
        if (asset is not null && !asset.AcceptsNewTickets)
            throw new DomainRuleException(
                $"El activo {asset.AssetTag} esta dado de baja y no acepta tickets nuevos.");

        if (parentTicket is not null)
        {
            if (parentTicket.IsTerminal)
                throw new DomainRuleException("No se puede colgar una subtarea de un ticket ya cerrado o cancelado.");
            if (parentTicket.ParentTicketId is not null)
                throw new DomainRuleException("Solo se permite un nivel de subtareas.");
        }

        var ticket = new Ticket
        {
            Title = title.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            Status = TicketStatus.New,
            Priority = priority,
            AssetId = asset?.Id,
            Asset = asset,
            ParentTicketId = parentTicket?.Id,
            ParentTicket = parentTicket,
            EscalationLevel = 0,
            CreatedBy = createdBy,
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc
        };

        ticket._events.Add(new TicketEvent(
            TicketChangeType.Created, null, TicketStatus.New.ToString(), createdBy, nowUtc, null));

        parentTicket?._subtasks.Add(ticket);

        return ticket;
    }

    /// <summary>Regla 1 y 3: valida la transicion, bloquea el cierre con subtareas abiertas y deja rastro.</summary>
    public void ChangeStatus(TicketStatus newStatus, string performedBy, DateTime nowUtc, string? note = null)
    {
        if (Status == newStatus)
            throw new DomainRuleException($"El ticket ya esta en estado {newStatus}.");

        if (!AllowedTransitions[Status].Contains(newStatus))
            throw new DomainRuleException(
                IsTerminal
                    ? $"El ticket esta en {Status}, que es un estado terminal: ya no cambia."
                    : $"No se permite pasar de {Status} a {newStatus}.");

        // Regla 1: no se cierra un ticket con subtareas abiertas.
        if (newStatus == TicketStatus.Closed && OpenSubtaskCount > 0)
            throw new DomainRuleException(
                $"No se puede cerrar el ticket: tiene {OpenSubtaskCount} subtarea(s) abierta(s).");

        _events.Add(new TicketEvent(
            TicketChangeType.StatusChanged, Status.ToString(), newStatus.ToString(), performedBy, nowUtc, note));

        Status = newStatus;
        UpdatedAtUtc = nowUtc;

        // Las marcas de tiempo se recalculan en cada transicion: reabrir un ticket
        // resuelto tiene que limpiar ResolvedAtUtc, si no las metricas de MTTR mienten.
        ResolvedAtUtc = newStatus == TicketStatus.Resolved ? nowUtc : null;
        ClosedAtUtc = newStatus is TicketStatus.Closed or TicketStatus.Cancelled ? nowUtc : null;
    }

    public void ChangePriority(TicketPriority newPriority, string performedBy, DateTime nowUtc, string? note = null)
    {
        if (IsTerminal)
            throw new DomainRuleException($"El ticket esta en {Status}: no se le cambia la prioridad.");
        if (Priority == newPriority)
            throw new DomainRuleException($"El ticket ya tiene prioridad {newPriority}.");

        _events.Add(new TicketEvent(
            TicketChangeType.PriorityChanged, Priority.ToString(), newPriority.ToString(), performedBy, nowUtc, note));

        Priority = newPriority;
        UpdatedAtUtc = nowUtc;
    }

    public void Assign(string userId, string performedBy, DateTime nowUtc, string? note = null)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new DomainRuleException("Hay que indicar a quien se asigna el ticket.");
        if (IsTerminal)
            throw new DomainRuleException($"El ticket esta en {Status}: no se reasigna.");
        if (AssignedToUserId == userId)
            throw new DomainRuleException($"El ticket ya esta asignado a {userId}.");

        _events.Add(new TicketEvent(
            TicketChangeType.Assigned, AssignedToUserId, userId, performedBy, nowUtc, note));

        AssignedToUserId = userId;
        UpdatedAtUtc = nowUtc;
    }

    public void Escalate(string performedBy, DateTime nowUtc, string? reason = null)
    {
        if (IsTerminal)
            throw new DomainRuleException($"El ticket esta en {Status}: no se escala.");
        if (EscalationLevel >= MaxEscalationLevel)
            throw new DomainRuleException(
                $"El ticket ya esta en el nivel maximo de escalamiento ({MaxEscalationLevel}).");

        var previous = EscalationLevel;
        EscalationLevel++;

        _events.Add(new TicketEvent(
            TicketChangeType.Escalated, previous.ToString(), EscalationLevel.ToString(), performedBy, nowUtc, reason));

        UpdatedAtUtc = nowUtc;
    }
}
