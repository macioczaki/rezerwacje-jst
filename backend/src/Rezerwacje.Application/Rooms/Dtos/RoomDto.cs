namespace Rezerwacje.Application.Rooms.Dtos;

public record RoomDto(
    Guid Id,
    string Name,
    string Location,
    int Capacity,
    string? Description,
    string? Equipment,
    bool IsActive
);