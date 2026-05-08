using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using SportClub.Application.DTOs;
using SportClub.Application.Interfaces;
using SportClub.Data;
using SportClub.Domain;

namespace SportClub.Application.Services;

public sealed class AnalyticsService : IAnalyticsService
{
    private readonly ApplicationDbContext _db;

    public AnalyticsService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<FinancialReportDto>> GetFinancialReportAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        if (from > to)
            return Result<FinancialReportDto>.Fail("Дата 'З' не може бути більшою за дату 'По'.");

        var subscriptions = await _db.Subscriptions
            .Include(s => s.Plan)
            .Include(s => s.ClientProfile)
            .ThenInclude(cp => cp.User)
            .Where(s => s.PurchaseDate >= from && s.PurchaseDate <= to)
            .AsNoTracking()
            .ToListAsync(ct);

        decimal totalRevenue = subscriptions.Sum(s => s.FinalPrice);
        int totalSold = subscriptions.Count;
        decimal totalDiscount = subscriptions.Sum(s => s.Plan.Price - s.FinalPrice);
        // Ми не зберігаємо конкретно кількість бонусів і промокод у Subscription, 
        // тому об'єднуємо їх у загальну знижку. Якщо потрібно детально, 
        // треба додати відповідні поля в БД. Для базової аналітики цього достатньо.

        var sales = subscriptions.Select(s => new SaleReportItemDto(
            s.PurchaseDate,
            s.ClientProfile.User.FullName,
            s.ClientProfile.Barcode,
            s.Plan.Name,
            s.Plan.Price,
            s.FinalPrice,
            s.Plan.Price - s.FinalPrice,
            null // Промокод
        )).OrderByDescending(x => x.Date).ToList();

        return Result<FinancialReportDto>.Ok(new FinancialReportDto(
            from,
            to,
            totalRevenue,
            totalSold,
            totalDiscount, // Використано як "Загальна знижка/бонуси"
            0,             // Кількість використаних промокодів
            sales
        ));
    }

    public async Task<Result<byte[]>> ExportToExcelAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        var reportResult = await GetFinancialReportAsync(from, to, ct);
        if (!reportResult.IsSuccess)
            return Result<byte[]>.Fail(reportResult.FirstError);

        var report = reportResult.Value!;

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Фінансовий звіт");

        // Заголовок
        worksheet.Cell("A1").Value = $"Фінансовий звіт за період з {from:dd.MM.yyyy} по {to:dd.MM.yyyy}";
        worksheet.Range("A1:G1").Merge().Style.Font.SetBold().Font.FontSize = 14;

        // Дані (summary)
        worksheet.Cell("A3").Value = "Загальний дохід:";
        worksheet.Cell("B3").Value = report.TotalRevenue;
        worksheet.Cell("B3").Style.NumberFormat.Format = "#,##0.00";

        worksheet.Cell("A4").Value = "Продано абонементів:";
        worksheet.Cell("B4").Value = report.TotalSubscriptionsSold;

        worksheet.Cell("A5").Value = "Надано знижок/бонусів:";
        worksheet.Cell("B5").Value = report.TotalBonusesUsed;
        worksheet.Cell("B5").Style.NumberFormat.Format = "#,##0.00";

        // Шапка таблиці
        int currentRow = 7;
        worksheet.Cell(currentRow, 1).Value = "Дата продажу";
        worksheet.Cell(currentRow, 2).Value = "Клієнт";
        worksheet.Cell(currentRow, 3).Value = "Баркод";
        worksheet.Cell(currentRow, 4).Value = "Тариф";
        worksheet.Cell(currentRow, 5).Value = "Початкова ціна";
        worksheet.Cell(currentRow, 6).Value = "Знижка / Бонуси";
        worksheet.Cell(currentRow, 7).Value = "Фактична ціна";

        var headerRange = worksheet.Range(currentRow, 1, currentRow, 7);
        headerRange.Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.LightGray);
        headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

        // Дані таблиці
        foreach (var sale in report.Sales)
        {
            currentRow++;
            worksheet.Cell(currentRow, 1).Value = sale.Date.ToString("dd.MM.yyyy HH:mm");
            worksheet.Cell(currentRow, 2).Value = sale.ClientFullName;
            worksheet.Cell(currentRow, 3).Value = sale.ClientBarcode;
            worksheet.Cell(currentRow, 4).Value = sale.PlanName;
            
            worksheet.Cell(currentRow, 5).Value = sale.OriginalPrice;
            worksheet.Cell(currentRow, 5).Style.NumberFormat.Format = "#,##0.00";
            
            worksheet.Cell(currentRow, 6).Value = sale.BonusesUsed;
            worksheet.Cell(currentRow, 6).Style.NumberFormat.Format = "#,##0.00";
            
            worksheet.Cell(currentRow, 7).Value = sale.FinalPrice;
            worksheet.Cell(currentRow, 7).Style.NumberFormat.Format = "#,##0.00";
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return Result<byte[]>.Ok(stream.ToArray());
    }
}
