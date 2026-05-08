using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SportClub.Application.DTOs;
using SportClub.Application.Interfaces;
using SportClub.Data;
using SportClub.Domain;
using System.Security.Claims;

using Microsoft.Extensions.Caching.Memory;

namespace SportClub.Application.Services;

public sealed class AuthService : IAuthService
{
    private readonly ApplicationDbContext _db;
    private readonly IHttpContextAccessor _http;
    private readonly IMemoryCache _cache;
    private readonly ISmsService _smsService;

    public AuthService(
        ApplicationDbContext db,
        IHttpContextAccessor http,
        IMemoryCache cache,
        ISmsService smsService)
    {
        _db = db;
        _http = http;
        _cache = cache;
        _smsService = smsService;
    }

    public async Task<Result<AuthUserDto>> LoginAdminAsync(string login, string password, CancellationToken ct = default)
    {
        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == login, ct);

        if (user is null || user.Role != UserRole.Admin)
            return Result<AuthUserDto>.Fail("Доступ заборонено. Тільки для адміністраторів.");

        if (string.IsNullOrEmpty(user.PasswordHash) || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            return Result<AuthUserDto>.Fail("Невірний логін або пароль.");

        // Замість прямого SignInAsync (який не працює в Blazor Server Interactive),
        // генеруємо тимчасовий токен для входу через HTTP endpoint.
        var loginToken = Guid.NewGuid().ToString();
        _cache.Set($"LOGIN_TOKEN_{loginToken}", user.Id, TimeSpan.FromMinutes(1));

        return Result<AuthUserDto>.Ok(MapToDto(user) with { Email = loginToken }); // Токен в Email
    }

    public async Task LogoutAsync(CancellationToken ct = default)
    {
        var context = _http.HttpContext
            ?? throw new InvalidOperationException("HttpContext недоступний.");

        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }

    public async Task<Result<AuthUserDto>> GetCurrentUserAsync(CancellationToken ct = default)
    {
        var context = _http.HttpContext;
        var userIdClaim = context?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            return Result<AuthUserDto>.Fail("Користувач не авторизований.");

        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user is null)
            return Result<AuthUserDto>.Fail("Користувача не знайдено.");

        return Result<AuthUserDto>.Ok(MapToDto(user));
    }

    public async Task<Result<string>> RequestOtpAsync(string phone, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return Result<string>.Fail("Номер телефону не може бути порожнім.");

        // Генерація 4-значного коду
        var code = new Random().Next(1000, 10000).ToString();

        // Збереження в кеш на 3 хвилини
        var cacheKey = $"OTP_{phone}";
        _cache.Set(cacheKey, code, TimeSpan.FromMinutes(3));

        // Відправка SMS
        await _smsService.SendOtpAsync(phone, code);

        return Result<string>.Ok(code);
    }

    public async Task<Result<AuthUserDto>> VerifyOtpAndLoginAsync(VerifyOtpDto dto, CancellationToken ct = default)
    {
        var cacheKey = $"OTP_{dto.Phone}";
        if (!_cache.TryGetValue(cacheKey, out string? cachedCode))
            return Result<AuthUserDto>.Fail("Код не знайдено або час його дії вичерпано.");

        if (cachedCode != dto.Code)
            return Result<AuthUserDto>.Fail("Невірний код.");

        // Код правильний, видаляємо з кешу
        _cache.Remove(cacheKey);

        // Пошук клієнта за номером телефону
        var clientProfile = await _db.ClientProfiles
            .Include(cp => cp.User)
            .FirstOrDefaultAsync(cp => cp.Phone == dto.Phone, ct);

        User user;

        if (clientProfile is not null)
        {
            // Існуючий клієнт
            user = clientProfile.User;
        }
        else
        {
            // Новий клієнт (Auto-Register)
            string firstName = string.IsNullOrWhiteSpace(dto.FirstName) ? "Новий" : dto.FirstName.Trim();
            string lastName = string.IsNullOrWhiteSpace(dto.LastName) ? "Клієнт" : dto.LastName.Trim();
            string fullName = $"{firstName} {lastName}".Trim();

                user = new User
                {
                    Email = $"{dto.Phone}@sportclub.ua", // Тимчасовий email, оскільки поле обов'язкове та унікальне
                    FullName = fullName,
                Role = UserRole.Client,
                CreatedAt = DateTime.UtcNow
            };

            var profile = new ClientProfile
            {
                Phone = dto.Phone,
                Barcode = $"SC-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}",
                DateOfBirth = DateTime.UtcNow.AddYears(-18), // Заглушка
                User = user,
                CreatedAt = DateTime.UtcNow
            };

            _db.Users.Add(user);
            _db.ClientProfiles.Add(profile);
            await _db.SaveChangesAsync(ct);
        }

        // Замість прямого SignInAsync (який не працює в Blazor Server Interactive),
        // генеруємо тимчасовий токен для входу через HTTP endpoint.
        var loginToken = Guid.NewGuid().ToString();
        _cache.Set($"LOGIN_TOKEN_{loginToken}", user.Id, TimeSpan.FromMinutes(1));

        return Result<AuthUserDto>.Ok(MapToDto(user) with { Email = loginToken }); // Використовуємо Email поле як транспорт для токена в DTO для спрощення
    }

    public async Task<bool> CheckUserExistsAsync(string phone, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(phone)) return false;
        return await _db.ClientProfiles.AnyAsync(cp => cp.Phone == phone, ct);
    }

    private static AuthUserDto MapToDto(User user) =>
        new(user.Id, user.Email, user.FullName, user.Role.ToString());
}
