namespace Rezerwacje.Application.Reservations.Dtos;

public record UpdateReservationRequest(
    string Title,
    DateTime StartTime,
    DateTime EndTime
);