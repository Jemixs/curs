namespace SportClub.Domain.Exceptions;

// Base business exception

public class BusinessRuleValidationException : Exception
{
    public string Code { get; }

    public BusinessRuleValidationException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    public BusinessRuleValidationException(string code, string message, Exception inner)
        : base(message, inner)
    {
        Code = code;
    }
}

// Specialised domain exceptions

public sealed class ClientNotFoundException : BusinessRuleValidationException
{
    public ClientNotFoundException(string barcode)
        : base("CLIENT_NOT_FOUND", $"Клієнта з баркодом '{barcode}' не знайдено.") { }

    public ClientNotFoundException(int id)
        : base("CLIENT_NOT_FOUND", $"Клієнта з ID {id} не знайдено.") { }
}

public sealed class ClientBlockedException : BusinessRuleValidationException
{
    public ClientBlockedException()
        : base("CLIENT_BLOCKED", "Профіль клієнта заблоковано. Зверніться до адміністратора.") { }
}

public sealed class SubscriptionFrozenException : BusinessRuleValidationException
{
    public SubscriptionFrozenException()
        : base("SUBSCRIPTION_FROZEN", "Абонемент заморожено. Розморозьте його для відновлення доступу.") { }
}

public sealed class NoActiveSubscriptionException : BusinessRuleValidationException
{
    public NoActiveSubscriptionException()
        : base("NO_ACTIVE_SUBSCRIPTION", "Активного абонемента не знайдено.") { }
}

public sealed class DuplicateBarcodeException : BusinessRuleValidationException
{
    public DuplicateBarcodeException(string barcode)
        : base("DUPLICATE_BARCODE", $"Баркод '{barcode}' вже використовується.") { }
}

public sealed class DuplicatePhoneException : BusinessRuleValidationException
{
    public DuplicatePhoneException(string phone)
        : base("DUPLICATE_PHONE", $"Номер телефону '{phone}' вже зареєстровано.") { }
}

public sealed class InvalidCredentialsException : BusinessRuleValidationException
{
    public InvalidCredentialsException()
        : base("INVALID_CREDENTIALS", "Невірний логін або пароль.") { }
}

public sealed class PlanArchivedException : BusinessRuleValidationException
{
    public PlanArchivedException(int planId)
        : base("PLAN_ARCHIVED", $"Тариф #{planId} архівовано і недоступний для продажу.") { }
}
