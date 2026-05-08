using Microsoft.EntityFrameworkCore;
using SportClub.Application.DTOs;
using SportClub.Application.Interfaces;
using SportClub.Data;
using SportClub.Domain;

namespace SportClub.Application.Services;

public sealed class CheckInService : ICheckInService
{
    private static readonly TimeSpan DebounceWindow = TimeSpan.FromMinutes(5);

    private readonly ApplicationDbContext _db;

    public CheckInService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<CheckInResultDto>> ProcessScanAsync(string barcode, CancellationToken ct = default)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(ct);

        try
        {
            var client = await _db.ClientProfiles
                .Include(cp => cp.User)
                .FirstOrDefaultAsync(cp => cp.Barcode == barcode, ct);

            if (client is null)
                return BuildResult(CheckInResult.ClientNotFound,
                    $"Клієнта з баркодом '{barcode}' не знайдено.");

            if (client.IsBlocked)
                return BuildResult(CheckInResult.ClientBlocked,
                    $"{client.User.FullName}: профіль заблоковано.");

            var debounceThreshold = DateTime.UtcNow.Subtract(DebounceWindow);
            var recentCheckIn = await _db.CheckIns
                .Where(ci => ci.ClientProfileId == client.Id && ci.CheckedInAt >= debounceThreshold)
                .OrderByDescending(ci => ci.CheckedInAt)
                .FirstOrDefaultAsync(ct);

            if (recentCheckIn is not null)
                return BuildResult(CheckInResult.AlreadyCheckedIn,
                    $"{client.User.FullName}: вхід вже зареєстровано менше 5 хвилин тому.");

            var subscriptions = await _db.Subscriptions
                .Include(s => s.Plan)
                .Where(s => s.ClientProfileId == client.Id)
                .ToListAsync(ct);

            var frozen = subscriptions.FirstOrDefault(s => s.IsFrozen);
            if (frozen is not null && !subscriptions.Any(s => s.IsActive))
                return BuildResult(CheckInResult.SubscriptionFrozen,
                    $"{client.User.FullName}: абонемент заморожено. Розморозьте його для відновлення доступу.");

            var activeSubscription = subscriptions
                .Where(s => s.IsActive)
                .OrderBy(s => s.ExpirationDate ?? DateTime.MaxValue)
                .FirstOrDefault();

            if (activeSubscription is null)
            {
                var hasExpired = subscriptions.Any(s =>
                    s.ExpirationDate.HasValue && s.ExpirationDate < DateTime.UtcNow);

                var hasNoVisits = subscriptions.Any(s =>
                    s.Plan.PlanType == PlanType.LimitedVisits &&
                    s.Plan.MaxVisits.HasValue &&
                    s.VisitsUsed >= s.Plan.MaxVisits.Value);

                if (hasExpired || hasNoVisits)
                    return BuildResult(CheckInResult.SubscriptionExpired,
                        $"{client.User.FullName}: абонемент вичерпано або прострочено.");

                return BuildResult(CheckInResult.NoActiveSubscription,
                    $"{client.User.FullName}: активного абонемента не знайдено.");
            }

            var now = DateTime.UtcNow;

            if (activeSubscription.ActivationDate is null)
            {
                activeSubscription.ActivationDate = now;
                activeSubscription.ExpirationDate = activeSubscription.Plan.DurationUnit == PlanDurationUnit.Months
                    ? now.AddMonths(activeSubscription.Plan.DurationValue)
                    : now.AddDays(activeSubscription.Plan.DurationValue);
            }

            activeSubscription.VisitsUsed++;

            var checkIn = new CheckIn
            {
                ClientProfileId = client.Id,
                SubscriptionId = activeSubscription.Id,
                CheckedInAt = now,
                BarcodeSnapshot = barcode
            };

            _db.CheckIns.Add(checkIn);
            await _db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            int? visitsLeft = activeSubscription.Plan.PlanType == PlanType.LimitedVisits
                ? activeSubscription.Plan.MaxVisits - activeSubscription.VisitsUsed
                : null;

            return BuildResult(
                CheckInResult.Success,
                $"Ласкаво просимо, {client.User.FullName}!",
                client.User.FullName,
                activeSubscription.Plan.Name,
                visitsLeft);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<Result<PagedResult<CheckInHistoryItemDto>>> GetHistoryAsync(
        int clientProfileId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.CheckIns
            .Include(ci => ci.Subscription)
                .ThenInclude(s => s.Plan)
            .Where(ci => ci.ClientProfileId == clientProfileId)
            .AsNoTracking();

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(ci => ci.CheckedInAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ci => new CheckInHistoryItemDto(
                ci.Id,
                ci.CheckedInAt,
                ci.Subscription.Plan.Name,
                ci.BarcodeSnapshot))
            .ToListAsync(ct);

        return Result<PagedResult<CheckInHistoryItemDto>>.Ok(
            new PagedResult<CheckInHistoryItemDto>(items, total, page, pageSize));
    }

    public async Task<Result<IReadOnlyList<CheckInHistoryItemDto>>> GetTodayAsync(CancellationToken ct = default)
    {
        // Використовуємо 24-годинне вікно від початку поточної доби UTC
        var todayStart = DateTime.UtcNow.Date;
        var nextDay = todayStart.AddDays(1);

        var items = await _db.CheckIns
            .Include(ci => ci.Subscription)
                .ThenInclude(s => s.Plan)
            .Include(ci => ci.ClientProfile)
                .ThenInclude(cp => cp.User)
            .Where(ci => ci.CheckedInAt >= todayStart && ci.CheckedInAt < nextDay)
            .OrderByDescending(ci => ci.CheckedInAt)
            .AsNoTracking()
            .Select(ci => new CheckInHistoryItemDto(
                ci.Id,
                ci.CheckedInAt,
                ci.Subscription.Plan.Name,
                $"{ci.ClientProfile.User.FullName} ({ci.BarcodeSnapshot})"))
            .ToListAsync(ct);

        return Result<IReadOnlyList<CheckInHistoryItemDto>>.Ok(items);
    }

    private static Result<CheckInResultDto> BuildResult(
        CheckInResult status,
        string message,
        string? clientFullName = null,
        string? planName = null,
        int? visitsLeft = null) =>
        Result<CheckInResultDto>.Ok(new CheckInResultDto(status, message, clientFullName, planName, visitsLeft));
}
