using SportClub.Domain;

namespace SportClub.Application.DTOs;

public sealed record SellSubscriptionDto(
    int ClientProfileId,
    int PlanId);

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
    bool IsActive);

public sealed record CheckInResultDto(
    CheckInResult Status,
    string Message,
    string? ClientFullName,
    string? PlanName,
    int? VisitsLeft);
