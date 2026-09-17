using Rezerwacje.Application.Rooms.Dtos;

namespace Rezerwacje.Application.Rooms;

public interface IRoomService
{
    Task<IReadOnlyList<RoomDto>> GetAllAsync(bool activeOnly, CancellationToken ct = default);
    Task<RoomDto> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<RoomDto> CreateAsync(CreateRoomRequest request, CancellationToken ct = default);
    Task<RoomDto> UpdateAsync(Guid id, UpdateRoomRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}