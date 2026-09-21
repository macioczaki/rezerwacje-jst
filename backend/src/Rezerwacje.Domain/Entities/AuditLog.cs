namespace Rezerwacje.Domain.Entities;

public enum AuditAction
{
    Created = 0,
    Updated = 1,
    Deleted = 2
}

public class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// ID użytkownika, który wykonał akcję. Null dla akcji systemowych (np. seed).
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Email zalogowanego użytkownika — denormalizowany, żeby log był czytelny
    /// nawet gdyby konto zostało później usunięte.
    /// </summary>
    public string? UserEmail { get; set; }

    /// <summary>
    /// Typ encji, której dotyczy zmiana ("Room", "Reservation", "User").
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// ID zmienionej encji.
    /// </summary>
    public Guid EntityId { get; set; }

    /// <summary>
    /// Rodzaj akcji: Created, Updated, Deleted.
    /// </summary>
    public AuditAction Action { get; set; }

    /// <summary>
    /// JSON ze szczegółami zmiany: dla Updated — lista pól i wartości przed/po;
    /// dla Created/Deleted — kluczowe pola encji.
    /// </summary>
    public string Changes { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}