using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Rezerwacje.Application.Common;
using Rezerwacje.Domain.Entities;

namespace Rezerwacje.Infrastructure.Persistence;

public class AuditInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUserService _currentUser;

    public AuditInterceptor(ICurrentUserService currentUser)
    {
        _currentUser = currentUser;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        AddAuditLogs(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        AddAuditLogs(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void AddAuditLogs(DbContext? context)
    {
        if (context is null) return;

        var entries = context.ChangeTracker.Entries()
            .Where(e =>
                e.Entity is not AuditLog
                && (e.State == EntityState.Added
                    || e.State == EntityState.Modified
                    || e.State == EntityState.Deleted))
            .ToList();

        if (entries.Count == 0) return;

        var userId = _currentUser.UserId;
        var email = _currentUser.Email;
        var now = DateTime.UtcNow;

        foreach (var entry in entries)
        {
            var action = entry.State switch
            {
                EntityState.Added => AuditAction.Created,
                EntityState.Modified => AuditAction.Updated,
                EntityState.Deleted => AuditAction.Deleted,
                _ => AuditAction.Updated
            };

            var entityId = GetEntityId(entry);
            var entityType = entry.Entity.GetType().Name;
            var changes = BuildChanges(entry, action);

            context.Add(new AuditLog
            {
                UserId = userId,
                UserEmail = email,
                EntityType = entityType,
                EntityId = entityId,
                Action = action,
                Changes = changes,
                Timestamp = now
            });
        }
    }

    private static Guid GetEntityId(EntityEntry entry)
    {
        var idProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "Id");
        if (idProp?.CurrentValue is Guid id) return id;
        if (idProp?.OriginalValue is Guid original) return original;
        return Guid.Empty;
    }

    private static string BuildChanges(EntityEntry entry, AuditAction action)
    {
        var data = new Dictionary<string, object?>();

        if (action == AuditAction.Updated)
        {
            // Tylko zmienione pola
            var changed = entry.Properties
                .Where(p => p.IsModified && !Equals(p.CurrentValue, p.OriginalValue))
                .ToDictionary(
                    p => p.Metadata.Name,
                    p => (object?)new { before = p.OriginalValue, after = p.CurrentValue });

            data["fields"] = changed;
        }
        else if (action == AuditAction.Deleted)
        {
            // Snapshot wszystkich pól
            foreach (var prop in entry.Properties)
            {
                data[prop.Metadata.Name] = prop.OriginalValue;
            }
        }
        else // Created
        {
            foreach (var prop in entry.Properties)
            {
                data[prop.Metadata.Name] = prop.CurrentValue;
            }
        }

        return JsonSerializer.Serialize(data, new JsonSerializerOptions
        {
            WriteIndented = false
        });
    }
}