using SpaceTruckers.Domain.Events;
using SpaceTruckers.Domain.Exceptions;
using SpaceTruckers.Domain.Ports;

namespace SpaceTruckers.Infrastructure.Repositories;

/// <summary>
/// Thread-safe in-memory event store for domain events.
/// </summary>
public sealed class InMemoryEventStore : IEventStore
{
    private readonly Dictionary<Guid, List<DomainEvent>> _eventStreams = new();
    private readonly object _lock = new();

    public Task AppendEventsAsync(
        Guid aggregateId,
        IEnumerable<DomainEvent> events,
        int expectedVersion,
        CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (!_eventStreams.TryGetValue(aggregateId, out var stream))
            {
                if (expectedVersion != 0)
                    throw new ConcurrencyConflictException(aggregateId, expectedVersion, 0);

                stream = new List<DomainEvent>();
                _eventStreams[aggregateId] = stream;
            }
            else if (stream.Count != expectedVersion)
            {
                throw new ConcurrencyConflictException(aggregateId, expectedVersion, stream.Count);
            }

            stream.AddRange(events);
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<DomainEvent>> GetEventsAsync(
        Guid aggregateId,
        CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (!_eventStreams.TryGetValue(aggregateId, out var stream))
                return Task.FromResult<IReadOnlyList<DomainEvent>>(Array.Empty<DomainEvent>());

            return Task.FromResult<IReadOnlyList<DomainEvent>>(stream.ToList());
        }
    }

    public Task<IReadOnlyList<DomainEvent>> GetEventsAfterVersionAsync(
        Guid aggregateId,
        int afterVersion,
        CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (!_eventStreams.TryGetValue(aggregateId, out var stream))
                return Task.FromResult<IReadOnlyList<DomainEvent>>(Array.Empty<DomainEvent>());

            var events = stream.Skip(afterVersion).ToList();
            return Task.FromResult<IReadOnlyList<DomainEvent>>(events);
        }
    }
}
