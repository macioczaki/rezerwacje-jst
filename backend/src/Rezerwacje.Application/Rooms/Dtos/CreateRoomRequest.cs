namespace Rezerwacje.Application.Rooms.Dtos;

public record CreateRoomRequest(
    string Name,
    string Location,
    int Capacity,
    string? Description,
    string? Equipment
);