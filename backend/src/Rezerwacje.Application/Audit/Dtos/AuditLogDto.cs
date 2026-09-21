namespace Rezerwacje.Application.Audit.Dtos;

public record AuditLogDto(
    Guid Id,
    Guid? UserId,
    string? UserEmail,
    string EntityType,
    Guid EntityId,
    string Action,
    string Changes,
    DateTime Timestamp
);