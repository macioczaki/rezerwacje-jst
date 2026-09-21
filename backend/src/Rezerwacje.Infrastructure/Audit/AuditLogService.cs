using Microsoft.EntityFrameworkCore;
using Rezerwacje.Application.Audit;
using Rezerwacje.Application.Audit.Dtos;
using Rezerwacje.Infrastructure.Persistence;

namespace Rezerwacje.Infrastructure.Audit;

public class AuditLogService : IAuditLogService
{
    private readonly AppDbContext _db;

    public AuditLogService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<AuditLogDto>> GetAllAsync(
        string? entityType,
        Guid? entityId,
        Guid? userId,
        DateTime? from,
        DateTime? to,
        int limit,
        CancellationToken ct = default)
    {
        var query = _db.AuditLogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(entityType))
            query = query.Where(x => x.EntityType == entityType);

        if (entityId.HasValue)
            query = query.Where(x => x.EntityId == entityId.Value);

        if (userId.HasValue)
            query = query.Where(x => x.UserId == userId.Value);

        if (from.HasValue)
            query = query.Where(x => x.Timestamp >= from.Value);

        if (to.HasValue)
            query = query.Where(x => x.Timestamp <= to.Value);

        // Limit — żeby nie wyciągnąć całej tabeli na raz.
        var safeLimit = limit is > 0 and <= 500 ? limit : 100;

        return await query
            .OrderByDescending(x => x.Timestamp)
            .Take(safeLimit)
            .Select(x => new AuditLogDto(
                x.Id,
                x.UserId,
                x.UserEmail,
                x.EntityType,
                x.EntityId,
                x.Action.ToString(),
                x.Changes,
                x.Timestamp
            ))
            .ToListAsync(ct);
    }
}