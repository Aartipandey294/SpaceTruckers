using SpaceTruckers.Domain.Entities;
using SpaceTruckers.Domain.Ports;

namespace SpaceTruckers.Infrastructure.Repositories;

/// <summary>
/// Thread-safe in-memory repository for vehicles.
/// </summary>
public sealed class InMemoryVehicleRepository : IVehicleRepository
{
    private readonly Dictionary<Guid, Vehicle> _vehicles = new();
    private readonly object _lock = new();

    public Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _vehicles.TryGetValue(id, out var vehicle);
            return Task.FromResult(vehicle);
        }
    }

    public Task<IReadOnlyList<Vehicle>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            return Task.FromResult<IReadOnlyList<Vehicle>>(_vehicles.Values.ToList());
        }
    }

    public Task<IReadOnlyList<Vehicle>> GetAvailableVehiclesAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var available = _vehicles.Values.Where(v => v.IsAvailable).ToList();
            return Task.FromResult<IReadOnlyList<Vehicle>>(available);
        }
    }

    public Task SaveAsync(Vehicle vehicle, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _vehicles[vehicle.Id] = vehicle;
        }
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _vehicles.Remove(id);
        }
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            return Task.FromResult(_vehicles.ContainsKey(id));
        }
    }
}
