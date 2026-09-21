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
        [Fact]
    public async Task LoginAsync_ReturnsRefreshToken()
    {
        using var db = CreateDb();
        var service = new AuthService(db, CreateConfig());
        await service.RegisterAsync(
            new RegisterRequest("rt@example.com", "Haslo123!", "Jan", "Kowalski"));

        var response = await service.LoginAsync(
            new LoginRequest("rt@example.com", "Haslo123!"));

        Assert.False(string.IsNullOrWhiteSpace(response.RefreshToken));
        Assert.True(response.RefreshTokenExpiresAt > DateTime.UtcNow);
        Assert.True(response.AccessTokenExpiresAt > DateTime.UtcNow);
        Assert.Equal(2, await db.RefreshTokens.CountAsync());
    }

    [Fact]
    public async Task RegisterAsync_CreatesRefreshToken()
    {
        using var db = CreateDb();
        var service = new AuthService(db, CreateConfig());

        var response = await service.RegisterAsync(
            new RegisterRequest("rt2@example.com", "Haslo123!", "Jan", "Kowalski"));

        Assert.False(string.IsNullOrWhiteSpace(response.RefreshToken));

        var token = await db.RefreshTokens.SingleAsync();
        Assert.Null(token.RevokedAt);
        Assert.True(token.ExpiresAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task RefreshAsync_ValidToken_ReturnsNewTokens()
    {
        using var db = CreateDb();
        var service = new AuthService(db, CreateConfig());
        var registered = await service.RegisterAsync(
            new RegisterRequest("refresh@example.com", "Haslo123!", "Jan", "Kowalski"));

        var refreshed = await service.RefreshAsync(registered.RefreshToken);

        Assert.False(string.IsNullOrWhiteSpace(refreshed.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(refreshed.RefreshToken));
        Assert.NotEqual(registered.RefreshToken, refreshed.RefreshToken);
        Assert.Equal(2, await db.RefreshTokens.CountAsync());
    }

    [Fact]
    public async Task RefreshAsync_ValidToken_RevokesOldToken()
    {
        using var db = CreateDb();
        var service = new AuthService(db, CreateConfig());
        var registered = await service.RegisterAsync(
            new RegisterRequest("revoke@example.com", "Haslo123!", "Jan", "Kowalski"));

        await service.RefreshAsync(registered.RefreshToken);

        var tokens = await db.RefreshTokens.OrderBy(t => t.CreatedAt).ToListAsync();
        Assert.NotNull(tokens[0].RevokedAt);       // stary unieważniony
        Assert.Null(tokens[1].RevokedAt);          // nowy aktywny
        Assert.Equal(tokens[1].Id, tokens[0].ReplacedByTokenId);
    }

    [Fact]
    public async Task RefreshAsync_ReusedToken_ThrowsAndRevokesAll()
    {
        using var db = CreateDb();
        var service = new AuthService(db, CreateConfig());
        var registered = await service.RegisterAsync(
            new RegisterRequest("reuse@example.com", "Haslo123!", "Jan", "Kowalski"));

        // Pierwszy refresh — token stary zostaje unieważniony, powstaje nowy
        var refreshed = await service.RefreshAsync(registered.RefreshToken);

        // Próba ponownego użycia starego tokenu
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.RefreshAsync(registered.RefreshToken));

        Assert.Contains("został już użyty", ex.Message);

        // Wszystkie aktywne tokeny użytkownika unieważnione
        var activeTokens = await db.RefreshTokens
            .Where(t => t.RevokedAt == null)
            .ToListAsync();
        Assert.Empty(activeTokens);
    }

    [Fact]
    public async Task RefreshAsync_UnknownToken_Throws()
    {
        using var db = CreateDb();
        var service = new AuthService(db, CreateConfig());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.RefreshAsync("nie-istnieje"));

        Assert.Contains("Nieprawidłowy", ex.Message);
    }

    [Fact]
    public async Task RefreshAsync_ExpiredToken_Throws()
    {
        using var db = CreateDb();
        var service = new AuthService(db, CreateConfig());
        var registered = await service.RegisterAsync(
            new RegisterRequest("expired@example.com", "Haslo123!", "Jan", "Kowalski"));

        // Ręcznie ustawiamy token jako wygasły
        var token = await db.RefreshTokens.SingleAsync();
        token.ExpiresAt = DateTime.UtcNow.AddDays(-1);
        await db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.RefreshAsync(registered.RefreshToken));

        Assert.Contains("wygasł", ex.Message);
    }

    [Fact]
    public async Task RefreshAsync_EmptyToken_Throws()
    {
        using var db = CreateDb();
        var service = new AuthService(db, CreateConfig());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.RefreshAsync(""));
    }

    [Fact]
    public async Task LogoutAsync_ValidToken_RevokesToken()
    {
        using var db = CreateDb();
        var service = new AuthService(db, CreateConfig());
        var registered = await service.RegisterAsync(
            new RegisterRequest("logout@example.com", "Haslo123!", "Jan", "Kowalski"));

        await service.LogoutAsync(registered.RefreshToken);

        var token = await db.RefreshTokens.SingleAsync();
        Assert.NotNull(token.RevokedAt);
    }

    [Fact]
    public async Task LogoutAsync_UnknownToken_DoesNotThrow()
    {
        using var db = CreateDb();
        var service = new AuthService(db, CreateConfig());

        // Idempotentne — brak błędu gdy token nie istnieje
        await service.LogoutAsync("nie-istnieje");
    }

    [Fact]
    public async Task LogoutAsync_RevokedToken_DoesNotThrow()
    {
        using var db = CreateDb();
        var service = new AuthService(db, CreateConfig());
        var registered = await service.RegisterAsync(
            new RegisterRequest("logout2@example.com", "Haslo123!", "Jan", "Kowalski"));

        await service.LogoutAsync(registered.RefreshToken);
        await service.LogoutAsync(registered.RefreshToken); // drugi raz — bez błędu
    }
}