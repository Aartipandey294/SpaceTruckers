using SpaceTruckers.Domain.Aggregates;
using SpaceTruckers.Domain.Entities;
using SpaceTruckers.Domain.Events;

namespace SpaceTruckers.Domain.Ports;

/// <summary>
/// Repository interface for Trip aggregate.
/// </summary>
public interface ITripRepository
{
    Task<Trip?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Trip?> GetByIdWithVersionAsync(Guid id, int expectedVersion, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Trip>> GetActiveTripsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Trip>> GetTripsByDriverAsync(Guid driverId, CancellationToken cancellationToken = default);
    Task SaveAsync(Trip trip, int expectedVersion, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository interface for Driver entity.
/// </summary>
public interface IDriverRepository
{
    Task<Driver?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Driver>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Driver>> GetAvailableDriversAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(Driver driver, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository interface for Vehicle entity.
/// </summary>
public interface IVehicleRepository
{
    Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Vehicle>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Vehicle>> GetAvailableVehiclesAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(Vehicle vehicle, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository interface for Route entity.
/// </summary>
public interface IRouteRepository
{
    Task<Route?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Route>> GetAllAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(Route route, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}

/// <summary>
/// Event store interface for persisting domain events.
/// </summary>
public interface IEventStore
{
    Task AppendEventsAsync(Guid aggregateId, IEnumerable<DomainEvent> events, int expectedVersion, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DomainEvent>> GetEventsAsync(Guid aggregateId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DomainEvent>> GetEventsAfterVersionAsync(Guid aggregateId, int afterVersion, CancellationToken cancellationToken = default);
}

/// <summary>
/// Clock interface for time abstraction (testability).
/// </summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}

/// <summary>
/// ID generator interface for deterministic testing.
/// </summary>
public interface IIdGenerator
{
    Guid NewId();
}
