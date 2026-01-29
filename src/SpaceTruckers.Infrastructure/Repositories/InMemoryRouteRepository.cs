using SpaceTruckers.Domain.Entities;
using SpaceTruckers.Domain.Ports;

namespace SpaceTruckers.Infrastructure.Repositories;

/// <summary>
/// Thread-safe in-memory repository for routes.
/// </summary>
public sealed class InMemoryRouteRepository : IRouteRepository
{
    private readonly Dictionary<Guid, Route> _routes = new();
    private readonly object _lock = new();

    public Task<Route?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _routes.TryGetValue(id, out var route);
            return Task.FromResult(route);
        }
    }

    public Task<IReadOnlyList<Route>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            return Task.FromResult<IReadOnlyList<Route>>(_routes.Values.ToList());
        }
    }

    public Task SaveAsync(Route route, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _routes[route.Id] = route;
        }
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _routes.Remove(id);
        }
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            return Task.FromResult(_routes.ContainsKey(id));
        }
    }
}
