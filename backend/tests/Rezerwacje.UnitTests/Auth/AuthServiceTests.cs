using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Rezerwacje.Application.Auth.Dtos;
using Rezerwacje.Infrastructure.Auth;
using Rezerwacje.Infrastructure.Persistence;
using Xunit;

namespace Rezerwacje.UnitTests.Auth;

public class AuthServiceTests
{
    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static IConfiguration CreateConfig()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "TEST_KEY_AT_LEAST_32_CHARS_LONG_FOR_TESTS_1234567890",
                ["Jwt:Issuer"] = "rezerwacje-jst-test",
                ["Jwt:Audience"] = "rezerwacje-jst-test"
            })
            .Build();
    }

    [Fact]
    public async Task RegisterAsync_ValidRequest_CreatesUserAndReturnsToken()
    {
        // Arrange
        using var db = CreateDb();
        var service = new AuthService(db, CreateConfig());
        var request = new RegisterRequest("jan@example.com", "Haslo123!", "Jan", "Kowalski");

        // Act
        var response = await service.RegisterAsync(request);

        // Assert
        Assert.NotNull(response);
        Assert.False(string.IsNullOrWhiteSpace(response.AccessToken));
        Assert.Equal("jan@example.com", response.Email);
        Assert.Equal("Employee", response.Role);

        var userInDb = await db.Users.SingleAsync();
        Assert.Equal("jan@example.com", userInDb.Email);
        Assert.NotEqual("Haslo123!", userInDb.PasswordHash);
        Assert.StartsWith("$2", userInDb.PasswordHash);
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_Throws()
    {
        using var db = CreateDb();
        var service = new AuthService(db, CreateConfig());
        var request = new RegisterRequest("dup@example.com", "Haslo123!", "Jan", "Kowalski");

        await service.RegisterAsync(request);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.RegisterAsync(request));

        Assert.Contains("już istnieje", ex.Message);
    }

    [Fact]
    public async Task RegisterAsync_EmailIsNormalizedToLowercase()
    {
        using var db = CreateDb();
        var service = new AuthService(db, CreateConfig());
        var request = new RegisterRequest("JAN@EXAMPLE.COM", "Haslo123!", "Jan", "Kowalski");

        var response = await service.RegisterAsync(request);

        Assert.Equal("jan@example.com", response.Email);
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsToken()
    {
        using var db = CreateDb();
        var service = new AuthService(db, CreateConfig());

        await service.RegisterAsync(
            new RegisterRequest("login@example.com", "Haslo123!", "Jan", "Kowalski"));

        var response = await service.LoginAsync(
            new LoginRequest("login@example.com", "Haslo123!"));

        Assert.NotNull(response);
        Assert.False(string.IsNullOrWhiteSpace(response.AccessToken));
        Assert.Equal("login@example.com", response.Email);
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_Throws()
    {
        using var db = CreateDb();
        var service = new AuthService(db, CreateConfig());

        await service.RegisterAsync(
            new RegisterRequest("wp@example.com", "Haslo123!", "Jan", "Kowalski"));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.LoginAsync(new LoginRequest("wp@example.com", "ZleHaslo!")));

        Assert.Contains("Nieprawidłowe", ex.Message);
    }

    [Fact]
    public async Task LoginAsync_UnknownEmail_Throws()
    {
        using var db = CreateDb();
        var service = new AuthService(db, CreateConfig());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.LoginAsync(new LoginRequest("nie.ma@example.com", "Haslo123!")));

        Assert.Contains("Nieprawidłowe", ex.Message);
    }

    [Fact]
    public async Task LoginAsync_InactiveUser_Throws()
    {
        using var db = CreateDb();
        var service = new AuthService(db, CreateConfig());

        await service.RegisterAsync(
            new RegisterRequest("inactive@example.com", "Haslo123!", "Jan", "Kowalski"));

        var user = await db.Users.SingleAsync();
        user.IsActive = false;
        await db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.LoginAsync(new LoginRequest("inactive@example.com", "Haslo123!")));

        Assert.Contains("nieaktywne", ex.Message);
    }
}