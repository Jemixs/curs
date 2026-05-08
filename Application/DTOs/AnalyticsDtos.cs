namespace SportClub.Application.DTOs;

public sealed record FinancialReportDto(
    DateTime From,
    DateTime To,
    decimal TotalRevenue,
    int TotalSubscriptionsSold,
    decimal TotalBonusesUsed,
    int PromoCodesUsed,
    IReadOnlyList<SaleReportItemDto> Sales);

public sealed record SaleReportItemDto(
    DateTime Date,
    string ClientFullName,
    string ClientBarcode,
    string PlanName,
    decimal OriginalPrice,
    decimal FinalPrice,
    decimal BonusesUsed,
    string? PromoCode);
