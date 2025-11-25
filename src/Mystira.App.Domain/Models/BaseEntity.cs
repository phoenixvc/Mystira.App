namespace Mystira.App.Domain.Models;

/// <summary>
/// Base entity with common Id property
/// </summary>
public abstract class Entity
{
    public string Id { get; set; } = string.Empty;
}

/// <summary>
/// Auditable entity with automatic tracking of creation and modification
/// </summary>
public abstract class AuditableEntity : Entity
{
    /// <summary>
    /// When the entity was created (UTC)
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Who created the entity (user ID or system identifier)
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// When the entity was last updated (UTC)
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Who last updated the entity (user ID or system identifier)
    /// </summary>
    public string? UpdatedBy { get; set; }
}

/// <summary>
/// Soft-deletable entity that supports logical deletion instead of physical deletion
/// </summary>
public abstract class SoftDeletableEntity : AuditableEntity
{
    /// <summary>
    /// Indicates if this entity has been soft-deleted
    /// </summary>
    public bool IsDeleted { get; set; } = false;

    /// <summary>
    /// When the entity was soft-deleted (UTC)
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Who soft-deleted the entity (user ID or system identifier)
    /// </summary>
    public string? DeletedBy { get; set; }

    /// <summary>
    /// Soft delete this entity
    /// </summary>
    public virtual void Delete(string? deletedBy = null)
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        DeletedBy = deletedBy;
    }

    /// <summary>
    /// Restore a soft-deleted entity
    /// </summary>
    public virtual void Restore()
    {
        IsDeleted = false;
        DeletedAt = null;
        DeletedBy = null;
    }
}
