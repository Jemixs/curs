using SportClub.Domain;

namespace SportClub.Domain;

// ─────────────────────────────────────────────────────────────
//  User  (system account — admins, receptionists, clients)
// ─────────────────────────────────────────────────────────────
public class User
{
    public int Id { get; set; }

    /// <summary>Login / e-mail. Unique.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>BCrypt hash.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    public UserRole Role { get; set; }

    public string FullName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ClientProfile? ClientProfile { get; set; }
}

// ─────────────────────────────────────────────────────────────
//  ClientProfile  (additional info for clients only)
// ─────────────────────────────────────────────────────────────
public class ClientProfile
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    /// <summary>Unique phone number. Indexed for O(1) lookup.</summary>
    public string Phone { get; set; } = string.Empty;

    /// <summary>Unique barcode/QR value. Indexed for O(1) scan lookup.</summary>
    public string Barcode { get; set; } = string.Empty;

    public DateTime DateOfBirth { get; set; }

    public string? Notes { get; set; }

    public bool IsBlocked { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
    public ICollection<CheckIn> CheckIns { get; set; } = new List<CheckIn>();
}

// ─────────────────────────────────────────────────────────────
//  Plan  (pricing plan / tariff)
// ─────────────────────────────────────────────────────────────
public class Plan
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public PlanType PlanType { get; set; }

    /// <summary>Relevant only when PlanType == LimitedVisits.</summary>
    public int? MaxVisits { get; set; }

    /// <summary>Numeric validity value (e.g. 1 month, 30 days).</summary>
    public int DurationValue { get; set; }

    public PlanDurationUnit DurationUnit { get; set; }

    public bool IsArchived { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
}

// ─────────────────────────────────────────────────────────────
//  Subscription  (client's purchased plan instance)
// ─────────────────────────────────────────────────────────────
public class Subscription
{
    public int Id { get; set; }

    public int ClientProfileId { get; set; }
    public ClientProfile ClientProfile { get; set; } = null!;

    public int PlanId { get; set; }
    public Plan Plan { get; set; } = null!;

    // ── Lifecycle dates ──────────────────────────────────────
    public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;

    /// <summary>Set on first check-in (lazy activation).</summary>
    public DateTime? ActivationDate { get; set; }

    /// <summary>Calculated from ActivationDate + Plan duration, extended by frozen days.</summary>
    public DateTime? ExpirationDate { get; set; }

    // ── Freeze support ───────────────────────────────────────
    public bool IsFrozen { get; set; }

    /// <summary>Accumulates total frozen calendar days; used to extend ExpirationDate on unfreeze.</summary>
    public int FrozenDaysUsed { get; set; }

    /// <summary>Timestamp when freeze started (null if not currently frozen).</summary>
    public DateTime? FrozenSince { get; set; }

    // ── Visit counter (only relevant for LimitedVisits plans) ─
    public int VisitsUsed { get; set; }

    // ── Computed active-state ────────────────────────────────
    /// <summary>
    /// Returns true when the subscription can admit a check-in.
    /// Frozen subscriptions are NEVER active.
    /// </summary>
    public bool IsActive
    {
        get
        {
            if (IsFrozen) return false;

            // Not yet activated — can be activated on first scan
            if (ActivationDate is null) return true;

            // Expired by date
            if (ExpirationDate.HasValue && DateTime.UtcNow > ExpirationDate.Value) return false;

            // Exhausted visits
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

    // Navigation
    public ICollection<CheckIn> CheckIns { get; set; } = new List<CheckIn>();
}

// ─────────────────────────────────────────────────────────────
//  CheckIn  (individual gym-entry event)
// ─────────────────────────────────────────────────────────────
public class CheckIn
{
    public int Id { get; set; }

    public int ClientProfileId { get; set; }
    public ClientProfile ClientProfile { get; set; } = null!;

    public int SubscriptionId { get; set; }
    public Subscription Subscription { get; set; } = null!;

    public DateTime CheckedInAt { get; set; } = DateTime.UtcNow;

    /// <summary>Optional: barcode value at the time of scan (audit trail).</summary>
    public string BarcodeSnapshot { get; set; } = string.Empty;
}
