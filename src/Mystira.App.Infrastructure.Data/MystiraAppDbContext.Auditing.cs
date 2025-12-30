using Microsoft.EntityFrameworkCore;
using Mystira.Domain.Models;

namespace Mystira.App.Infrastructure.Data;

/// <summary>
/// Partial class for MystiraAppDbContext - Auditing and Soft Delete functionality
/// </summary>
public partial class MystiraAppDbContext
{
    /// <summary>
    /// Current user ID for audit tracking (set by middleware/service)
    /// </summary>
    public string? CurrentUserId { get; set; }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditInformation();
        return await base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        ApplyAuditInformation();
        return base.SaveChanges();
    }

    private void ApplyAuditInformation()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is AuditableEntity && (
                e.State == EntityState.Added ||
                e.State == EntityState.Modified ||
                e.State == EntityState.Deleted));

        var utcNow = DateTime.UtcNow;

        foreach (var entry in entries)
        {
            var entity = (AuditableEntity)entry.Entity;

            switch (entry.State)
            {
                case EntityState.Added:
                    entity.CreatedAt = utcNow;
                    entity.CreatedBy = CurrentUserId;
                    break;

                case EntityState.Modified:
                    entity.UpdatedAt = utcNow;
                    entity.UpdatedBy = CurrentUserId;
                    break;

                case EntityState.Deleted:
                    // Convert hard deletes to soft deletes for SoftDeletableEntity
                    if (entity is SoftDeletableEntity softDeletableEntity)
                    {
                        entry.State = EntityState.Modified;  // Don't actually delete
                        softDeletableEntity.Delete(CurrentUserId);
                    }
                    break;
            }
        }
    }
}
