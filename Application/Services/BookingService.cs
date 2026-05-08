using Microsoft.EntityFrameworkCore;
using SportClub.Application.DTOs;
using SportClub.Application.Interfaces;
using SportClub.Data;
using SportClub.Domain;

namespace SportClub.Application.Services;

public sealed class BookingService : IBookingService
{
    private readonly ApplicationDbContext _db;

    public BookingService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<IReadOnlyList<ClassSessionDto>>> GetUpcomingSessionsAsync(
        int? clientProfileId = null, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var to = now.AddDays(7);
        return await GetSessionsRangeAsync(now, to, clientProfileId, ct);
    }

    public async Task<Result<IReadOnlyList<ClassSessionDto>>> GetSessionsRangeAsync(
        DateTime from, DateTime to, int? clientProfileId = null, CancellationToken ct = default)
    {
        var sessions = await _db.ClassSessions
            .Include(cs => cs.Trainer)
            .Include(cs => cs.Bookings)
            .Where(cs => cs.StartTime >= from && cs.StartTime <= to && !cs.IsCancelled)
            .OrderBy(cs => cs.StartTime)
            .AsNoTracking()
            .ToListAsync(ct);

        HashSet<int> userBookedIds = new();
        if (clientProfileId.HasValue)
        {
            userBookedIds = (await _db.ClassBookings
                .Where(b => b.ClientProfileId == clientProfileId.Value
                         && b.Status == BookingStatus.Active)
                .Select(b => b.SessionId)
                .ToListAsync(ct))
                .ToHashSet();
        }

        var dtos = sessions.Select(cs => MapToSessionDto(cs, userBookedIds)).ToList();
        return Result<IReadOnlyList<ClassSessionDto>>.Ok(dtos);
    }

    public async Task<Result<ClassSessionDto>> CreateSessionAsync(CreateClassSessionDto dto, CancellationToken ct = default)
    {
        var trainer = await _db.Trainers.FirstOrDefaultAsync(t => t.Id == dto.TrainerId, ct);
        if (trainer is null || !trainer.IsActive)
            return Result<ClassSessionDto>.Fail("Тренера не знайдено або він неактивний.");

        if (dto.StartTime >= dto.EndTime)
            return Result<ClassSessionDto>.Fail("Час початку повинен бути раніше часу закінчення.");

        if (dto.MaxParticipants <= 0)
            return Result<ClassSessionDto>.Fail("Максимальна кількість учасників повинна бути більше 0.");

        var session = new ClassSession
        {
            Title = dto.Title,
            TrainerId = dto.TrainerId,
            StartTime = dto.StartTime.ToUniversalTime(),
            EndTime = dto.EndTime.ToUniversalTime(),
            MaxParticipants = dto.MaxParticipants,
            IsCancelled = false,
            Version = Guid.NewGuid()
        };

        _db.ClassSessions.Add(session);
        await _db.SaveChangesAsync(ct);

        session.Trainer = trainer;
        return Result<ClassSessionDto>.Ok(MapToSessionDto(session, new HashSet<int>()));
    }

    public async Task<Result> CancelSessionAsync(int sessionId, CancellationToken ct = default)
    {
        var session = await _db.ClassSessions.FirstOrDefaultAsync(cs => cs.Id == sessionId, ct);
        if (session is null)
            return Result.Fail("Заняття не знайдено.");

        session.IsCancelled = true;
        await _db.SaveChangesAsync(ct);
        return Result.Ok();
    }

    public async Task<Result<IReadOnlyList<ClassBookingDto>>> GetSessionBookingsAsync(int sessionId, CancellationToken ct = default)
    {
        var session = await _db.ClassSessions.FirstOrDefaultAsync(cs => cs.Id == sessionId, ct);
        if (session is null)
            return Result<IReadOnlyList<ClassBookingDto>>.Fail("Заняття не знайдено.");

        var bookings = await _db.ClassBookings
            .Include(b => b.Client).ThenInclude(c => c.User)
            .Include(b => b.Session)
            .Where(b => b.SessionId == sessionId)
            .AsNoTracking()
            .ToListAsync(ct);

        return Result<IReadOnlyList<ClassBookingDto>>.Ok(bookings.Select(MapToBookingDto).ToList());
    }

    public async Task<Result<IReadOnlyList<ClassBookingDto>>> GetClientBookingsAsync(int clientProfileId, CancellationToken ct = default)
    {
        var bookings = await _db.ClassBookings
            .Include(b => b.Client).ThenInclude(c => c.User)
            .Include(b => b.Session)
            .Where(b => b.ClientProfileId == clientProfileId)
            .OrderByDescending(b => b.Session.StartTime)
            .AsNoTracking()
            .ToListAsync(ct);

        return Result<IReadOnlyList<ClassBookingDto>>.Ok(bookings.Select(MapToBookingDto).ToList());
    }

