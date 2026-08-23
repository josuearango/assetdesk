using AssetDesk.Api.Application.Abstractions;
using AssetDesk.Api.Domain.Entities;
using AssetDesk.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AssetDesk.Api.Infrastructure.Repositories;

public class TicketRepository(AppDbContext db) : ITicketRepository
{
    public Task<Ticket?> GetByIdAsync(int id, CancellationToken ct = default) =>
        db.Tickets.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, ct);

    public Task<Ticket?> GetForUpdateAsync(int id, CancellationToken ct = default) =>
        db.Tickets
            .Include(t => t.Subtasks) // obligatorio: ver el comentario en ITicketRepository
            .FirstOrDefaultAsync(t => t.Id == id, ct);

    public Task<Ticket?> GetWithDetailsAsync(int id, CancellationToken ct = default) =>
        db.Tickets
            .AsNoTracking()
            .Include(t => t.Asset)
            .Include(t => t.Subtasks)
            .Include(t => t.Events)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<PagedResult<Ticket>> SearchAsync(TicketQuery query, CancellationToken ct = default)
    {
        var q = db.Tickets.AsNoTracking().AsQueryable();

        if (query.Status is not null)
            q = q.Where(t => t.Status == query.Status);
        if (query.Priority is not null)
            q = q.Where(t => t.Priority == query.Priority);
        if (!string.IsNullOrWhiteSpace(query.AssignedToUserId))
            q = q.Where(t => t.AssignedToUserId == query.AssignedToUserId);
        if (query.AssetId is not null)
            q = q.Where(t => t.AssetId == query.AssetId);
        if (query.OnlyRootTickets == true)
            q = q.Where(t => t.ParentTicketId == null);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            q = q.Where(t =>
                EF.Functions.Like(t.Title, $"%{term}%") ||
                (t.Description != null && EF.Functions.Like(t.Description, $"%{term}%")));
        }

        var total = await q.CountAsync(ct);

        var page = Math.Max(1, query.Page);
        var size = Math.Clamp(query.PageSize, 1, 100);

        // Prioridad descendente primero: la cola de trabajo de un tecnico se lee asi,
        // lo mas critico y mas viejo arriba.
        var items = await q
            .OrderByDescending(t => t.Priority)
            .ThenBy(t => t.CreatedAtUtc)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync(ct);

        return new PagedResult<Ticket>(items, total, page, size);
    }

    public void Add(Ticket ticket) => db.Tickets.Add(ticket);
}
