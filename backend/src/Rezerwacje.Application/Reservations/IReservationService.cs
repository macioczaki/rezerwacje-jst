using Rezerwacje.Application.Reservations.Dtos;

namespace Rezerwacje.Application.Reservations;

public interface IReservationService
{
    Task<IReadOnlyList<ReservationDto>> GetAllAsync(
        Guid? roomId,
        Guid? userId,
        DateTime? from,
        DateTime? to,
        bool onlyActive,
        CancellationToken ct = default);

    Task<ReservationDto> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<ReservationDto> CreateAsync(Guid userId, CreateReservationRequest request, CancellationToken ct = default);

    Task<ReservationDto> UpdateAsync(Guid id, Guid userId, bool isAdmin, UpdateReservationRequest request, CancellationToken ct = default);

    Task CancelAsync(Guid id, Guid userId, bool isAdmin, CancellationToken ct = default);

    Task<AvailabilitySlotDto> GetAvailabilityAsync(Guid roomId, DateTime date, CancellationToken ct = default);
}