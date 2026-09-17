using Microsoft.EntityFrameworkCore;
using Rezerwacje.Application.Reservations.Dtos;
using Rezerwacje.Domain.Entities;
using Rezerwacje.Infrastructure.Persistence;
using Rezerwacje.Infrastructure.Reservations;
using Xunit;

namespace Rezerwacje.UnitTests.Reservations;

public class ReservationServiceTests
{
    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static async Task<(Guid roomId, Guid userId)> SeedAsync(AppDbContext db)
    {
        var room = new Room
        {
            Name = "Sala A",
            Location = "Budynek główny",
            Capacity = 10,
            IsActive = true
        };
        var user = new User
        {
            Email = "user@example.com",
            PasswordHash = "x",
            FirstName = "Jan",
            LastName = "Kowalski",
            Role = UserRole.Employee,
            IsActive = true
        };
        db.Rooms.Add(room);
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return (room.Id, user.Id);
    }

    private static CreateReservationRequest NewRequest(Guid roomId, DateTime start, DateTime end, string title = "Test") =>
        new(roomId, title, start, end);

    private static DateTime FutureDate(int daysAhead = 1) =>
        DateTime.UtcNow.Date.AddDays(daysAhead);

    [Fact]
    public async Task CreateAsync_ValidRequest_CreatesReservation()
    {
        using var db = CreateDb();
        var (roomId, userId) = await SeedAsync(db);
        var service = new ReservationService(db);

        var start = FutureDate().AddHours(10);
        var end = start.AddHours(1);

        var result = await service.CreateAsync(userId, NewRequest(roomId, start, end));

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(roomId, result.RoomId);
        Assert.Equal(userId, result.UserId);
        Assert.Equal("Test", result.Title);
        Assert.Equal("Active", result.Status);
        Assert.Equal(1, await db.Reservations.CountAsync());
    }

    [Fact]
    public async Task CreateAsync_CollidingTime_Throws()
    {
        using var db = CreateDb();
        var (roomId, userId) = await SeedAsync(db);
        var service = new ReservationService(db);

        var start = FutureDate().AddHours(10);
        var end = start.AddHours(2);

        await service.CreateAsync(userId, NewRequest(roomId, start, end, "Pierwsza"));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(userId, NewRequest(roomId, start.AddMinutes(30), end.AddMinutes(-30), "Druga")));

