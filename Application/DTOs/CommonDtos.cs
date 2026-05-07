namespace SportClub.Application.DTOs;

public sealed record CheckInHistoryItemDto(
    int Id,
    DateTime CheckedInAt,
    string PlanName,
    string BarcodeSnapshot);

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize)
{
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}

public sealed record ServerDataRequest(
    int Page,
    int PageSize,
    string? SearchTerm);
