using SportClub.Domain;

namespace SportClub.Application.DTOs;

public sealed record SellSubscriptionDto(
    int ClientProfileId,
    int PlanId,
    string? PromoCode = null,
    bool UseBonuses = false);

public sealed record BuyOnlineDto(
    int ClientProfileId,
    int PlanId,
    string TransactionId);

public sealed record SubscriptionDto(
    int Id,
    int ClientProfileId,
    PlanDto Plan,
    DateTime PurchaseDate,
    DateTime? ActivationDate,
    DateTime? ExpirationDate,
    bool IsFrozen,
    int FrozenDaysUsed,
    DateTime? FrozenSince,
    int VisitsUsed,
    bool IsActive,
    decimal FinalPrice,
    string? TransactionId = null);

public sealed record CheckInResultDto(
    CheckInResult Status,
    string Message,
    string? ClientFullName,
    string? PlanName,
    int? VisitsLeft);
