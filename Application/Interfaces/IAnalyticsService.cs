using SportClub.Application.DTOs;
using SportClub.Domain;

namespace SportClub.Application.Interfaces;

public interface IAnalyticsService
{
    Task<Result<FinancialReportDto>> GetFinancialReportAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<Result<byte[]>> ExportToExcelAsync(DateTime from, DateTime to, CancellationToken ct = default);
}
