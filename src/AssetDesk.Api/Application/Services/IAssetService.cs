using AssetDesk.Api.Application.Abstractions;
using AssetDesk.Api.Application.Dtos;

namespace AssetDesk.Api.Application.Services;

public interface IAssetService
{
    Task<AssetResponse> CreateAsync(CreateAssetRequest request, CancellationToken ct = default);
    Task<AssetDetailResponse> GetAsync(int id, CancellationToken ct = default);
    Task<PagedResult<AssetResponse>> SearchAsync(AssetQuery query, CancellationToken ct = default);
    Task<AssetResponse> AssignAsync(int id, AssignAssetRequest request, CancellationToken ct = default);
    Task<AssetResponse> ReturnAsync(int id, string? note, CancellationToken ct = default);
    Task<AssetResponse> ChangeStatusAsync(int id, ChangeAssetStatusRequest request, CancellationToken ct = default);
    Task<AssetResponse> DecommissionAsync(int id, DecommissionAssetRequest request, CancellationToken ct = default);
}
