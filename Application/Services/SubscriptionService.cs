using Microsoft.EntityFrameworkCore;
using SportClub.Application.DTOs;
using SportClub.Application.Interfaces;
using SportClub.Data;
using SportClub.Domain;

namespace SportClub.Application.Services;

public sealed class SubscriptionService : ISubscriptionService
{
    private readonly ApplicationDbContext _db;

    public SubscriptionService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<SubscriptionDto>> SellAsync(SellSubscriptionDto dto, CancellationToken ct = default)
    {
        var client = await _db.ClientProfiles
            .FirstOrDefaultAsync(cp => cp.Id == dto.ClientProfileId, ct);

        if (client is null)
            return Result<SubscriptionDto>.Fail($"Клієнта з ID {dto.ClientProfileId} не знайдено.");

        if (client.IsBlocked)
            return Result<SubscriptionDto>.Fail("Неможливо продати абонемент заблокованому клієнту.");

        var plan = await _db.Plans
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == dto.PlanId, ct);

        if (plan is null)
            return Result<SubscriptionDto>.Fail($"Тариф з ID {dto.PlanId} не знайдено.");

        if (plan.IsArchived)
            return Result<SubscriptionDto>.Fail($"Тариф '{plan.Name}' архівовано і недоступний для продажу.");

        decimal finalPrice = plan.Price;

        // 1. Застосування промокоду
        if (!string.IsNullOrWhiteSpace(dto.PromoCode))
        {
            var promo = await _db.PromoCodes
                .FirstOrDefaultAsync(p => p.Code == dto.PromoCode.ToUpper(), ct);

            if (promo is null || !promo.IsActive)
                return Result<SubscriptionDto>.Fail("Недійсний промокод.");

            if (promo.ExpiryDate.HasValue && promo.ExpiryDate.Value < DateTime.UtcNow)
                return Result<SubscriptionDto>.Fail("Термін дії промокоду минув.");

            if (promo.MaxUses.HasValue && promo.CurrentUses >= promo.MaxUses.Value)
                return Result<SubscriptionDto>.Fail("Ліміт використання промокоду вичерпано.");

            finalPrice -= finalPrice * (promo.DiscountPercentage / 100m);
            promo.CurrentUses++;
        }

        // 2. Списання бонусів
        if (dto.UseBonuses && client.BonusBalance > 0)
        {
            decimal bonusToUse = Math.Min(client.BonusBalance, finalPrice);
            finalPrice -= bonusToUse;
            client.BonusBalance -= bonusToUse;
        }

        // 3. Кешбек (5% від фінальної суми до оплати)
        decimal cashback = finalPrice * 0.05m;
        client.BonusBalance += cashback;

        var subscription = new Subscription
        {
            ClientProfileId = dto.ClientProfileId,
            PlanId = dto.PlanId,
            PurchaseDate = DateTime.UtcNow,
            ActivationDate = null,
            ExpirationDate = null,
            IsFrozen = false,
            FrozenDaysUsed = 0,
            FrozenSince = null,
            VisitsUsed = 0,
            FinalPrice = finalPrice,
            CreatedAt = DateTime.UtcNow
        };

        _db.Subscriptions.Add(subscription);
        await _db.SaveChangesAsync(ct);

