using SportClub.Application.DTOs;
using SportClub.Domain;

namespace SportClub.Application.Interfaces;

public interface IClientProfileService
{
    Task<Result<ClientDetailDto>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Result<ClientDetailDto>> GetByBarcodeAsync(string barcode, CancellationToken ct = default);
    Task<Result<ClientDetailDto>> GetByUserIdAsync(int userId, CancellationToken ct = default);
    Task<Result<PagedResult<ClientListItemDto>>> GetPagedAsync(ServerDataRequest request, CancellationToken ct = default);
    Task<Result<ClientDetailDto>> CreateAsync(RegisterClientDto dto, CancellationToken ct = default);
    Task<Result<ClientDetailDto>> UpdateAsync(UpdateClientDto dto, CancellationToken ct = default);
    Task<Result> DeleteAsync(int id, CancellationToken ct = default);
    Task<Result> ToggleBlockAsync(int id, CancellationToken ct = default);
}
