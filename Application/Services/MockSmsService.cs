using SportClub.Application.Interfaces;

namespace SportClub.Application.Services;

public sealed class MockSmsService : ISmsService
{
    public async Task SendOtpAsync(string phone, string code)
    {
        // Імітація затримки мережі
        await Task.Delay(500);

        // В реальному проекті тут буде виклик API Twilio, TurboSMS або іншого провайдера
        Console.WriteLine($"[SMS MOCK] Відправлено код {code} на номер {phone}");
    }
}
