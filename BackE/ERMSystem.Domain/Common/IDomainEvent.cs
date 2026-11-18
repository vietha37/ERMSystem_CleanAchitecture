using System;

namespace ERMSystem.Domain.Common;

/// <summary>
/// Marker interface for all domain events within the ERM system.
/// </summary>
public interface IDomainEvent
{
    Guid EventId => Guid.NewGuid();
    DateTime OccurredOnUtc => DateTime.UtcNow;
}
