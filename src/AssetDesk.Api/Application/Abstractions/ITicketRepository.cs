using AssetDesk.Api.Domain.Entities;
using AssetDesk.Api.Domain.Enums;

namespace AssetDesk.Api.Application.Abstractions;

public record TicketQuery(
    TicketStatus? Status = null,
    TicketPriority? Priority = null,
    string? AssignedToUserId = null,
    int? AssetId = null,
    bool? OnlyRootTickets = null,
    string? Search = null,
    int Page = 1,
    int PageSize = 20);

public interface ITicketRepository
{
    Task<Ticket?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Con tracking y con las subtareas cargadas. Lo segundo no es opcional: la regla de
    /// "no cerrar con subtareas abiertas" se evalua sobre esa coleccion, y si viniera vacia
    /// por no haberla incluido, la regla pasaria en falso y el bug seria silencioso.
    /// </summary>
    Task<Ticket?> GetForUpdateAsync(int id, CancellationToken ct = default);

    Task<Ticket?> GetWithDetailsAsync(int id, CancellationToken ct = default);

    Task<PagedResult<Ticket>> SearchAsync(TicketQuery query, CancellationToken ct = default);

    void Add(Ticket ticket);
}
