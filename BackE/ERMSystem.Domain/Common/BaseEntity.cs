using System;
using System.Collections.Generic;

namespace ERMSystem.Domain.Common;

/// <summary>
/// Abstract base entity with strongly-typed identifier, audit trail, and structural equality.
/// Complies with Domain-Driven Design and Clean Architecture standards.
/// </summary>
public abstract class BaseEntity<TKey> where TKey : IEquatable<TKey>
{
    public TKey Id { get; protected set; } = default!;

    public DateTime CreatedAtUtc { get; protected set; } = DateTime.UtcNow;
    public string? CreatedBy { get; protected set; }
    public DateTime? UpdatedAtUtc { get; protected set; }
    public string? UpdatedBy { get; protected set; }
    public DateTime? DeletedAtUtc { get; protected set; }
    public bool IsDeleted => DeletedAtUtc.HasValue;

    public virtual void SetAuditCreated(string createdBy)
    {
        CreatedAtUtc = DateTime.UtcNow;
        CreatedBy = createdBy;
    }

    public virtual void SetAuditUpdated(string updatedBy)
    {
        UpdatedAtUtc = DateTime.UtcNow;
        UpdatedBy = updatedBy;
    }

    public virtual void SoftDelete(string deletedBy)
    {
        DeletedAtUtc = DateTime.UtcNow;
        UpdatedBy = deletedBy;
    }

    public virtual void Restore(string restoredBy)
    {
        DeletedAtUtc = null;
        UpdatedBy = restoredBy;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not BaseEntity<TKey> other)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        if (GetType() != other.GetType())
            return false;

        if (EqualityComparer<TKey>.Default.Equals(Id, default!) ||
            EqualityComparer<TKey>.Default.Equals(other.Id, default!))
            return false;

        return EqualityComparer<TKey>.Default.Equals(Id, other.Id);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(GetType(), Id);
    }

    public static bool operator ==(BaseEntity<TKey>? left, BaseEntity<TKey>? right)
    {
        if (left is null && right is null) return true;
        if (left is null || right is null) return false;
        return left.Equals(right);
    }

    public static bool operator !=(BaseEntity<TKey>? left, BaseEntity<TKey>? right)
    {
        return !(left == right);
    }
}

/// <summary>
/// Default base entity using Guid as default identifier.
/// </summary>
public abstract class BaseEntity : BaseEntity<Guid>
{
    protected BaseEntity()
    {
        Id = Guid.NewGuid();
    }

    protected BaseEntity(Guid id)
    {
        Id = id;
    }
}
