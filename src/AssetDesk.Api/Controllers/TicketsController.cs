using AssetDesk.Api.Application.Abstractions;
using AssetDesk.Api.Application.Dtos;
using AssetDesk.Api.Application.Services;
using AssetDesk.Api.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace AssetDesk.Api.Controllers;

/// <summary>Tickets de soporte y sus subtareas.</summary>
[ApiController]
[Route("api/tickets")]
[Produces("application/json")]
public class TicketsController(ITicketService service) : ControllerBase
{
    /// <summary>
    /// Crea un ticket. Si trae AssetId, el activo tiene que estar activo: uno dado de baja
    /// no acepta tickets nuevos y la peticion sale 409. Si trae ParentTicketId, el ticket
    /// nace como subtarea de ese padre.
    /// </summary>
    [HttpPost]
    [ProducesResponseType<TicketResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TicketResponse>> Create(
        [FromBody] CreateTicketRequest request, CancellationToken ct)
    {
        var created = await service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpGet]
    [ProducesResponseType<PagedResult<TicketResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<TicketResponse>>> Search(
        [FromQuery] TicketStatus? status,
        [FromQuery] TicketPriority? priority,
        [FromQuery] string? assignedToUserId,
        [FromQuery] int? assetId,
        [FromQuery] bool? onlyRootTickets,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await service.SearchAsync(
            new TicketQuery(status, priority, assignedToUserId, assetId, onlyRootTickets, search, page, pageSize), ct);
        return Ok(result);
    }

    /// <summary>Ticket con su activo, sus subtareas y su bitacora completa.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<TicketDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TicketDetailResponse>> GetById(int id, CancellationToken ct)
        => Ok(await service.GetAsync(id, ct));

    /// <summary>
    /// Cambia el estado. Rechaza transiciones invalidas y rechaza cerrar un ticket que
    /// todavia tiene subtareas abiertas; en ambos casos responde 409 con el motivo.
    /// </summary>
    [HttpPut("{id:int}/status")]
    [ProducesResponseType<TicketResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TicketResponse>> ChangeStatus(
        int id, [FromBody] ChangeTicketStatusRequest request, CancellationToken ct)
        => Ok(await service.ChangeStatusAsync(id, request, ct));

    [HttpPut("{id:int}/priority")]
    [ProducesResponseType<TicketResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TicketResponse>> ChangePriority(
        int id, [FromBody] ChangeTicketPriorityRequest request, CancellationToken ct)
        => Ok(await service.ChangePriorityAsync(id, request, ct));

    [HttpPost("{id:int}/assignment")]
    [ProducesResponseType<TicketResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TicketResponse>> Assign(
        int id, [FromBody] AssignTicketRequest request, CancellationToken ct)
        => Ok(await service.AssignAsync(id, request, ct));

    /// <summary>Sube un nivel de escalamiento, hasta el maximo definido en el dominio.</summary>
    [HttpPost("{id:int}/escalation")]
    [ProducesResponseType<TicketResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TicketResponse>> Escalate(
        int id, [FromBody] EscalateTicketRequest? request, CancellationToken ct)
        => Ok(await service.EscalateAsync(id, request ?? new EscalateTicketRequest(null), ct));
}
