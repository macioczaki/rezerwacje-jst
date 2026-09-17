using Microsoft.EntityFrameworkCore;
using Rezerwacje.Application.Rooms.Dtos;
using Rezerwacje.Infrastructure.Persistence;
using Rezerwacje.Infrastructure.Rooms;
using Xunit;

namespace Rezerwacje.UnitTests.Rooms;

public class RoomServiceTests
{
    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static CreateRoomRequest ValidCreateRequest() =>
        new("Sala A", "Budynek główny, I piętro", 20, "Opis", "Projektor");

    [Fact]
    public async Task CreateAsync_ValidRequest_CreatesRoom()
    {
        using var db = CreateDb();
        var service = new RoomService(db);

        var room = await service.CreateAsync(ValidCreateRequest());

        Assert.NotEqual(Guid.Empty, room.Id);
        Assert.Equal("Sala A", room.Name);
        Assert.Equal("Budynek główny, I piętro", room.Location);
        Assert.Equal(20, room.Capacity);
        Assert.True(room.IsActive);

        var count = await db.Rooms.CountAsync();
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task CreateAsync_EmptyName_Throws()
    {
        using var db = CreateDb();
        var service = new RoomService(db);
        var request = ValidCreateRequest() with { Name = "  " };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(request));

        Assert.Contains("Nazwa", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_EmptyLocation_Throws()
    {
        using var db = CreateDb();
        var service = new RoomService(db);
        var request = ValidCreateRequest() with { Location = "" };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(request));

        Assert.Contains("Lokalizacja", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_NonPositiveCapacity_Throws()
    {
        using var db = CreateDb();
        var service = new RoomService(db);
        var request = ValidCreateRequest() with { Capacity = 0 };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(request));

        Assert.Contains("Pojemność", ex.Message);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsActiveOnly_ByDefault()
    {
        using var db = CreateDb();
        var service = new RoomService(db);

        await service.CreateAsync(ValidCreateRequest());
        var toDelete = await service.CreateAsync(ValidCreateRequest() with { Name = "Sala B" });
        await service.DeleteAsync(toDelete.Id);

        var result = await service.GetAllAsync(activeOnly: true);

        Assert.Single(result);
        Assert.Equal("Sala A", result[0].Name);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAll_WhenActiveOnlyFalse()
    {
        using var db = CreateDb();
        var service = new RoomService(db);

        await service.CreateAsync(ValidCreateRequest());
        var toDelete = await service.CreateAsync(ValidCreateRequest() with { Name = "Sala B" });
        await service.DeleteAsync(toDelete.Id);

        var result = await service.GetAllAsync(activeOnly: false);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetAllAsync_OrdersByName()
    {
        using var db = CreateDb();
        var service = new RoomService(db);

        await service.CreateAsync(ValidCreateRequest() with { Name = "Zebra" });
        await service.CreateAsync(ValidCreateRequest() with { Name = "Alfa" });
        await service.CreateAsync(ValidCreateRequest() with { Name = "Beta" });

        var result = await service.GetAllAsync(activeOnly: true);

        Assert.Equal("Alfa", result[0].Name);
        Assert.Equal("Beta", result[1].Name);
        Assert.Equal("Zebra", result[2].Name);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingRoom_ReturnsRoom()
    {
        using var db = CreateDb();
        var service = new RoomService(db);
        var created = await service.CreateAsync(ValidCreateRequest());

        var room = await service.GetByIdAsync(created.Id);

        Assert.Equal(created.Id, room.Id);
        Assert.Equal("Sala A", room.Name);
    }

    [Fact]
    public async Task GetByIdAsync_UnknownId_Throws()
    {
        using var db = CreateDb();
        var service = new RoomService(db);

        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.GetByIdAsync(Guid.NewGuid()));

        Assert.Contains("nie istnieje", ex.Message);
    }

    [Fact]
    public async Task UpdateAsync_ExistingRoom_UpdatesFields()
    {
        using var db = CreateDb();
        var service = new RoomService(db);
        var created = await service.CreateAsync(ValidCreateRequest());

        var updated = await service.UpdateAsync(created.Id,
            new UpdateRoomRequest("Sala A Premium", "Nowy budynek", 30, "Nowy opis", "TV", true));

        Assert.Equal("Sala A Premium", updated.Name);
        Assert.Equal("Nowy budynek", updated.Location);
        Assert.Equal(30, updated.Capacity);
        Assert.Equal("TV", updated.Equipment);
    }

    [Fact]
    public async Task UpdateAsync_InvalidData_Throws()
    {
        using var db = CreateDb();
        var service = new RoomService(db);
        var created = await service.CreateAsync(ValidCreateRequest());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateAsync(created.Id,
                new UpdateRoomRequest("", "Lok", 10, null, null, true)));

        Assert.Contains("Nazwa", ex.Message);
    }

    [Fact]
    public async Task UpdateAsync_UnknownId_Throws()
    {
        using var db = CreateDb();
        var service = new RoomService(db);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.UpdateAsync(Guid.NewGuid(),
                new UpdateRoomRequest("X", "Y", 5, null, null, true)));
    }

    [Fact]
    public async Task DeleteAsync_ExistingRoom_SetsInactive()
    {
        using var db = CreateDb();
        var service = new RoomService(db);
        var created = await service.CreateAsync(ValidCreateRequest());

        await service.DeleteAsync(created.Id);

        var inDb = await db.Rooms.FindAsync(created.Id);
        Assert.NotNull(inDb);
        Assert.False(inDb!.IsActive);
    }

    [Fact]
    public async Task DeleteAsync_UnknownId_Throws()
    {
        using var db = CreateDb();
        var service = new RoomService(db);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.DeleteAsync(Guid.NewGuid()));
    }
}