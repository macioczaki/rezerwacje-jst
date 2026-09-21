namespace Rezerwacje.Domain.Entities;

public class PasswordResetToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    /// <summary>
    /// Hash tokenu (SHA-256). Nie trzymamy plaintextu — nawet jeśli baza wycieknie,
    /// atakujący nie może użyć skradzionych tokenów.
    /// </summary>
    public string TokenHash { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Kiedy token został wykorzystany. Null = jeszcze aktywny.
    /// Token jest jednorazowy — po użyciu nie da się go użyć ponownie.
    /// </summary>
    public DateTime? UsedAt { get; set; }

    public bool IsActive => UsedAt is null && ExpiresAt > DateTime.UtcNow;
}