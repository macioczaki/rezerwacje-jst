namespace Rezerwacje.Domain.Entities;

public class RefreshToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    /// <summary>
    /// Hash tokenu (BCrypt). Nie trzymamy plaintextu — gdyby baza wyciekła,
    /// atakujący nie może użyć skradzionych tokenów.
    /// </summary>
    public string TokenHash { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Kiedy token został unieważniony (przy logout, rotacji lub wykryciu nadużycia).
    /// </summary>
    public DateTime? RevokedAt { get; set; }

    /// <summary>
    /// Token, który zastąpił ten przy rotacji. Używane do wykrywania reuse.
    /// </summary>
    public Guid? ReplacedByTokenId { get; set; }

    public bool IsActive => RevokedAt is null && ExpiresAt > DateTime.UtcNow;
}