namespace Rezerwacje.Application.Rooms.Dtos;

public record UpdateRoomRequest(
    string Name,
    string Location,
    int Capacity,
    string? Description,
    string? Equipment,
    bool IsActive
);