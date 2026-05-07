using SportClub.Application.DTOs;
using SportClub.Domain;

namespace SportClub.Application.Interfaces;

public interface IAuthService
{
    Task<Result<AuthUserDto>> LoginAsync(LoginDto dto, CancellationToken ct = default);
    Task LogoutAsync(CancellationToken ct = default);
    Task<Result<AuthUserDto>> GetCurrentUserAsync(CancellationToken ct = default);
}
