using SportClub.Domain;

namespace SportClub.Domain;

// Користувач (адмін, рецепція, клієнт)
public class User
{
    public int Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string? PasswordHash { get; set; }

    public UserRole Role { get; set; }

    public string FullName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Навігація
    public ClientProfile? ClientProfile { get; set; }
}

// Профіль клієнта
public class ClientProfile
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public string Phone { get; set; } = string.Empty;

    public string Barcode { get; set; } = string.Empty;

    public DateTime DateOfBirth { get; set; }

    public string? Notes { get; set; }

    public bool IsBlocked { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Фінанси та лояльність
    public decimal BonusBalance { get; set; } = 0;

    // Навігація
    public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
    public ICollection<CheckIn> CheckIns { get; set; } = new List<CheckIn>();
    public ICollection<ClassBooking> ClassBookings { get; set; } = new List<ClassBooking>();
}

// Тарифний план
public class Plan
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public PlanType PlanType { get; set; }

    public int? MaxVisits { get; set; }

    public int DurationValue { get; set; }

    public PlanDurationUnit DurationUnit { get; set; }

    public bool IsArchived { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Навігація
    public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
}

// Абонемент клієнта
public class Subscription
{
    public int Id { get; set; }

    public int ClientProfileId { get; set; }
    public ClientProfile ClientProfile { get; set; } = null!;

    public int PlanId { get; set; }
    public Plan Plan { get; set; } = null!;

    // Фінанси
    public decimal FinalPrice { get; set; }

    public string? TransactionId { get; set; }

    // Дати життєвого циклу
    public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;

    public DateTime? ActivationDate { get; set; }

    public DateTime? ExpirationDate { get; set; }

    // Заморозка
    public bool IsFrozen { get; set; }

    public int FrozenDaysUsed { get; set; }

    public DateTime? FrozenSince { get; set; }

    // Лічильник візитів
    public int VisitsUsed { get; set; }

    // Статус активності
    public bool IsActive
    {
        get
        {
            if (IsFrozen) return false;

            // Ще не активовано — активується при першому скануванні
            if (ActivationDate is null) return true;

            // Вийшов термін дії
            if (ExpirationDate.HasValue && DateTime.UtcNow > ExpirationDate.Value) return false;

            // Вичерпано ліміт візитів
            if (Plan is not null
                && Plan.PlanType == PlanType.LimitedVisits
                && Plan.MaxVisits.HasValue
                && VisitsUsed >= Plan.MaxVisits.Value)
            {
                return false;
            }

            return true;
        }
    }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Навігація
    public ICollection<CheckIn> CheckIns { get; set; } = new List<CheckIn>();
}

// Запис про вхід (відвідування)
public class CheckIn
{
    public int Id { get; set; }

    public int ClientProfileId { get; set; }
    public ClientProfile ClientProfile { get; set; } = null!;

    public int SubscriptionId { get; set; }
    public Subscription Subscription { get; set; } = null!;

    public DateTime CheckedInAt { get; set; } = DateTime.UtcNow;

    public string BarcodeSnapshot { get; set; } = string.Empty;
}
