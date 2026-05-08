namespace SportClub.Domain;

public enum BookingStatus
{
    Active = 0,
    Cancelled = 1
}

public class ClassBooking
{
    public int Id { get; set; }
    public int SessionId { get; set; }
    public int ClientProfileId { get; set; }
    public DateTime BookingTime { get; set; }
    public BookingStatus Status { get; set; } = BookingStatus.Active;

    // Navigation
    public ClassSession Session { get; set; } = null!;
    public ClientProfile Client { get; set; } = null!;
}
