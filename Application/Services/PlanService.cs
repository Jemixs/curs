using Microsoft.EntityFrameworkCore;
using SportClub.Application.DTOs;
using SportClub.Application.Interfaces;
using SportClub.Data;
using SportClub.Domain;

namespace SportClub.Application.Services;

public sealed class PlanService : IPlanService
{
    private readonly ApplicationDbContext _db;

    public PlanService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<IReadOnlyList<PlanDto>>> GetAllAsync(CancellationToken ct = default)
    {
        var plans = await _db.Plans
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .Select(p => MapToDto(p))
            .ToListAsync(ct);

        return Result<IReadOnlyList<PlanDto>>.Ok(plans);
    }

    public async Task<Result<PlanDto>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var plan = await _db.Plans
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        return plan is null
            ? Result<PlanDto>.Fail($"Тариф з ID {id} не знайдено.")
            : Result<PlanDto>.Ok(MapToDto(plan));
    }

    public async Task<Result<PlanDto>> CreateAsync(CreatePlanDto dto, CancellationToken ct = default)
    {
        var validationError = Validate(dto.PlanType, dto.MaxVisits, dto.Price, dto.DurationValue);
        if (validationError is not null)
            return Result<PlanDto>.Fail(validationError);

        if (await _db.Plans.AnyAsync(p => p.Name == dto.Name, ct))
            return Result<PlanDto>.Fail($"Тариф з назвою '{dto.Name}' вже існує.");

        var plan = new Plan
        {
            Name = dto.Name,
            Description = dto.Description,
            Price = dto.Price,
            PlanType = dto.PlanType,
            MaxVisits = dto.PlanType == PlanType.LimitedVisits ? dto.MaxVisits : null,
            DurationValue = dto.DurationValue,
            DurationUnit = dto.DurationUnit,
            IsArchived = false,
            CreatedAt = DateTime.UtcNow
        };

        _db.Plans.Add(plan);
        await _db.SaveChangesAsync(ct);

        return Result<PlanDto>.Ok(MapToDto(plan));
    }

    public async Task<Result<PlanDto>> UpdateAsync(UpdatePlanDto dto, CancellationToken ct = default)
    {
        var plan = await _db.Plans
            .FirstOrDefaultAsync(p => p.Id == dto.Id, ct);

        if (plan is null)
            return Result<PlanDto>.Fail($"Тариф з ID {dto.Id} не знайдено.");

        var validationError = Validate(dto.PlanType, dto.MaxVisits, dto.Price, dto.DurationValue);
        if (validationError is not null)
            return Result<PlanDto>.Fail(validationError);

        if (await _db.Plans.AnyAsync(p => p.Name == dto.Name && p.Id != dto.Id, ct))
            return Result<PlanDto>.Fail($"Тариф з назвою '{dto.Name}' вже існує.");

        plan.Name = dto.Name;
        plan.Description = dto.Description;
        plan.Price = dto.Price;
        plan.PlanType = dto.PlanType;
        plan.MaxVisits = dto.PlanType == PlanType.LimitedVisits ? dto.MaxVisits : null;
        plan.DurationValue = dto.DurationValue;
        plan.DurationUnit = dto.DurationUnit;

        await _db.SaveChangesAsync(ct);

        return Result<PlanDto>.Ok(MapToDto(plan));
    }

    public async Task<Result> ArchiveAsync(int id, CancellationToken ct = default)
    {
        var plan = await _db.Plans
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (plan is null)
            return Result.Fail($"Тариф з ID {id} не знайдено.");

        plan.IsArchived = true;
        await _db.SaveChangesAsync(ct);

        return Result.Ok();
    }

    public async Task<Result> RestoreAsync(int id, CancellationToken ct = default)
    {
        var plan = await _db.Plans
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (plan is null)
            return Result.Fail($"Тариф з ID {id} не знайдено.");

        plan.IsArchived = false;
        await _db.SaveChangesAsync(ct);

        return Result.Ok();
    }

    private static string? Validate(PlanType type, int? maxVisits, decimal price, int durationValue)
    {
        if (price <= 0)
            return "Ціна тарифу повинна бути більше нуля.";

        if (durationValue <= 0)
            return "Тривалість тарифу повинна бути більше нуля.";

        if (type == PlanType.LimitedVisits && (maxVisits is null || maxVisits <= 0))
            return "Для лімітованого тарифу необхідно вказати кількість занять (більше нуля).";

        return null;
    }

    private static PlanDto MapToDto(Plan p) =>
        new(p.Id, p.Name, p.Description, p.Price, p.PlanType,
            p.MaxVisits, p.DurationValue, p.DurationUnit, p.IsArchived, p.CreatedAt);
}
