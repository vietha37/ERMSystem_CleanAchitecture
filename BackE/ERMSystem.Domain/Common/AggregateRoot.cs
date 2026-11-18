using System;
using System.Collections.Generic;

namespace ERMSystem.Domain.Common;

/// <summary>
/// Base class for Aggregate Roots in Domain-Driven Design.
/// Manages domain events that occurred within transactional boundaries.
/// </summary>
public abstract class AggregateRoot<TKey> : BaseEntity<TKey> where TKey : IEquatable<TKey>
{
    private readonly List<IDomainEvent> _domainEvents = new();

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}

/// <summary>
/// Default AggregateRoot using Guid as identifier.
/// </summary>
public abstract class AggregateRoot : AggregateRoot<Guid>
{
    protected AggregateRoot() : base() { }

    protected AggregateRoot(Guid id)
    {
        Id = id;
    }
}
