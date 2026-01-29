using Xunit;
using SpaceTruckers.Domain.Aggregates;
using SpaceTruckers.Domain.Entities;
using SpaceTruckers.Domain.Exceptions;
using SpaceTruckers.Domain.ValueObjects;
using SpaceTruckers.Infrastructure.Repositories;

namespace SpaceTruckers.Application.Tests;

public class ConcurrencyTests
{
    private readonly InMemoryEventStore _eventStore = new();
    private readonly InMemoryTripRepository _tripRepo;

    public ConcurrencyTests()
    {
        _tripRepo = new InMemoryTripRepository(_eventStore);
    }

    [Fact]
    public async Task SaveAsync_WithCorrectVersion_ShouldSucceed()
    {
        // Arrange
        var trip = Trip.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Cargo",
            DateTimeOffset.UtcNow);

        // Act - Save with expected version 0 (new trip)
        await _tripRepo.SaveAsync(trip, 0);
        trip.ClearUncommittedEvents();

        // Assert
        var saved = await _tripRepo.GetByIdAsync(trip.Id);
        Assert.NotNull(saved);
        Assert.Equal(1, saved.Version);
    }

    [Fact]
    public async Task SaveAsync_WithWrongVersion_ShouldThrowConcurrencyConflict()
    {
        // Arrange
        var tripId = Guid.NewGuid();
        var trip = Trip.Create(tripId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Cargo", DateTimeOffset.UtcNow);
        await _tripRepo.SaveAsync(trip, 0);
        trip.ClearUncommittedEvents();

        // Act - Try to save with wrong expected version
        trip.ReachCheckpoint(Guid.NewGuid(), "Station", 1, DateTimeOffset.UtcNow);

        // Assert
        var ex = await Assert.ThrowsAsync<ConcurrencyConflictException>(
            () => _tripRepo.SaveAsync(trip, 99)); // Wrong version
        
        Assert.Equal(tripId, ex.AggregateId);
        Assert.Equal(99, ex.ExpectedVersion);
        Assert.Equal(1, ex.ActualVersion);
    }

    [Fact]
    public async Task SaveAsync_NewTripWithNonZeroVersion_ShouldThrow()
    {
        // Arrange
        var trip = Trip.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Cargo", DateTimeOffset.UtcNow);

        // Act & Assert
        await Assert.ThrowsAsync<ConcurrencyConflictException>(
            () => _tripRepo.SaveAsync(trip, 1)); // Should be 0 for new trip
    }

    [Fact]
    public async Task ConcurrentModifications_ShouldDetectConflict()
    {
        // Arrange
        var trip = Trip.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Cargo", DateTimeOffset.UtcNow);
        await _tripRepo.SaveAsync(trip, 0);
        trip.ClearUncommittedEvents();

        // Simulate two parallel modifications by loading the trip twice
        var trip1 = await _tripRepo.GetByIdAsync(trip.Id);
        var trip2 = await _tripRepo.GetByIdAsync(trip.Id);

        // First modification succeeds
        trip1!.ReachCheckpoint(Guid.NewGuid(), "Station 1", 1, DateTimeOffset.UtcNow);
        await _tripRepo.SaveAsync(trip1, 1);

        // Second modification should fail due to version mismatch
        trip2!.ReachCheckpoint(Guid.NewGuid(), "Station 2", 2, DateTimeOffset.UtcNow);
        
        // Assert
        await Assert.ThrowsAsync<ConcurrencyConflictException>(
            () => _tripRepo.SaveAsync(trip2, 1)); // Still expects version 1, but it's now 2
    }

    [Fact]
    public async Task ThreadSafeRepository_ShouldHandleParallelReads()
    {
        // Arrange
        var trip = Trip.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Cargo", DateTimeOffset.UtcNow);
        await _tripRepo.SaveAsync(trip, 0);

        // Act - Parallel reads
        var tasks = Enumerable.Range(0, 100)
            .Select(_ => _tripRepo.GetByIdAsync(trip.Id))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        // Assert - All reads should succeed
        Assert.All(results, r => Assert.NotNull(r));
        Assert.All(results, r => Assert.Equal(trip.Id, r!.Id));
    }

    [Fact]
    public async Task ThreadSafeRepository_ShouldHandleParallelWrites()
    {
        // Arrange - Create multiple trips
        var tripIds = Enumerable.Range(0, 10).Select(_ => Guid.NewGuid()).ToArray();
        var trips = tripIds.Select(id =>
            Trip.Create(id, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), $"Cargo-{id}", DateTimeOffset.UtcNow))
            .ToArray();

        // Act - Parallel writes (different trips, should all succeed)
        var tasks = trips.Select(t => _tripRepo.SaveAsync(t, 0)).ToArray();
        await Task.WhenAll(tasks);

        // Assert
        foreach (var id in tripIds)
        {
            var saved = await _tripRepo.GetByIdAsync(id);
            Assert.NotNull(saved);
        }

        var active = await _tripRepo.GetActiveTripsAsync();
        Assert.Equal(10, active.Count);
    }
}

public class InMemoryRepositoryConcurrencyTests
{
    [Fact]
    public async Task DriverRepository_ShouldBeThreadSafe()
    {
        // Arrange
        var repo = new InMemoryDriverRepository();
        var drivers = Enumerable.Range(0, 50)
            .Select(i => new Driver(Guid.NewGuid(), $"Driver-{i}", $"LICENSE-{i}", i % 20))
            .ToArray();

        // Act - Parallel saves
        var saveTasks = drivers.Select(d => repo.SaveAsync(d)).ToArray();
        await Task.WhenAll(saveTasks);

        // Assert
        var all = await repo.GetAllAsync();
        Assert.Equal(50, all.Count);
    }

    [Fact]
    public async Task VehicleRepository_ShouldBeThreadSafe()
    {
        // Arrange
        var repo = new InMemoryVehicleRepository();
        var vehicles = Enumerable.Range(0, 50)
            .Select(i => new Vehicle(Guid.NewGuid(), $"Vehicle-{i}", VehicleType.CargoFreighter, 1000 + i, 500 + i))
            .ToArray();

        // Act - Parallel saves
        var saveTasks = vehicles.Select(v => repo.SaveAsync(v)).ToArray();
        await Task.WhenAll(saveTasks);

        // Assert
        var all = await repo.GetAllAsync();
        Assert.Equal(50, all.Count);
    }

    [Fact]
    public async Task RouteRepository_ShouldBeThreadSafe()
    {
        // Arrange
        var repo = new InMemoryRouteRepository();
        var routes = Enumerable.Range(0, 50)
            .Select(i => new Route(
                Guid.NewGuid(),
                $"Route-{i}",
                new SpaceLocation($"Origin-{i}", "Sol", i, 0, 0),
                new SpaceLocation($"Dest-{i}", "Sol", i + 100, 0, 0),
                (i % 10) + 1))
            .ToArray();

        // Act - Parallel saves
        var saveTasks = routes.Select(r => repo.SaveAsync(r)).ToArray();
        await Task.WhenAll(saveTasks);

        // Assert
        var all = await repo.GetAllAsync();
        Assert.Equal(50, all.Count);
    }
}
