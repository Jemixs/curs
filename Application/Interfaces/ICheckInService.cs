using SportClub.Application.DTOs;
using SportClub.Domain;

namespace SportClub.Application.Interfaces;

public interface ICheckInService
{
    Task<Result<CheckInResultDto>> ProcessScanAsync(string barcode, CancellationToken ct = default);
    Task<Result<PagedResult<CheckInHistoryItemDto>>> GetHistoryAsync(int clientProfileId, int page, int pageSize, CancellationToken ct = default);
    Task<Result<IReadOnlyList<CheckInHistoryItemDto>>> GetTodayAsync(CancellationToken ct = default);
}
