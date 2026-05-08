namespace SportClub.Domain;

public enum UserRole
{
    Admin = 0,
    Receptionist = 1,
    Client = 2
}

public enum PlanType
{
    Unlimited = 0,

    LimitedVisits = 1
}

public enum PlanDurationUnit
{
    Days = 0,
    Months = 1
}

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
