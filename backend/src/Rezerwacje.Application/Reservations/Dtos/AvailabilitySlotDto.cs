namespace Rezerwacje.Application.Reservations.Dtos;

public record AvailabilitySlotDto(
    Guid RoomId,
    DateTime Date,
    IReadOnlyList<BusySlot> BusySlots
);

public record BusySlot(DateTime StartTime, DateTime EndTime, string Title);