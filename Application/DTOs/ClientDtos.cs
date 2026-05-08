namespace SportClub.Application.DTOs;

public sealed record LoginDto(
    string Email,
    string Password);

public sealed record RequestOtpDto(string Phone);

public sealed record VerifyOtpDto(
    string Phone,
    string Code,
    string? FirstName = null,
    string? LastName = null);

public sealed record RegisterClientDto(
    string Email,
    string Password,
    string FullName,
    string Phone,
    DateTime DateOfBirth,
    string? Notes);

public sealed record UpdateClientDto(
    int Id,
    string FullName,
    string Email,
    string Phone,
    DateTime DateOfBirth,
    string? Notes,
    bool IsBlocked);

public sealed record ClientListItemDto(
    int Id,
    int UserId,
    string FullName,
    string Email,
    string Phone,
    string Barcode,
    bool IsBlocked,
    DateTime CreatedAt);

public sealed record ClientDetailDto(
    int Id,
    int UserId,
    string FullName,
    string Email,
    string Phone,
    string Barcode,
    DateTime DateOfBirth,
    string? Notes,
    bool IsBlocked,
    decimal BonusBalance,
    DateTime CreatedAt,
    IReadOnlyList<SubscriptionDto> Subscriptions);

public sealed record AuthUserDto(
    int UserId,
    string Email,
    string FullName,
    string Role);