        subscription.Plan = plan;
        return Result<SubscriptionDto>.Ok(MapToDto(subscription));
    }

    public async Task<Result<IReadOnlyList<SubscriptionDto>>> GetByClientAsync(int clientProfileId, CancellationToken ct = default)
    {
        var subscriptions = await _db.Subscriptions
            .Include(s => s.Plan)
            .Where(s => s.ClientProfileId == clientProfileId)
            .OrderByDescending(s => s.PurchaseDate)
            .AsNoTracking()
            .ToListAsync(ct);

        return Result<IReadOnlyList<SubscriptionDto>>.Ok(
            subscriptions.Select(MapToDto).ToList());
    }

    public async Task<Result<SubscriptionDto>> GetActiveAsync(int clientProfileId, CancellationToken ct = default)
    {
        var subscriptions = await _db.Subscriptions
            .Include(s => s.Plan)
            .Where(s => s.ClientProfileId == clientProfileId)
            .AsNoTracking()
            .ToListAsync(ct);

        var active = subscriptions
            .Where(s => s.IsActive || s.IsFrozen)
            .OrderBy(s => s.ExpirationDate ?? DateTime.MaxValue)
            .FirstOrDefault();

        return active is null
            ? Result<SubscriptionDto>.Fail("Активного абонемента не знайдено.")
            : Result<SubscriptionDto>.Ok(MapToDto(active));
    }

    public async Task<Result> FreezeSubscriptionAsync(int subscriptionId, int daysToFreeze, CancellationToken ct = default)
    {
        if (daysToFreeze <= 0)
            return Result.Fail("Кількість днів для заморозки має бути більшою за нуль.");

        var subscription = await _db.Subscriptions
            .Include(s => s.Plan)
            .FirstOrDefaultAsync(s => s.Id == subscriptionId, ct);

        if (subscription is null)
            return Result.Fail($"Абонемент з ID {subscriptionId} не знайдено.");

        if (subscription.IsFrozen)
            return Result.Fail("Абонемент вже заморожено.");

        if (!subscription.IsActive)
            return Result.Fail("Неможливо заморозити неактивний абонемент.");

        if (subscription.ActivationDate is null)
            return Result.Fail("Неможливо заморозити абонемент, який ще не активовано (не було жодного відвідування).");

        subscription.IsFrozen = true;
        subscription.FrozenSince = DateTime.UtcNow;
        subscription.FrozenDaysUsed += daysToFreeze;

        if (subscription.ExpirationDate.HasValue)
            subscription.ExpirationDate = subscription.ExpirationDate.Value.AddDays(daysToFreeze);

        await _db.SaveChangesAsync(ct);

        return Result.Ok();
    }

    public async Task<Result> UnfreezeSubscriptionAsync(int subscriptionId, CancellationToken ct = default)
    {
        var subscription = await _db.Subscriptions
            .Include(s => s.Plan)
            .FirstOrDefaultAsync(s => s.Id == subscriptionId, ct);

        if (subscription is null)
            return Result.Fail($"Абонемент з ID {subscriptionId} не знайдено.");

        if (!subscription.IsFrozen)
            return Result.Fail("Абонемент не заморожено.");

        subscription.IsFrozen = false;
        subscription.FrozenSince = null;

        await _db.SaveChangesAsync(ct);

        return Result.Ok();
    }

    public async Task<Result> CancelAsync(int subscriptionId, CancellationToken ct = default)
    {
        var subscription = await _db.Subscriptions
            .FirstOrDefaultAsync(s => s.Id == subscriptionId, ct);

        if (subscription is null)
            return Result.Fail($"Абонемент з ID {subscriptionId} не знайдено.");

        if (subscription.ActivationDate.HasValue && subscription.VisitsUsed > 0)
            return Result.Fail("Неможливо скасувати абонемент, яким вже користувалися. Зверніться до адміністратора.");

        _db.Subscriptions.Remove(subscription);
        await _db.SaveChangesAsync(ct);

        return Result.Ok();
    }

    public async Task<Result<SubscriptionDto>> BuySubscriptionOnlineAsync(BuyOnlineDto dto, CancellationToken ct = default)
    {
        var client = await _db.ClientProfiles
            .FirstOrDefaultAsync(cp => cp.Id == dto.ClientProfileId, ct);

        if (client is null)
            return Result<SubscriptionDto>.Fail($"Клієнта з ID {dto.ClientProfileId} не знайдено.");

        if (client.IsBlocked)
            return Result<SubscriptionDto>.Fail("Неможливо продати абонемент заблокованому клієнту.");

        var plan = await _db.Plans
            .FirstOrDefaultAsync(p => p.Id == dto.PlanId, ct);

        if (plan is null)
            return Result<SubscriptionDto>.Fail($"Тариф з ID {dto.PlanId} не знайдено.");

        if (plan.IsArchived)
            return Result<SubscriptionDto>.Fail($"Тариф '{plan.Name}' архівовано.");

        // Перевірка на дублювання: чи ця транзакція вже не була оброблена
        var duplicate = await _db.Subscriptions
            .AnyAsync(s => s.TransactionId == dto.TransactionId, ct);

        if (duplicate)
            return Result<SubscriptionDto>.Fail("Цю транзакцію вже оброблено. Повторне проведення платежу неможливе.");

        // Нараховуємо кешбек 5% за онлайн-оплату
        decimal cashback = plan.Price * 0.05m;
        client.BonusBalance += cashback;

        var subscription = new Subscription
        {
            ClientProfileId = dto.ClientProfileId,
            PlanId = dto.PlanId,
            PurchaseDate = DateTime.UtcNow,
            FinalPrice = plan.Price,
            TransactionId = dto.TransactionId,
            ActivationDate = null,
            ExpirationDate = null,
            IsFrozen = false,
            FrozenDaysUsed = 0,
            FrozenSince = null,
            VisitsUsed = 0,
            CreatedAt = DateTime.UtcNow
        };

        _db.Subscriptions.Add(subscription);
        await _db.SaveChangesAsync(ct);

        subscription.Plan = plan;
        return Result<SubscriptionDto>.Ok(MapToDto(subscription));
    }

    internal static SubscriptionDto MapToDto(Subscription s) =>
        new(
            s.Id,
            s.ClientProfileId,
            new PlanDto(
                s.Plan.Id,
                s.Plan.Name,
                s.Plan.Description,
                s.Plan.Price,
                s.Plan.PlanType,
                s.Plan.MaxVisits,
                s.Plan.DurationValue,
                s.Plan.DurationUnit,
                s.Plan.IsArchived,
                s.Plan.CreatedAt),
            s.PurchaseDate,
            s.ActivationDate,
            s.ExpirationDate,
            s.IsFrozen,
            s.FrozenDaysUsed,
            s.FrozenSince,
            s.VisitsUsed,
            s.IsActive,
            s.FinalPrice,
            s.TransactionId);
}
