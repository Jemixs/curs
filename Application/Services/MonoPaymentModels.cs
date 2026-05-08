namespace SportClub.Application.Services;

public sealed record MonoPaymentInitResult(string PaymentId, string InvoiceUrl);

public enum MonoPaymentStatus
{
    WaitingForUserApprove,
    Success,
    Failed
}
