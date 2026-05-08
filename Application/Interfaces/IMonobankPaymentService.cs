using SportClub.Application.Services;

namespace SportClub.Application.Interfaces;

public interface IMonobankPaymentService
{
    Task<MonoPaymentInitResult> InitiatePaymentAsync(decimal amount, string description);

    Task<MonoPaymentStatus> CheckPaymentStatusAsync(string paymentId);
}
