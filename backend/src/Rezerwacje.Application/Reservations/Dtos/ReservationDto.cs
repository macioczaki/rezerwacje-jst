namespace Rezerwacje.Application.Reservations.Dtos;

public record ReservationDto(
    Guid Id,
    Guid RoomId,
    string RoomName,
    Guid UserId,
    string UserEmail,
    string Title,
    DateTime StartTime,
    DateTime EndTime,
    string Status,
    DateTime CreatedAt
);