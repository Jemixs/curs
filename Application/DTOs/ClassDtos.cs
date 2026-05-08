namespace SportClub.Application.DTOs;

public sealed record TrainerDto(
    int Id,
    string FullName,
    string Specialization,
    bool IsActive);

public sealed record CreateTrainerDto(
    string FullName,
    string Specialization);

public sealed record ClassSessionDto(
    int Id,
    string Title,
    TrainerDto Trainer,
    DateTime StartTime,
    DateTime EndTime,
    int MaxParticipants,
    int CurrentParticipants,
    bool IsCancelled,
    bool IsUserBooked);

public sealed record CreateClassSessionDto(
    string Title,
    int TrainerId,
    DateTime StartTime,
    DateTime EndTime,
    int MaxParticipants);

public sealed record ClassBookingDto(
    int Id,
    int SessionId,
    string SessionTitle,
    DateTime SessionStart,
    string ClientFullName,
    string ClientBarcode,
    DateTime BookingTime,
    string Status);
