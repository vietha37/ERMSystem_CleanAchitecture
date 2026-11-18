using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ERMSystem.Domain.Common;

namespace ERMSystem.Application.Interfaces;

/// <summary>
/// Dispatches domain events to their respective in-process or outbox handlers.
/// </summary>
public interface IDomainEventDispatcher
{
    Task DispatchEventsAsync(IEnumerable<IDomainEvent> events, CancellationToken cancellationToken = default);
}
