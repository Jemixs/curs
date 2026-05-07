using SportClub.Domain;

namespace SportClub.Application.DTOs;

public sealed record CreatePlanDto(
    string Name,
    string? Description,
    decimal Price,
    PlanType PlanType,
    int? MaxVisits,
    int DurationValue,
    PlanDurationUnit DurationUnit);

public sealed record UpdatePlanDto(
    int Id,
    string Name,
    string? Description,
    decimal Price,
    PlanType PlanType,
    int? MaxVisits,
    int DurationValue,
    PlanDurationUnit DurationUnit);

public sealed record PlanDto(
    int Id,
    string Name,
    string? Description,
    decimal Price,
    PlanType PlanType,
    int? MaxVisits,
    int DurationValue,
    PlanDurationUnit DurationUnit,
    bool IsArchived,
    DateTime CreatedAt);
