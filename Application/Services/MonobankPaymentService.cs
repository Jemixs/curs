using SportClub.Application.Interfaces;

namespace SportClub.Application.Services;

public sealed class MonobankPaymentService : IMonobankPaymentService
{
    // PaymentId -> кількість викликів CheckPaymentStatus
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, int> _callCounts = new();

    public Task<MonoPaymentInitResult> InitiatePaymentAsync(decimal amount, string description)
    {
        var paymentId = $"mono_{Guid.NewGuid():N}";
        _callCounts[paymentId] = 0;

        // Імітуємо URL для QR-коду (реальний Mono повертає URL на сторінку оплати)
        var invoiceUrl = $"https://pay.monobank.ua/invoice/{paymentId}";

        return Task.FromResult(new MonoPaymentInitResult(paymentId, invoiceUrl));
    }

    public Task<MonoPaymentStatus> CheckPaymentStatusAsync(string paymentId)
    {
        if (!_callCounts.ContainsKey(paymentId))
            return Task.FromResult(MonoPaymentStatus.Failed);

        _callCounts.AddOrUpdate(paymentId, 1, (_, count) => count + 1);
        var currentCount = _callCounts[paymentId];

        // Перші 2 виклики — очікування, 3-й — успіх
        var status = currentCount >= 3
            ? MonoPaymentStatus.Success
            : MonoPaymentStatus.WaitingForUserApprove;

        return Task.FromResult(status);
    }
}
