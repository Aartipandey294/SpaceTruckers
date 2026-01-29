using SpaceTruckers.Domain.Aggregates;
using SpaceTruckers.Domain.Events;
using SpaceTruckers.Domain.Exceptions;
using SpaceTruckers.Domain.Ports;
using SpaceTruckers.Domain.ValueObjects;

namespace SpaceTruckers.Infrastructure.Repositories;

/// <summary>
/// Thread-safe in-memory repository for trips with optimistic concurrency control.
/// Uses internal event store for event sourcing pattern.
/// </summary>
public sealed class InMemoryTripRepository : ITripRepository
{
    private readonly IEventStore _eventStore;
    private readonly Dictionary<Guid, int> _versionCache = new();
    private readonly object _lock = new();

    public InMemoryTripRepository(IEventStore eventStore)
    {
        _eventStore = eventStore;
    }

    public async Task<Trip?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var events = await _eventStore.GetEventsAsync(id, cancellationToken);
        if (!events.Any())
            return null;

        return Trip.FromEvents(events);
    }

    public async Task<Trip?> GetByIdWithVersionAsync(Guid id, int expectedVersion, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (_versionCache.TryGetValue(id, out var currentVersion) && currentVersion != expectedVersion)
                throw new ConcurrencyConflictException(id, expectedVersion, currentVersion);
        }

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<IReadOnlyList<Trip>> GetActiveTripsAsync(CancellationToken cancellationToken = default)
    {
        var allTrips = await GetAllTripsAsync(cancellationToken);
        return allTrips.Where(t => t.Status == TripStatus.InProgress).ToList();
    }

    public async Task<IReadOnlyList<Trip>> GetTripsByDriverAsync(Guid driverId, CancellationToken cancellationToken = default)
    {
        var allTrips = await GetAllTripsAsync(cancellationToken);
        return allTrips.Where(t => t.DriverId == driverId).ToList();
    }

    public async Task SaveAsync(Trip trip, int expectedVersion, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (_versionCache.TryGetValue(trip.Id, out var currentVersion))
            {
                if (currentVersion != expectedVersion)
                    throw new ConcurrencyConflictException(trip.Id, expectedVersion, currentVersion);
            }
            else if (expectedVersion != 0)
            {
                throw new ConcurrencyConflictException(trip.Id, expectedVersion, 0);
            }
        }

        await _eventStore.AppendEventsAsync(trip.Id, trip.UncommittedEvents, expectedVersion, cancellationToken);

        lock (_lock)
        {
            _versionCache[trip.Id] = trip.Version;
        }
    }

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            return Task.FromResult(_versionCache.ContainsKey(id));
        }
    }

    private async Task<IReadOnlyList<Trip>> GetAllTripsAsync(CancellationToken cancellationToken)
    {
        List<Guid> tripIds;
        lock (_lock)
        {
            tripIds = _versionCache.Keys.ToList();
        }

        var trips = new List<Trip>();
        foreach (var id in tripIds)
        {
            var trip = await GetByIdAsync(id, cancellationToken);
            if (trip != null)
                trips.Add(trip);
        }

        return trips;
    }
}
