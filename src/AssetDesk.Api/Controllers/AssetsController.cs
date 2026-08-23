using AssetDesk.Api.Application.Abstractions;
using AssetDesk.Api.Application.Dtos;
using AssetDesk.Api.Application.Services;
using AssetDesk.Api.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace AssetDesk.Api.Controllers;

/// <summary>
/// Activos de TI.
/// <para>
/// Las operaciones que no son un simple "editar campos" (asignar, devolver, dar de baja)
/// tienen su propia ruta en lugar de un PATCH generico. Es a proposito: cada una arrastra
/// efectos y deja un renglon distinto en la bitacora, y la intencion del cliente tiene que
/// quedar explicita en la URL, no adivinarse a partir de que campos cambiaron.
/// </para>
/// </summary>
[ApiController]
[Route("api/assets")]
[Produces("application/json")]
public class AssetsController(IAssetService service) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<AssetResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AssetResponse>> Create(
        [FromBody] CreateAssetRequest request, CancellationToken ct)
    {
        var created = await service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpGet]
    [ProducesResponseType<PagedResult<AssetResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AssetResponse>>> Search(
        [FromQuery] AssetStatus? status,
        [FromQuery] AssetCategory? category,
        [FromQuery] string? assignedToUserId,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await service.SearchAsync(
            new AssetQuery(status, category, assignedToUserId, search, page, pageSize), ct);
        return Ok(result);
    }

    /// <summary>Devuelve el activo con su historial completo, lo mas reciente primero.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<AssetDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssetDetailResponse>> GetById(int id, CancellationToken ct)
        => Ok(await service.GetAsync(id, ct));

    [HttpPost("{id:int}/assignment")]
    [ProducesResponseType<AssetResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AssetResponse>> Assign(
        int id, [FromBody] AssignAssetRequest request, CancellationToken ct)
        => Ok(await service.AssignAsync(id, request, ct));

    [HttpPost("{id:int}/return")]
    [ProducesResponseType<AssetResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AssetResponse>> Return(
        int id, [FromBody] ReturnAssetRequest? request, CancellationToken ct)
        => Ok(await service.ReturnAsync(id, request?.Note, ct));

    [HttpPut("{id:int}/status")]
    [ProducesResponseType<AssetResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AssetResponse>> ChangeStatus(
        int id, [FromBody] ChangeAssetStatusRequest request, CancellationToken ct)
        => Ok(await service.ChangeStatusAsync(id, request, ct));

    /// <summary>
    /// Baja del activo. Es terminal y no es un DELETE: la fila se conserva porque el
    /// historial y los tickets que la referencian tienen que seguir existiendo.
    /// </summary>
    [HttpPost("{id:int}/decommission")]
    [ProducesResponseType<AssetResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AssetResponse>> Decommission(
        int id, [FromBody] DecommissionAssetRequest? request, CancellationToken ct)
        => Ok(await service.DecommissionAsync(id, request ?? new DecommissionAssetRequest(null), ct));
}