    public async Task<Result<ClassBookingDto>> BookAsync(int sessionId, int clientProfileId, CancellationToken ct = default)
    {
        var activeSubscription = await _db.Subscriptions
            .Include(s => s.Plan)
            .Where(s => s.ClientProfileId == clientProfileId)
            .ToListAsync(ct);

        if (!activeSubscription.Any(s => s.IsActive))
            return Result<ClassBookingDto>.Fail("Запис можливий лише за наявності активного абонемента.");

        bool alreadyBooked = await _db.ClassBookings
            .AnyAsync(b => b.SessionId == sessionId
                        && b.ClientProfileId == clientProfileId
                        && b.Status == BookingStatus.Active, ct);

        if (alreadyBooked)
            return Result<ClassBookingDto>.Fail("Ви вже записані на це заняття.");

        // Concurrency-safe бронювання: перевіряємо місця та змінюємо Version в межах одної транзакції
        var maxRetries = 3;
        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            var session = await _db.ClassSessions
                .Include(cs => cs.Trainer)
                .Include(cs => cs.Bookings)
                .FirstOrDefaultAsync(cs => cs.Id == sessionId, ct);

            if (session is null)
                return Result<ClassBookingDto>.Fail("Заняття не знайдено.");

            if (session.IsCancelled)
                return Result<ClassBookingDto>.Fail("Це заняття скасовано.");

            if (session.StartTime <= DateTime.UtcNow)
                return Result<ClassBookingDto>.Fail("Не можна записатися на заняття, яке вже почалося або завершилося.");

            var activeBookingsCount = session.Bookings.Count(b => b.Status == BookingStatus.Active);
            if (activeBookingsCount >= session.MaxParticipants)
                return Result<ClassBookingDto>.Fail("На це заняття більше немає вільних місць.");

            // Оновлюємо Version як Concurrency Token — якщо між читанням і записом хтось ще записався, SaveChanges кине DbUpdateConcurrencyException
            var originalVersion = session.Version;
            session.Version = Guid.NewGuid();

            var booking = new ClassBooking
            {
                SessionId = sessionId,
                ClientProfileId = clientProfileId,
                BookingTime = DateTime.UtcNow,
                Status = BookingStatus.Active
            };
            _db.ClassBookings.Add(booking);

            try
            {
                await _db.SaveChangesAsync(ct);

                await _db.Entry(booking).Reference(b => b.Session).LoadAsync(ct);
                await _db.Entry(booking).Reference(b => b.Client).LoadAsync(ct);
                await _db.Entry(booking.Client).Reference(c => c.User).LoadAsync(ct);

                return Result<ClassBookingDto>.Ok(MapToBookingDto(booking));
            }
            catch (DbUpdateConcurrencyException)
            {
                _db.ClassBookings.Remove(booking);
                _db.ChangeTracker.Clear();

                if (attempt == maxRetries - 1)
                    return Result<ClassBookingDto>.Fail("Місце вже зайняте. Будь ласка, спробуйте ще раз.");
            }
        }

        return Result<ClassBookingDto>.Fail("Не вдалося виконати бронювання. Спробуйте пізніше.");
    }

    public async Task<Result> CancelBookingAsync(int bookingId, int clientProfileId, CancellationToken ct = default)
    {
        var booking = await _db.ClassBookings
            .Include(b => b.Session)
            .FirstOrDefaultAsync(b => b.Id == bookingId, ct);

        if (booking is null)
            return Result.Fail("Бронювання не знайдено.");

        if (booking.ClientProfileId != clientProfileId)
            return Result.Fail("Ви не маєте права скасовувати це бронювання.");

        if (booking.Session.StartTime <= DateTime.UtcNow.AddHours(1))
            return Result.Fail("Скасування можливе не пізніше ніж за 1 годину до початку заняття.");

        booking.Status = BookingStatus.Cancelled;
        await _db.SaveChangesAsync(ct);
        return Result.Ok();
    }

    public async Task<Result<IReadOnlyList<TrainerDto>>> GetTrainersAsync(CancellationToken ct = default)
    {
        var trainers = await _db.Trainers
            .Where(t => t.IsActive)
            .OrderBy(t => t.FullName)
            .AsNoTracking()
            .ToListAsync(ct);

        return Result<IReadOnlyList<TrainerDto>>.Ok(trainers.Select(MapToTrainerDto).ToList());
    }

    public async Task<Result<TrainerDto>> CreateTrainerAsync(CreateTrainerDto dto, CancellationToken ct = default)
    {
        var trainer = new Trainer
        {
            FullName = dto.FullName,
            Specialization = dto.Specialization,
            IsActive = true
        };
        _db.Trainers.Add(trainer);
        await _db.SaveChangesAsync(ct);
        return Result<TrainerDto>.Ok(MapToTrainerDto(trainer));
    }

    // Маппери

    private static ClassSessionDto MapToSessionDto(ClassSession cs, HashSet<int> userBookedIds) =>
        new(
            cs.Id,
            cs.Title,
            MapToTrainerDto(cs.Trainer),
            cs.StartTime,
            cs.EndTime,
            cs.MaxParticipants,
            cs.Bookings.Count(b => b.Status == BookingStatus.Active),
            cs.IsCancelled,
            userBookedIds.Contains(cs.Id));

    private static ClassBookingDto MapToBookingDto(ClassBooking b) =>
        new(
            b.Id,
            b.SessionId,
            b.Session.Title,
            b.Session.StartTime,
            b.Client.User.FullName,
            b.Client.Barcode,
            b.BookingTime,
            b.Status.ToString());

    private static TrainerDto MapToTrainerDto(Trainer t) =>
        new(t.Id, t.FullName, t.Specialization, t.IsActive);
}
