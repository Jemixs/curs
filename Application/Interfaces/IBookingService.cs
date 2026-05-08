using SportClub.Application.DTOs;
using SportClub.Domain;

namespace SportClub.Application.Interfaces;

public interface IBookingService
{
    Task<Result<IReadOnlyList<ClassSessionDto>>> GetUpcomingSessionsAsync(int? clientProfileId = null, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ClassSessionDto>>> GetSessionsRangeAsync(DateTime from, DateTime to, int? clientProfileId = null, CancellationToken ct = default);
    Task<Result<ClassSessionDto>> CreateSessionAsync(CreateClassSessionDto dto, CancellationToken ct = default);
    Task<Result> CancelSessionAsync(int sessionId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ClassBookingDto>>> GetSessionBookingsAsync(int sessionId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ClassBookingDto>>> GetClientBookingsAsync(int clientProfileId, CancellationToken ct = default);
    Task<Result<ClassBookingDto>> BookAsync(int sessionId, int clientProfileId, CancellationToken ct = default);
    Task<Result> CancelBookingAsync(int bookingId, int clientProfileId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<TrainerDto>>> GetTrainersAsync(CancellationToken ct = default);
    Task<Result<TrainerDto>> CreateTrainerAsync(CreateTrainerDto dto, CancellationToken ct = default);
}
