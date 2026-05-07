using SportClub.Application.DTOs;
using SportClub.Domain;

namespace SportClub.Application.Interfaces;

public interface ISubscriptionService
{
    Task<Result<SubscriptionDto>> SellAsync(SellSubscriptionDto dto, CancellationToken ct = default);
    Task<Result<IReadOnlyList<SubscriptionDto>>> GetByClientAsync(int clientProfileId, CancellationToken ct = default);
    Task<Result<SubscriptionDto>> GetActiveAsync(int clientProfileId, CancellationToken ct = default);
    Task<Result> FreezeAsync(int subscriptionId, CancellationToken ct = default);
    Task<Result> UnfreezeAsync(int subscriptionId, CancellationToken ct = default);
    Task<Result> CancelAsync(int subscriptionId, CancellationToken ct = default);
}
