using Rezerwacje.Application.Audit.Dtos;

namespace Rezerwacje.Application.Audit;

public interface IAuditLogService
{
    Task<IReadOnlyList<AuditLogDto>> GetAllAsync(
        string? entityType,
        Guid? entityId,
        Guid? userId,
        DateTime? from,
        DateTime? to,
        int limit,
        CancellationToken ct = default);
}