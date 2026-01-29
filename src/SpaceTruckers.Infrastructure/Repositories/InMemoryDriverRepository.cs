using SpaceTruckers.Domain.Entities;
using SpaceTruckers.Domain.Ports;

namespace SpaceTruckers.Infrastructure.Repositories;

/// <summary>
/// Thread-safe in-memory repository for drivers.
/// </summary>
public sealed class InMemoryDriverRepository : IDriverRepository
{
    private readonly Dictionary<Guid, Driver> _drivers = new();
    private readonly object _lock = new();

    public Task<Driver?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _drivers.TryGetValue(id, out var driver);
            return Task.FromResult(driver);
        }
    }

    public Task<IReadOnlyList<Driver>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            return Task.FromResult<IReadOnlyList<Driver>>(_drivers.Values.ToList());
        }
    }

    public Task<IReadOnlyList<Driver>> GetAvailableDriversAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var available = _drivers.Values.Where(d => d.IsAvailable).ToList();
            return Task.FromResult<IReadOnlyList<Driver>>(available);
        }
    }

    public Task SaveAsync(Driver driver, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _drivers[driver.Id] = driver;
        }
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _drivers.Remove(id);
        }
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            return Task.FromResult(_drivers.ContainsKey(id));
        }
    }
}
