using System;
using System.Threading;
using System.Threading.Tasks;

namespace ERMSystem.Application.Interfaces;

/// <summary>
/// Unit of Work contract coordinating database transactions and domain events.
/// Enforces consistency boundaries across aggregates.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
