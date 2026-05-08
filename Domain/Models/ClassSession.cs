namespace SportClub.Domain;

public class ClassSession
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int TrainerId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int MaxParticipants { get; set; }
    public bool IsCancelled { get; set; }

    public Trainer Trainer { get; set; } = null!;
    public ICollection<ClassBooking> Bookings { get; set; } = new List<ClassBooking>();

    public Guid Version { get; set; } = Guid.NewGuid();
}
