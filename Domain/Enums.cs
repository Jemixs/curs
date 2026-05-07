namespace SportClub.Domain;

/// <summary>Role assigned to a user account.</summary>
public enum UserRole
{
    Admin = 0,
    Receptionist = 1,
    Client = 2
}

/// <summary>Determines how a subscription's session count is tracked.</summary>
public enum PlanType
{
    /// <summary>Unlimited visits within the validity period.</summary>
    Unlimited = 0,

    /// <summary>A fixed number of visits within the validity period.</summary>
    LimitedVisits = 1
}

/// <summary>Granularity of the plan validity period.</summary>
public enum PlanDurationUnit
{
    Days = 0,
    Months = 1
}

/// <summary>Outcome of a check-in attempt.</summary>
public enum CheckInResult
{
    Success = 0,
    AlreadyCheckedIn = 1,      // debounce guard
    ClientNotFound = 2,
    ClientBlocked = 3,
    NoActiveSubscription = 4,
    SubscriptionFrozen = 5,
    SubscriptionExpired = 6,
    NoVisitsLeft = 7
}
