using SportClub.Application.DTOs;
using SportClub.Domain;

namespace SportClub.Application.Interfaces;

public interface IAuthService
{
    Task<Result<AuthUserDto>> LoginAdminAsync(string login, string password, CancellationToken ct = default);
    Task LogoutAsync(CancellationToken ct = default);
    Task<Result<AuthUserDto>> GetCurrentUserAsync(CancellationToken ct = default);

    Task<Result<string>> RequestOtpAsync(string phone, CancellationToken ct = default);

    Task<Result<AuthUserDto>> VerifyOtpAndLoginAsync(VerifyOtpDto dto, CancellationToken ct = default);

    Task<bool> CheckUserExistsAsync(string phone, CancellationToken ct = default);
}
