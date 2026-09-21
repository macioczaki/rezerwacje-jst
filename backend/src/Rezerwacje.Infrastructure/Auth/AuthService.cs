using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Rezerwacje.Application.Auth;
using Rezerwacje.Application.Auth.Dtos;
using Rezerwacje.Domain.Entities;
using Rezerwacje.Infrastructure.Persistence;
using Rezerwacje.Application.Common;
using Rezerwacje.Infrastructure.Email;

namespace Rezerwacje.Infrastructure.Auth;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly IEmailSender _email;

    public AuthService(AppDbContext db, IConfiguration config, IEmailSender email)
{
    _db = db;
    _config = config;
    _email = email;
}

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        if (await _db.Users.AnyAsync(u => u.Email == email, ct))
            throw new InvalidOperationException("Użytkownik z tym adresem e-mail już istnieje.");

        var user = new User
        {
            Email = email,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = UserRole.Employee
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        return await IssueTokensAsync(user, ct);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email, ct)
            ?? throw new InvalidOperationException("Nieprawidłowe dane logowania.");

        if (!user.IsActive)
            throw new InvalidOperationException("Konto jest nieaktywne.");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new InvalidOperationException("Nieprawidłowe dane logowania.");

        return await IssueTokensAsync(user, ct);
    }

    public async Task<AuthResponse> RefreshAsync(string refreshToken, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            throw new InvalidOperationException("Refresh token jest wymagany.");

        var hash = HashToken(refreshToken);

        var entity = await _db.RefreshTokens
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.TokenHash == hash, ct)
            ?? throw new InvalidOperationException("Nieprawidłowy refresh token.");

        if (entity.RevokedAt is not null)
        {
            var activeTokens = await _db.RefreshTokens
                .Where(r => r.UserId == entity.UserId && r.RevokedAt == null)
                .ToListAsync(ct);

            foreach (var t in activeTokens)
                t.RevokedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);

            throw new InvalidOperationException(
                "Refresh token został już użyty. Ze względów bezpieczeństwa wszystkie sesje zostały zakończone.");
        }

        if (entity.ExpiresAt <= DateTime.UtcNow)
            throw new InvalidOperationException("Refresh token wygasł. Zaloguj się ponownie.");

        if (!entity.User.IsActive)
            throw new InvalidOperationException("Konto jest nieaktywne.");

        return await IssueTokensAsync(entity.User, ct, toReplace: entity);
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return;

        var hash = HashToken(refreshToken);
        var entity = await _db.RefreshTokens.FirstOrDefaultAsync(r => r.TokenHash == hash, ct);

        if (entity is null || entity.RevokedAt is not null)
            return;

        entity.RevokedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }
    public async Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken ct = default)
    {
    var email = request.Email.Trim().ToLowerInvariant();

    var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

    // Bezpieczeństwo: nie ujawniamy, czy email istnieje w bazie.
    // Nawet jeśli użytkownik nie istnieje, kończymy sukcesem.
    if (user is null || !user.IsActive)
        return;

    // Unieważnij poprzednie aktywne tokeny resetu (jeśli były).
    var oldTokens = await _db.PasswordResetTokens
        .Where(t => t.UserId == user.Id && t.UsedAt == null)
        .ToListAsync(ct);
    foreach (var old in oldTokens)
        old.UsedAt = DateTime.UtcNow;

    var minutes = int.TryParse(_config["App:PasswordResetTokenMinutes"], out var m) ? m : 30;
    var plainToken = GenerateRandomToken();
    var resetToken = new PasswordResetToken
    {
        UserId = user.Id,
        TokenHash = HashToken(plainToken),
        ExpiresAt = DateTime.UtcNow.AddMinutes(minutes)
    };

    _db.PasswordResetTokens.Add(resetToken);
    await _db.SaveChangesAsync(ct);

    var frontendUrl = _config["App:FrontendUrl"] ?? "http://localhost:5173";
    var resetLink = $"{frontendUrl.TrimEnd('/')}/reset-password?token={Uri.EscapeDataString(plainToken)}";

    var html = EmailTemplates.PasswordReset(resetLink, minutes);
    await _email.SendAsync(user.Email, "Reset hasła — Rezerwacje JST", html, ct);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
            throw new InvalidOperationException("Token jest wymagany.");

        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 6)
            throw new InvalidOperationException("Hasło musi mieć co najmniej 6 znaków.");

        var hash = HashToken(request.Token);

        var token = await _db.PasswordResetTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == hash, ct)
            ?? throw new InvalidOperationException("Nieprawidłowy token resetu hasła.");

        if (token.UsedAt is not null)
            throw new InvalidOperationException("Token został już wykorzystany.");

        if (token.ExpiresAt <= DateTime.UtcNow)
            throw new InvalidOperationException("Token wygasł. Poproś o nowy link.");

        if (!token.User.IsActive)
            throw new InvalidOperationException("Konto jest nieaktywne.");

        // Ustaw nowe hasło
        token.User.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);

        // Oznacz token jako wykorzystany
        token.UsedAt = DateTime.UtcNow;

        // Bezpieczeństwo: unieważnij wszystkie aktywne refresh tokeny użytkownika.
        // Jeśli ktoś przejął konto, wylogowujemy wszystkie sesje.
        var activeRefreshTokens = await _db.RefreshTokens
            .Where(t => t.UserId == token.UserId && t.RevokedAt == null)
            .ToListAsync(ct);
        foreach (var rt in activeRefreshTokens)
            rt.RevokedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
    }

    private async Task<AuthResponse> IssueTokensAsync(User user, CancellationToken ct, RefreshToken? toReplace = null)
    {
        var accessToken = GenerateAccessToken(user, out var accessExpiresAt);

        var refreshDays = int.TryParse(_config["Jwt:RefreshTokenDays"], out var rd) ? rd : 7;
        var refreshExpiresAt = DateTime.UtcNow.AddDays(refreshDays);
        var plainRefreshToken = GenerateRandomToken();

        var refreshEntity = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = HashToken(plainRefreshToken),
            ExpiresAt = refreshExpiresAt
        };

        _db.RefreshTokens.Add(refreshEntity);
        await _db.SaveChangesAsync(ct);

        if (toReplace is not null)
        {
            toReplace.RevokedAt = DateTime.UtcNow;
            toReplace.ReplacedByTokenId = refreshEntity.Id;
            await _db.SaveChangesAsync(ct);
        }

        return new AuthResponse(
            accessToken,
            accessExpiresAt,
            plainRefreshToken,
            refreshExpiresAt,
            user.Email,
            user.Role.ToString()
        );
    }

    private string GenerateAccessToken(User user, out DateTime expiresAt)
    {
        var key = _config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is not configured.");
        var issuer = _config["Jwt:Issuer"] ?? "rezerwacje-jst";
        var audience = _config["Jwt:Audience"] ?? "rezerwacje-jst";
        var minutes = int.TryParse(_config["Jwt:AccessTokenMinutes"], out var m) ? m : 15;

        expiresAt = DateTime.UtcNow.AddMinutes(minutes);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateRandomToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }
}