namespace Rezerwacje.Application.Reservations.Dtos;

public record CreateReservationRequest(
    Guid RoomId,
    string Title,
    DateTime StartTime,
    DateTime EndTime
);