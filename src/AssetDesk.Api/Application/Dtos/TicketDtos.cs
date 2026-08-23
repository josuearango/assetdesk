using System.ComponentModel.DataAnnotations;
using AssetDesk.Api.Domain.Entities;
using AssetDesk.Api.Domain.Enums;

namespace AssetDesk.Api.Application.Dtos;

// ---------- Requests ----------

public record CreateTicketRequest(
    [Required, MaxLength(200)] string Title,
    [MaxLength(4000)] string? Description,
    [Required] TicketPriority Priority,
    int? AssetId,
    int? ParentTicketId);

public record ChangeTicketStatusRequest(
    [Required] TicketStatus Status,
    [MaxLength(500)] string? Note);

public record ChangeTicketPriorityRequest(
    [Required] TicketPriority Priority,
    [MaxLength(500)] string? Note);

public record AssignTicketRequest(
    [Required, MaxLength(128)] string UserId,
    [MaxLength(500)] string? Note);

public record EscalateTicketRequest([MaxLength(500)] string? Reason);

// ---------- Responses ----------

public record TicketResponse(
    int Id,
    string Title,
    string? Description,
    TicketStatus Status,
    TicketPriority Priority,
    int? AssetId,
    string? AssignedToUserId,
    int? ParentTicketId,
    int EscalationLevel,
    bool IsTerminal,
    string CreatedBy,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    DateTime? ResolvedAtUtc,
    DateTime? ClosedAtUtc)
{
    public static TicketResponse From(Ticket t) => new(
        t.Id, t.Title, t.Description, t.Status, t.Priority, t.AssetId, t.AssignedToUserId,
        t.ParentTicketId, t.EscalationLevel, t.IsTerminal, t.CreatedBy, t.CreatedAtUtc,
        t.UpdatedAtUtc, t.ResolvedAtUtc, t.ClosedAtUtc);
}

public record TicketEventResponse(
    int Id,
    TicketChangeType ChangeType,
    string? FromValue,
    string? ToValue,
    string PerformedBy,
    DateTime OccurredAtUtc,
    string? Note)
{
    public static TicketEventResponse From(TicketEvent e) => new(
        e.Id, e.ChangeType, e.FromValue, e.ToValue, e.PerformedBy, e.OccurredAtUtc, e.Note);
}

public record TicketSubtaskResponse(int Id, string Title, TicketStatus Status, bool IsTerminal)
{
    public static TicketSubtaskResponse From(Ticket t) => new(t.Id, t.Title, t.Status, t.IsTerminal);
}

public record TicketDetailResponse(
    TicketResponse Ticket,
    AssetResponse? Asset,
    IReadOnlyList<TicketSubtaskResponse> Subtasks,
    int OpenSubtaskCount,
    IReadOnlyList<TicketEventResponse> Events)
{
    public static TicketDetailResponse From(Ticket t) => new(
        TicketResponse.From(t),
        t.Asset is null ? null : AssetResponse.From(t.Asset),
        [.. t.Subtasks.OrderBy(s => s.Id).Select(TicketSubtaskResponse.From)],
        t.OpenSubtaskCount,
        [.. t.Events.OrderByDescending(e => e.OccurredAtUtc).Select(TicketEventResponse.From)]);
}