        Assert.Contains("zajęta", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_AdjacentTime_DoesNotThrow()
    {
        using var db = CreateDb();
        var (roomId, userId) = await SeedAsync(db);
        var service = new ReservationService(db);

        var start = FutureDate().AddHours(10);
        var end = start.AddHours(1);

        await service.CreateAsync(userId, NewRequest(roomId, start, end));

        var result = await service.CreateAsync(userId, NewRequest(roomId, end, end.AddHours(1)));

        Assert.Equal("Active", result.Status);
        Assert.Equal(2, await db.Reservations.CountAsync());
    }

    [Fact]
    public async Task CreateAsync_DifferentRooms_DoNotCollide()
    {
        using var db = CreateDb();
        var (roomId1, userId) = await SeedAsync(db);

        var room2 = new Room { Name = "Sala B", Location = "Budynek A", Capacity = 5, IsActive = true };
        db.Rooms.Add(room2);
        await db.SaveChangesAsync();

        var service = new ReservationService(db);
        var start = FutureDate().AddHours(10);
        var end = start.AddHours(1);

        await service.CreateAsync(userId, NewRequest(roomId1, start, end));
        await service.CreateAsync(userId, NewRequest(room2.Id, start, end));

        Assert.Equal(2, await db.Reservations.CountAsync());
    }

    [Fact]
    public async Task CreateAsync_EndBeforeStart_Throws()
    {
        using var db = CreateDb();
        var (roomId, userId) = await SeedAsync(db);
        var service = new ReservationService(db);

        var start = FutureDate().AddHours(10);
        var end = start.AddHours(-1);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(userId, NewRequest(roomId, start, end)));

        Assert.Contains("późniejsza", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_PastDate_Throws()
    {
        using var db = CreateDb();
        var (roomId, userId) = await SeedAsync(db);
        var service = new ReservationService(db);

        var start = DateTime.UtcNow.AddHours(-5);
        var end = start.AddHours(1);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(userId, NewRequest(roomId, start, end)));

        Assert.Contains("przeszłości", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_LongerThan24h_Throws()
    {
        using var db = CreateDb();
        var (roomId, userId) = await SeedAsync(db);
        var service = new ReservationService(db);

        var start = FutureDate().AddHours(10);
        var end = start.AddHours(25);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(userId, NewRequest(roomId, start, end)));

        Assert.Contains("24", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_UnknownRoom_Throws()
    {
        using var db = CreateDb();
        var (_, userId) = await SeedAsync(db);
        var service = new ReservationService(db);

        var start = FutureDate().AddHours(10);
        var end = start.AddHours(1);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.CreateAsync(userId, NewRequest(Guid.NewGuid(), start, end)));
    }

    [Fact]
    public async Task CreateAsync_InactiveRoom_Throws()
    {
        using var db = CreateDb();
        var (roomId, userId) = await SeedAsync(db);

        var room = await db.Rooms.FindAsync(roomId);
        room!.IsActive = false;
        await db.SaveChangesAsync();

        var service = new ReservationService(db);
        var start = FutureDate().AddHours(10);
        var end = start.AddHours(1);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(userId, NewRequest(roomId, start, end)));

        Assert.Contains("nieaktywna", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_EmptyTitle_Throws()
    {
        using var db = CreateDb();
        var (roomId, userId) = await SeedAsync(db);
        var service = new ReservationService(db);

        var start = FutureDate().AddHours(10);
        var end = start.AddHours(1);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(userId, NewRequest(roomId, start, end, "   ")));

        Assert.Contains("Tytuł", ex.Message);
    }

    [Fact]
    public async Task CancelAsync_OwnReservation_SetsCancelled()
    {
        using var db = CreateDb();
        var (roomId, userId) = await SeedAsync(db);
        var service = new ReservationService(db);

        var start = FutureDate().AddHours(10);
        var end = start.AddHours(1);
        var created = await service.CreateAsync(userId, NewRequest(roomId, start, end));

        await service.CancelAsync(created.Id, userId, isAdmin: false);

        var inDb = await db.Reservations.FindAsync(created.Id);
        Assert.NotNull(inDb);
        Assert.Equal(ReservationStatus.Cancelled, inDb!.Status);
    }

    [Fact]
    public async Task CancelAsync_OtherUsersReservation_WithoutAdmin_Throws()
    {
        using var db = CreateDb();
        var (roomId, userId) = await SeedAsync(db);
        var service = new ReservationService(db);

        var start = FutureDate().AddHours(10);
        var end = start.AddHours(1);
        var created = await service.CreateAsync(userId, NewRequest(roomId, start, end));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.CancelAsync(created.Id, Guid.NewGuid(), isAdmin: false));
    }

    [Fact]
    public async Task CancelAsync_OtherUsersReservation_AsAdmin_Succeeds()
    {
        using var db = CreateDb();
        var (roomId, userId) = await SeedAsync(db);
        var service = new ReservationService(db);

        var start = FutureDate().AddHours(10);
        var end = start.AddHours(1);
        var created = await service.CreateAsync(userId, NewRequest(roomId, start, end));

        await service.CancelAsync(created.Id, Guid.NewGuid(), isAdmin: true);

        var inDb = await db.Reservations.FindAsync(created.Id);
        Assert.Equal(ReservationStatus.Cancelled, inDb!.Status);
    }

    [Fact]
    public async Task CancelAsync_AlreadyCancelled_Throws()
    {
        using var db = CreateDb();
        var (roomId, userId) = await SeedAsync(db);
        var service = new ReservationService(db);

        var start = FutureDate().AddHours(10);
        var end = start.AddHours(1);
        var created = await service.CreateAsync(userId, NewRequest(roomId, start, end));

        await service.CancelAsync(created.Id, userId, isAdmin: false);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CancelAsync(created.Id, userId, isAdmin: false));

        Assert.Contains("anulowana", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_AfterCancellation_NoCollision()
    {
        using var db = CreateDb();
        var (roomId, userId) = await SeedAsync(db);
        var service = new ReservationService(db);

        var start = FutureDate().AddHours(10);
        var end = start.AddHours(1);
        var first = await service.CreateAsync(userId, NewRequest(roomId, start, end));

        await service.CancelAsync(first.Id, userId, isAdmin: false);

        var second = await service.CreateAsync(userId, NewRequest(roomId, start, end, "Nowa"));

        Assert.Equal("Active", second.Status);
    }

    [Fact]
    public async Task UpdateAsync_OwnReservation_UpdatesFields()
    {
        using var db = CreateDb();
        var (roomId, userId) = await SeedAsync(db);
        var service = new ReservationService(db);

        var start = FutureDate().AddHours(10);
        var end = start.AddHours(1);
        var created = await service.CreateAsync(userId, NewRequest(roomId, start, end, "Stara"));

        var newStart = FutureDate().AddHours(14);
        var newEnd = newStart.AddHours(2);

        var updated = await service.UpdateAsync(created.Id, userId, isAdmin: false,
            new UpdateReservationRequest("Nowa", newStart, newEnd));

        Assert.Equal("Nowa", updated.Title);
        Assert.Equal(newStart, updated.StartTime);
        Assert.Equal(newEnd, updated.EndTime);
    }

    [Fact]
    public async Task UpdateAsync_ToCollidingTime_Throws()
    {
        using var db = CreateDb();
        var (roomId, userId) = await SeedAsync(db);
        var service = new ReservationService(db);

        var start = FutureDate().AddHours(10);
        await service.CreateAsync(userId, NewRequest(roomId, start, start.AddHours(1), "Pierwsza"));
        var second = await service.CreateAsync(userId, NewRequest(roomId, start.AddHours(2), start.AddHours(3), "Druga"));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateAsync(second.Id, userId, isAdmin: false,
                new UpdateReservationRequest("Druga", start.AddMinutes(30), start.AddMinutes(90))));

        Assert.Contains("zajęta", ex.Message);
    }

    [Fact]
    public async Task UpdateAsync_OwnReservation_WithSameTime_DoesNotCollideWithItself()
    {
        using var db = CreateDb();
        var (roomId, userId) = await SeedAsync(db);
        var service = new ReservationService(db);

        var start = FutureDate().AddHours(10);
        var end = start.AddHours(1);
        var created = await service.CreateAsync(userId, NewRequest(roomId, start, end, "Stara"));

        var updated = await service.UpdateAsync(created.Id, userId, isAdmin: false,
            new UpdateReservationRequest("Nowa", start, end));

        Assert.Equal("Nowa", updated.Title);
    }

    [Fact]
    public async Task GetAvailabilityAsync_ReturnsOnlyActiveForDay()
    {
        using var db = CreateDb();
        var (roomId, userId) = await SeedAsync(db);
        var service = new ReservationService(db);

        var day = FutureDate();
        await service.CreateAsync(userId, NewRequest(roomId, day.AddHours(10), day.AddHours(11), "Rano"));
        var cancelled = await service.CreateAsync(userId, NewRequest(roomId, day.AddHours(14), day.AddHours(15), "Południe"));
        await service.CancelAsync(cancelled.Id, userId, isAdmin: false);

        var result = await service.GetAvailabilityAsync(roomId, day);

        Assert.Single(result.BusySlots);
        Assert.Equal("Rano", result.BusySlots[0].Title);
    }

    [Fact]
    public async Task GetAvailabilityAsync_ExcludesOtherDays()
    {
        using var db = CreateDb();
        var (roomId, userId) = await SeedAsync(db);
        var service = new ReservationService(db);

        var day = FutureDate();
        var otherDay = day.AddDays(1);

        await service.CreateAsync(userId, NewRequest(roomId, day.AddHours(10), day.AddHours(11)));
        await service.CreateAsync(userId, NewRequest(roomId, otherDay.AddHours(10), otherDay.AddHours(11)));

        var result = await service.GetAvailabilityAsync(roomId, day);

        Assert.Single(result.BusySlots);
    }

    [Fact]
    public async Task GetAvailabilityAsync_UnknownRoom_Throws()
    {
        using var db = CreateDb();
        var service = new ReservationService(db);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.GetAvailabilityAsync(Guid.NewGuid(), FutureDate()));
    }
}