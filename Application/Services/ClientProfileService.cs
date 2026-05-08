using Microsoft.EntityFrameworkCore;
using SportClub.Application.DTOs;
using SportClub.Application.Interfaces;
using SportClub.Data;
using SportClub.Domain;

namespace SportClub.Application.Services;

public sealed class ClientProfileService : IClientProfileService
{
    private readonly ApplicationDbContext _db;

    public ClientProfileService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<ClientDetailDto>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var profile = await QueryWithDetails()
            .FirstOrDefaultAsync(cp => cp.Id == id, ct);

        return profile is null
            ? Result<ClientDetailDto>.Fail($"Клієнта з ID {id} не знайдено.")
            : Result<ClientDetailDto>.Ok(MapToDetail(profile));
    }

    public async Task<Result<ClientDetailDto>> GetByBarcodeAsync(string barcode, CancellationToken ct = default)
    {
        var profile = await QueryWithDetails()
            .FirstOrDefaultAsync(cp => cp.Barcode == barcode, ct);

        return profile is null
            ? Result<ClientDetailDto>.Fail($"Клієнта з баркодом '{barcode}' не знайдено.")
            : Result<ClientDetailDto>.Ok(MapToDetail(profile));
    }

    public async Task<Result<ClientDetailDto>> GetByUserIdAsync(int userId, CancellationToken ct = default)
    {
        var profile = await QueryWithDetails()
            .FirstOrDefaultAsync(cp => cp.UserId == userId, ct);

        return profile is null
            ? Result<ClientDetailDto>.Fail($"Профіль для користувача ID {userId} не знайдено.")
            : Result<ClientDetailDto>.Ok(MapToDetail(profile));
    }

    public async Task<Result<PagedResult<ClientListItemDto>>> GetPagedAsync(ServerDataRequest request, CancellationToken ct = default)
    {
        var query = _db.ClientProfiles
            .Include(cp => cp.User)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.ToLower();
            query = query.Where(cp =>
                cp.User.FullName.ToLower().Contains(term) ||
                cp.Phone.Contains(term) ||
                cp.Barcode.Contains(term) ||
                cp.User.Email.ToLower().Contains(term));
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(cp => cp.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(cp => new ClientListItemDto(
                cp.Id,
                cp.UserId,
                cp.User.FullName,
                cp.User.Email,
                cp.Phone,
                cp.Barcode,
                cp.IsBlocked,
                cp.CreatedAt))
            .ToListAsync(ct);

        return Result<PagedResult<ClientListItemDto>>.Ok(
            new PagedResult<ClientListItemDto>(items, total, request.Page, request.PageSize));
    }

    public async Task<Result<ClientDetailDto>> CreateAsync(RegisterClientDto dto, CancellationToken ct = default)
    {
        if (await _db.Users.AnyAsync(u => u.Email == dto.Email, ct))
            return Result<ClientDetailDto>.Fail($"Email '{dto.Email}' вже зареєстровано.");

        if (await _db.ClientProfiles.AnyAsync(cp => cp.Phone == dto.Phone, ct))
            return Result<ClientDetailDto>.Fail($"Телефон '{dto.Phone}' вже використовується.");

        var barcode = await GenerateUniqueBarcodeAsync(ct);

        var user = new User
        {
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = UserRole.Client,
            FullName = dto.FullName,
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        var profile = new ClientProfile
        {
            UserId = user.Id,
            Phone = dto.Phone,
            Barcode = barcode,
            DateOfBirth = dto.DateOfBirth,
            Notes = dto.Notes,
            IsBlocked = false,
            CreatedAt = DateTime.UtcNow
        };

        _db.ClientProfiles.Add(profile);
        await _db.SaveChangesAsync(ct);

        return await GetByIdAsync(profile.Id, ct);
    }

    public async Task<Result<ClientDetailDto>> UpdateAsync(UpdateClientDto dto, CancellationToken ct = default)
    {
        var profile = await _db.ClientProfiles
            .Include(cp => cp.User)
            .FirstOrDefaultAsync(cp => cp.Id == dto.Id, ct);

        if (profile is null)
            return Result<ClientDetailDto>.Fail($"Клієнта з ID {dto.Id} не знайдено.");

        if (await _db.Users.AnyAsync(u => u.Email == dto.Email && u.Id != profile.UserId, ct))
            return Result<ClientDetailDto>.Fail($"Email '{dto.Email}' вже використовується.");

        if (await _db.ClientProfiles.AnyAsync(cp => cp.Phone == dto.Phone && cp.Id != dto.Id, ct))
            return Result<ClientDetailDto>.Fail($"Телефон '{dto.Phone}' вже використовується.");

        profile.User.FullName = dto.FullName;
        profile.User.Email = dto.Email;
        profile.Phone = dto.Phone;
        profile.DateOfBirth = dto.DateOfBirth;
        profile.Notes = dto.Notes;
        profile.IsBlocked = dto.IsBlocked;

        await _db.SaveChangesAsync(ct);

        return await GetByIdAsync(profile.Id, ct);
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct = default)
    {
        var profile = await _db.ClientProfiles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(cp => cp.Id == id, ct);

        if (profile is null)
            return Result.Fail($"Клієнта з ID {id} не знайдено.");

        _db.Entry(profile).Property("IsDeleted").CurrentValue = true;
        await _db.SaveChangesAsync(ct);

        return Result.Ok();
    }

    public async Task<Result> ToggleBlockAsync(int id, CancellationToken ct = default)
    {
        var profile = await _db.ClientProfiles
            .FirstOrDefaultAsync(cp => cp.Id == id, ct);

        if (profile is null)
            return Result.Fail($"Клієнта з ID {id} не знайдено.");

        profile.IsBlocked = !profile.IsBlocked;
        await _db.SaveChangesAsync(ct);

        return Result.Ok();
    }

    private IQueryable<ClientProfile> QueryWithDetails() =>
        _db.ClientProfiles
            .Include(cp => cp.User)
            .Include(cp => cp.Subscriptions)
                .ThenInclude(s => s.Plan)
            .AsNoTracking();

    private async Task<string> GenerateUniqueBarcodeAsync(CancellationToken ct)
    {
        string barcode;
        do
        {
            var number = await _db.ClientProfiles
                .IgnoreQueryFilters()
                .CountAsync(ct) + 1;
            barcode = $"SC-{number:D6}";
        }
        while (await _db.ClientProfiles.IgnoreQueryFilters().AnyAsync(cp => cp.Barcode == barcode, ct));

        return barcode;
    }

    private static ClientDetailDto MapToDetail(ClientProfile cp) =>
        new(
            cp.Id,
            cp.UserId,
            cp.User.FullName,
            cp.User.Email,
            cp.Phone,
            cp.Barcode,
            cp.DateOfBirth,
            cp.Notes,
            cp.IsBlocked,
            cp.BonusBalance,
            cp.CreatedAt,
            cp.Subscriptions
                .OrderByDescending(s => s.PurchaseDate)
                .Select(MapSubscription)
                .ToList());

    private static SubscriptionDto MapSubscription(Subscription s) =>
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
            s.FinalPrice);
}
