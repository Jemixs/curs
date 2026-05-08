using SportClub.Application.DTOs;
using SportClub.Domain;

namespace SportClub.Application.Interfaces;

public interface ISubscriptionService
{
    Task<Result<SubscriptionDto>> SellAsync(SellSubscriptionDto dto, CancellationToken ct = default);
    Task<Result<IReadOnlyList<SubscriptionDto>>> GetByClientAsync(int clientProfileId, CancellationToken ct = default);
    Task<Result<SubscriptionDto>> GetActiveAsync(int clientProfileId, CancellationToken ct = default);
    Task<Result> FreezeSubscriptionAsync(int subscriptionId, int daysToFreeze, CancellationToken ct = default);
    Task<Result> UnfreezeSubscriptionAsync(int subscriptionId, CancellationToken ct = default);
    Task<Result> CancelAsync(int subscriptionId, CancellationToken ct = default);

    Task<Result<SubscriptionDto>> BuySubscriptionOnlineAsync(BuyOnlineDto dto, CancellationToken ct = default);
}
