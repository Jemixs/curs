using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using SportClub.Data;

namespace SportClub.Application.Services;

public sealed class SubscriptionMaintenanceService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SubscriptionMaintenanceService> _logger;

    public SubscriptionMaintenanceService(
        IServiceProvider serviceProvider,
        ILogger<SubscriptionMaintenanceService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SubscriptionMaintenanceService запущено.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PerformMaintenanceAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка під час виконання фонового завдання SubscriptionMaintenanceService.");
            }

            // Імітація добового таймера. У реальному житті можна використовувати 
            // Quartz.NET або Hangfire для cron-завдань. 
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }

    private async Task PerformMaintenanceAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var now = DateTime.UtcNow;

        // Логування прострочених абонементів
        // Оскільки IsActive є обчислюваною властивістю, просто знаходимо ті, що щойно закінчилися, для генерації логів (або у майбутньому — відправки Email/Telegram)
        var expiredSubscriptions = await db.Subscriptions
            .Include(s => s.ClientProfile)
            .ThenInclude(cp => cp.User)
            .Where(s => s.ExpirationDate.HasValue 
                     && s.ExpirationDate.Value < now 
                     && s.ExpirationDate.Value >= now.AddDays(-1)) // Які закінчилися за останню добу
            .ToListAsync(stoppingToken);

        if (expiredSubscriptions.Any())
        {
            _logger.LogInformation($"Знайдено {expiredSubscriptions.Count} абонементів, термін дії яких минув за останню добу.");
            foreach (var sub in expiredSubscriptions)
            {
                _logger.LogInformation($"Абонемент ID: {sub.Id} клієнта {sub.ClientProfile.User.Email} минув {sub.ExpirationDate}.");
            }
        }

        // Автоматичне розморожування (якщо заморозка триває більше 30 днів)
        var maxFreezeDays = 30;
        var frozenTooLong = await db.Subscriptions
            .Where(s => s.IsFrozen 
                     && s.FrozenSince.HasValue 
                     && s.FrozenSince.Value.AddDays(maxFreezeDays) < now)
            .ToListAsync(stoppingToken);

        if (frozenTooLong.Any())
        {
            _logger.LogInformation($"Знайдено {frozenTooLong.Count} абонементів з перевищеним терміном заморозки. Виконується авто-розморожування.");
            
            foreach (var sub in frozenTooLong)
            {
                var frozenDays = (int)(now - sub.FrozenSince!.Value).TotalDays;
                sub.FrozenDaysUsed += frozenDays;
                sub.IsFrozen = false;
                sub.FrozenSince = null;

                if (sub.ExpirationDate.HasValue)
                {
                    sub.ExpirationDate = sub.ExpirationDate.Value.AddDays(frozenDays);
                }
            }

            await db.SaveChangesAsync(stoppingToken);
            _logger.LogInformation("Авто-розморожування успішно завершено.");
        }
    }
}
