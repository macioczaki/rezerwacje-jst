using Microsoft.EntityFrameworkCore;
using Rezerwacje.Application.Reservations;
using Rezerwacje.Application.Reservations.Dtos;
using Rezerwacje.Domain.Entities;
using Rezerwacje.Infrastructure.Persistence;

namespace Rezerwacje.Infrastructure.Reservations;

public class ReservationService : IReservationService
{
    private readonly AppDbContext _db;

    public ReservationService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<ReservationDto>> GetAllAsync(
        Guid? roomId,
        Guid? userId,
        DateTime? from,
        DateTime? to,
        bool onlyActive,
        CancellationToken ct = default)
    {
        var query = _db.Reservations
            .Include(r => r.Room)
            .Include(r => r.User)
            .AsQueryable();

        if (onlyActive)
            query = query.Where(r => r.Status == ReservationStatus.Active);
        if (roomId.HasValue)
            query = query.Where(r => r.RoomId == roomId.Value);
        if (userId.HasValue)
            query = query.Where(r => r.UserId == userId.Value);
        if (from.HasValue)
            query = query.Where(r => r.EndTime > from.Value);
        if (to.HasValue)
            query = query.Where(r => r.StartTime < to.Value);

        var result = await query
            .OrderBy(r => r.StartTime)
            .Select(r => ToDto(r))
            .ToListAsync(ct);

        return result;
    }

    public async Task<ReservationDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var reservation = await _db.Reservations
            .Include(r => r.Room)
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new KeyNotFoundException("Rezerwacja nie istnieje.");

        return ToDto(reservation);
    }

    public async Task<ReservationDto> CreateAsync(Guid userId, CreateReservationRequest request, CancellationToken ct = default)
    {
        ValidateTimes(request.StartTime, request.EndTime);

        if (string.IsNullOrWhiteSpace(request.Title))
            throw new InvalidOperationException("Tytuł rezerwacji jest wymagany.");

        var room = await _db.Rooms.FirstOrDefaultAsync(r => r.Id == request.RoomId, ct)
            ?? throw new KeyNotFoundException("Sala nie istnieje.");

        if (!room.IsActive)
            throw new InvalidOperationException("Sala jest nieaktywna — nie można tworzyć nowych rezerwacji.");

        await EnsureNoCollisionAsync(request.RoomId, request.StartTime, request.EndTime, excludeId: null, ct);

        var reservation = new Reservation
        {
            RoomId = request.RoomId,
            UserId = userId,
            Title = request.Title.Trim(),
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            Status = ReservationStatus.Active
        };

        _db.Reservations.Add(reservation);
        await _db.SaveChangesAsync(ct);

        // Doładuj nawigacje, żeby DTO miało RoomName i UserEmail.
        await _db.Entry(reservation).Reference(r => r.Room).LoadAsync(ct);
        await _db.Entry(reservation).Reference(r => r.User).LoadAsync(ct);

        return ToDto(reservation);
    }

    public async Task<ReservationDto> UpdateAsync(Guid id, Guid userId, bool isAdmin, UpdateReservationRequest request, CancellationToken ct = default)
    {
        ValidateTimes(request.StartTime, request.EndTime);

        if (string.IsNullOrWhiteSpace(request.Title))
            throw new InvalidOperationException("Tytuł rezerwacji jest wymagany.");

        var reservation = await _db.Reservations
            .Include(r => r.Room)
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new KeyNotFoundException("Rezerwacja nie istnieje.");

        if (reservation.Status == ReservationStatus.Cancelled)
            throw new InvalidOperationException("Nie można edytować anulowanej rezerwacji.");

        if (!isAdmin && reservation.UserId != userId)
            throw new UnauthorizedAccessException("Możesz edytować tylko własne rezerwacje.");

        await EnsureNoCollisionAsync(reservation.RoomId, request.StartTime, request.EndTime, excludeId: id, ct);

        reservation.Title = request.Title.Trim();
        reservation.StartTime = request.StartTime;
        reservation.EndTime = request.EndTime;

        await _db.SaveChangesAsync(ct);

        return ToDto(reservation);
    }

    public async Task CancelAsync(Guid id, Guid userId, bool isAdmin, CancellationToken ct = default)
    {
        var reservation = await _db.Reservations
            .FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new KeyNotFoundException("Rezerwacja nie istnieje.");

        if (reservation.Status == ReservationStatus.Cancelled)
            throw new InvalidOperationException("Rezerwacja jest już anulowana.");

        if (!isAdmin && reservation.UserId != userId)
            throw new UnauthorizedAccessException("Możesz anulować tylko własne rezerwacje.");

        reservation.Status = ReservationStatus.Cancelled;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<AvailabilitySlotDto> GetAvailabilityAsync(Guid roomId, DateTime date, CancellationToken ct = default)
    {
        var roomExists = await _db.Rooms.AnyAsync(r => r.Id == roomId, ct);
        if (!roomExists)
            throw new KeyNotFoundException("Sala nie istnieje.");

        // Dzień w UTC: [00:00, 24:00)
        var dayStart = date.Date;
        var dayEnd = dayStart.AddDays(1);

        var busy = await _db.Reservations
            .Where(r => r.RoomId == roomId
                        && r.Status == ReservationStatus.Active
                        && r.EndTime > dayStart
                        && r.StartTime < dayEnd)
            .OrderBy(r => r.StartTime)
            .Select(r => new BusySlot(r.StartTime, r.EndTime, r.Title))
            .ToListAsync(ct);

        return new AvailabilitySlotDto(roomId, dayStart, busy);
    }

    private async Task EnsureNoCollisionAsync(Guid roomId, DateTime start, DateTime end, Guid? excludeId, CancellationToken ct)
    {
        // Kolizja: nowa rezerwacja nachodzi na istniejącą, gdy
        //   nowy.Start < istn.End  ORAZ  nowy.End > istn.Start
        // (z pominięciem anulowanych i ewentualnie edytowanej samej siebie)
        var collision = await _db.Reservations
            .Where(r => r.RoomId == roomId
                        && r.Status == ReservationStatus.Active
                        && (excludeId == null || r.Id != excludeId.Value)
                        && r.StartTime < end
                        && r.EndTime > start)
            .Select(r => new { r.Id, r.Title, r.StartTime, r.EndTime })
            .FirstOrDefaultAsync(ct);

        if (collision is not null)
        {
            throw new InvalidOperationException(
                $"Sala jest już zajęta w tym czasie (rezerwacja „{collision.Title}”: " +
                $"{collision.StartTime:yyyy-MM-dd HH:mm}–{collision.EndTime:HH:mm}).");
        }
    }

    private static void ValidateTimes(DateTime start, DateTime end)
    {
        if (end <= start)
            throw new InvalidOperationException("Data zakończenia musi być późniejsza niż data rozpoczęcia.");

        if (start < DateTime.UtcNow.AddMinutes(-5))
            throw new InvalidOperationException("Nie można tworzyć rezerwacji w przeszłości.");

        if ((end - start) > TimeSpan.FromHours(24))
            throw new InvalidOperationException("Rezerwacja nie może trwać dłużej niż 24 godziny.");
    }

    private static ReservationDto ToDto(Reservation r) =>
        new(
            r.Id,
            r.RoomId,
            r.Room?.Name ?? string.Empty,
            r.UserId,
            r.User?.Email ?? string.Empty,
            r.Title,
            r.StartTime,
            r.EndTime,
            r.Status.ToString(),
            r.CreatedAt
        );
}