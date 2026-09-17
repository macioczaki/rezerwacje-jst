using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Rezerwacje.Domain.Entities;

namespace Rezerwacje.Infrastructure.Persistence;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db, ILogger logger)
    {
        // Migracje
        await db.Database.MigrateAsync();

        // Jeśli są już jacyś użytkownicy — nie seedujemy ponownie.
        if (await db.Users.AnyAsync())
        {
            logger.LogInformation("Baza już zawiera dane — pomijam seed.");
            return;
        }

        logger.LogInformation("Seedowanie danych startowych...");

        var admin = new User
        {
            Email = "admin@example.com",
            FirstName = "Adam",
            LastName = "Administrator",
            Role = UserRole.Admin,
            IsActive = true,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!")
        };

        var employee = new User
        {
            Email = "pracownik@example.com",
            FirstName = "Jan",
            LastName = "Pracownik",
            Role = UserRole.Employee,
            IsActive = true,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Pracownik123!")
        };

        db.Users.AddRange(admin, employee);

        var rooms = new[]
        {
            new Room
            {
                Name = "Sala konferencyjna A",
                Location = "Budynek główny, I piętro",
                Capacity = 20,
                Description = "Największa sala konferencyjna w budynku.",
                Equipment = "Projektor, tablica suchościeralna, telewizor 65\"",
                IsActive = true
            },
            new Room
            {
                Name = "Sala szkoleniowa B",
                Location = "Budynek główny, parter",
                Capacity = 12,
                Description = "Sala do szkoleń i prezentacji.",
                Equipment = "Projektor, flipchart, Wi-Fi",
                IsActive = true
            },
            new Room
            {
                Name = "Pokój spotkań C",
                Location = "Budynek główny, II piętro",
                Capacity = 6,
                Description = "Kameralny pokój do szybkich spotkań zespołu.",
                Equipment = "Telewizor, wideokonferencja",
                IsActive = true
            },
            new Room
            {
                Name = "Biuro obsługi klienta",
                Location = "Budynek B, parter",
                Capacity = 4,
                Description = "Pokój do spotkań z interesantami.",
                Equipment = "Komputer, drukarka, skaner",
                IsActive = true
            },
            new Room
            {
                Name = "Sala archiwalna (nieaktywna)",
                Location = "Budynek C, piwnica",
                Capacity = 2,
                Description = "Sala wyłączona z użytku — przechowywanie dokumentów.",
                Equipment = null,
                IsActive = false
            }
        };

        db.Rooms.AddRange(rooms);

        await db.SaveChangesAsync();

        // Przykładowa rezerwacja na jutro, żeby endpoint availability miał co zwracać.
        var tomorrow = DateTime.UtcNow.Date.AddDays(1);
        var exampleReservation = new Reservation
        {
            RoomId = rooms[0].Id,
            UserId = employee.Id,
            Title = "Spotkanie zespołu projektowego",
            StartTime = tomorrow.AddHours(10),
            EndTime = tomorrow.AddHours(11),
            Status = ReservationStatus.Active
        };

        db.Reservations.Add(exampleReservation);
        await db.SaveChangesAsync();

        logger.LogInformation("Seed zakończony. Utworzono {Users} użytkowników, {Rooms} sal i {Reservations} rezerwację.",
            await db.Users.CountAsync(),
            await db.Rooms.CountAsync(),
            await db.Reservations.CountAsync());
    }
}