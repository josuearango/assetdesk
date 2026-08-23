using AssetDesk.Api.Application.Abstractions;
using AssetDesk.Api.Application.Dtos;

namespace AssetDesk.Api.Application.Services;

public interface ITicketService
{
    Task<TicketResponse> CreateAsync(CreateTicketRequest request, CancellationToken ct = default);
    Task<TicketDetailResponse> GetAsync(int id, CancellationToken ct = default);
    Task<PagedResult<TicketResponse>> SearchAsync(TicketQuery query, CancellationToken ct = default);
    Task<TicketResponse> ChangeStatusAsync(int id, ChangeTicketStatusRequest request, CancellationToken ct = default);
    Task<TicketResponse> ChangePriorityAsync(int id, ChangeTicketPriorityRequest request, CancellationToken ct = default);
    Task<TicketResponse> AssignAsync(int id, AssignTicketRequest request, CancellationToken ct = default);
    Task<TicketResponse> EscalateAsync(int id, EscalateTicketRequest request, CancellationToken ct = default);
}
