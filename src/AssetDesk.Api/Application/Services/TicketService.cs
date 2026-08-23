using AssetDesk.Api.Application.Abstractions;
using AssetDesk.Api.Application.Dtos;
using AssetDesk.Api.Domain.Entities;
using AssetDesk.Api.Domain.Exceptions;

namespace AssetDesk.Api.Application.Services;

public class TicketService(
    ITicketRepository tickets,
    IAssetRepository assets,
    IUnitOfWork uow,
    ICurrentUser currentUser,
    TimeProvider clock) : ITicketService
{
    private DateTime NowUtc => clock.GetUtcNow().UtcDateTime;

    public async Task<TicketResponse> CreateAsync(CreateTicketRequest request, CancellationToken ct = default)
    {
        // Activo y padre se cargan CON tracking. Si vinieran con AsNoTracking, al colgarlos
        // del ticket nuevo EF los tomaria por entidades nuevas e intentaria insertarlos otra vez.
        Asset? asset = null;
        if (request.AssetId is { } assetId)
        {
            asset = await assets.GetForUpdateAsync(assetId, ct)
                    ?? throw new NotFoundException(nameof(Asset), assetId);
        }

        Ticket? parent = null;
        if (request.ParentTicketId is { } parentId)
        {
            parent = await tickets.GetForUpdateAsync(parentId, ct)
                     ?? throw new NotFoundException(nameof(Ticket), parentId);
        }

        // La validacion de "activo dado de baja" y la de anidamiento las hace Ticket.Create.
        var ticket = Ticket.Create(
            request.Title,
            request.Description,
            request.Priority,
            asset,
            parent,
            currentUser.UserId,
            NowUtc);

        tickets.Add(ticket);
        await uow.SaveChangesAsync(ct);

        return TicketResponse.From(ticket);
    }

    public async Task<TicketDetailResponse> GetAsync(int id, CancellationToken ct = default)
    {
        var ticket = await tickets.GetWithDetailsAsync(id, ct)
                     ?? throw new NotFoundException(nameof(Ticket), id);
        return TicketDetailResponse.From(ticket);
    }

    public async Task<PagedResult<TicketResponse>> SearchAsync(TicketQuery query, CancellationToken ct = default)
    {
        var page = await tickets.SearchAsync(query, ct);
        return new PagedResult<TicketResponse>(
            [.. page.Items.Select(TicketResponse.From)], page.TotalCount, page.Page, page.PageSize);
    }

    public async Task<TicketResponse> ChangeStatusAsync(int id, ChangeTicketStatusRequest request, CancellationToken ct = default)
    {
        var ticket = await tickets.GetForUpdateAsync(id, ct)
                     ?? throw new NotFoundException(nameof(Ticket), id);

        ticket.ChangeStatus(request.Status, currentUser.UserId, NowUtc, request.Note);
        await uow.SaveChangesAsync(ct);

        return TicketResponse.From(ticket);
    }

    public async Task<TicketResponse> ChangePriorityAsync(int id, ChangeTicketPriorityRequest request, CancellationToken ct = default)
    {
        var ticket = await tickets.GetForUpdateAsync(id, ct)
                     ?? throw new NotFoundException(nameof(Ticket), id);

        ticket.ChangePriority(request.Priority, currentUser.UserId, NowUtc, request.Note);
        await uow.SaveChangesAsync(ct);

        return TicketResponse.From(ticket);
    }

    public async Task<TicketResponse> AssignAsync(int id, AssignTicketRequest request, CancellationToken ct = default)
    {
        var ticket = await tickets.GetForUpdateAsync(id, ct)
                     ?? throw new NotFoundException(nameof(Ticket), id);

        ticket.Assign(request.UserId, currentUser.UserId, NowUtc, request.Note);
        await uow.SaveChangesAsync(ct);

        return TicketResponse.From(ticket);
    }

    public async Task<TicketResponse> EscalateAsync(int id, EscalateTicketRequest request, CancellationToken ct = default)
    {
        var ticket = await tickets.GetForUpdateAsync(id, ct)
                     ?? throw new NotFoundException(nameof(Ticket), id);

        ticket.Escalate(currentUser.UserId, NowUtc, request.Reason);
        await uow.SaveChangesAsync(ct);

        return TicketResponse.From(ticket);
    }
}
