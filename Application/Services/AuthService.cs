using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SportClub.Application.DTOs;
using SportClub.Application.Interfaces;
using SportClub.Data;
using SportClub.Domain;
using System.Security.Claims;

namespace SportClub.Application.Services;

public sealed class AuthService : IAuthService
{
    private readonly ApplicationDbContext _db;
    private readonly IHttpContextAccessor _http;

    public AuthService(ApplicationDbContext db, IHttpContextAccessor http)
    {
        _db = db;
        _http = http;
    }

    public async Task<Result<AuthUserDto>> LoginAsync(LoginDto dto, CancellationToken ct = default)
    {
        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == dto.Email, ct);

        if (user is null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            return Result<AuthUserDto>.Fail("Невірний логін або пароль.");

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString())
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        var context = _http.HttpContext
            ?? throw new InvalidOperationException("HttpContext недоступний.");

        await context.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties { IsPersistent = true });

        return Result<AuthUserDto>.Ok(MapToDto(user));
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

    private static AuthUserDto MapToDto(User user) =>
        new(user.Id, user.Email, user.FullName, user.Role.ToString());
}
