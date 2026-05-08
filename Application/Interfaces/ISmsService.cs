namespace SportClub.Application.Interfaces;

public interface ISmsService
{
    Task SendOtpAsync(string phone, string code);
}
