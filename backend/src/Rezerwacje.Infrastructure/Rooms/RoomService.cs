using Microsoft.EntityFrameworkCore;
using Rezerwacje.Application.Rooms;
using Rezerwacje.Application.Rooms.Dtos;
using Rezerwacje.Domain.Entities;
using Rezerwacje.Infrastructure.Persistence;

namespace Rezerwacje.Infrastructure.Rooms;

public class RoomService : IRoomService
{
    private readonly AppDbContext _db;

    public RoomService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<RoomDto>> GetAllAsync(bool activeOnly, CancellationToken ct = default)
    {
        var query = _db.Rooms.AsQueryable();
        if (activeOnly)
            query = query.Where(r => r.IsActive);

        var rooms = await query
            .OrderBy(r => r.Name)
            .Select(r => ToDto(r))
            .ToListAsync(ct);

        return rooms;
    }

    public async Task<RoomDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var room = await _db.Rooms.FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new KeyNotFoundException("Sala nie istnieje.");

        return ToDto(room);
    }

    public async Task<RoomDto> CreateAsync(CreateRoomRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new InvalidOperationException("Nazwa sali jest wymagana.");
        if (string.IsNullOrWhiteSpace(request.Location))
            throw new InvalidOperationException("Lokalizacja jest wymagana.");
        if (request.Capacity <= 0)
            throw new InvalidOperationException("Pojemność musi być większa od zera.");

        var room = new Room
        {
            Name = request.Name.Trim(),
            Location = request.Location.Trim(),
            Capacity = request.Capacity,
            Description = request.Description?.Trim(),
            Equipment = request.Equipment?.Trim(),
            IsActive = true
        };

        _db.Rooms.Add(room);
        await _db.SaveChangesAsync(ct);

        return ToDto(room);
    }

    public async Task<RoomDto> UpdateAsync(Guid id, UpdateRoomRequest request, CancellationToken ct = default)
    {
        var room = await _db.Rooms.FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new KeyNotFoundException("Sala nie istnieje.");

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new InvalidOperationException("Nazwa sali jest wymagana.");
        if (string.IsNullOrWhiteSpace(request.Location))
            throw new InvalidOperationException("Lokalizacja jest wymagana.");
        if (request.Capacity <= 0)
            throw new InvalidOperationException("Pojemność musi być większa od zera.");

        room.Name = request.Name.Trim();
        room.Location = request.Location.Trim();
        room.Capacity = request.Capacity;
        room.Description = request.Description?.Trim();
        room.Equipment = request.Equipment?.Trim();
        room.IsActive = request.IsActive;

        await _db.SaveChangesAsync(ct);

        return ToDto(room);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var room = await _db.Rooms.FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new KeyNotFoundException("Sala nie istnieje.");

        // Soft delete — zachowujemy historię rezerwacji.
        room.IsActive = false;
        await _db.SaveChangesAsync(ct);
    }

    private static RoomDto ToDto(Room r) =>
        new(r.Id, r.Name, r.Location, r.Capacity, r.Description, r.Equipment, r.IsActive);
}