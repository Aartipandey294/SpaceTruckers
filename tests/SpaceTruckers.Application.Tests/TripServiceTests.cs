using Xunit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SpaceTruckers.Application.DTOs;
using SpaceTruckers.Application.Services;
using SpaceTruckers.Domain.Entities;
using SpaceTruckers.Domain.Ports;
using SpaceTruckers.Domain.ValueObjects;
using SpaceTruckers.Infrastructure.Repositories;

namespace SpaceTruckers.Application.Tests;

/// <summary>
/// Fake clock for deterministic testing.
/// </summary>
public class FakeClock : IClock
{
    public DateTimeOffset UtcNow { get; set; } = DateTimeOffset.UtcNow;
    public void Advance(TimeSpan duration) => UtcNow = UtcNow.Add(duration);
}

/// <summary>
/// Deterministic ID generator for testing.
/// </summary>
public class FakeIdGenerator : IIdGenerator
{
    private readonly Queue<Guid> _ids = new();

    public void SetNextId(Guid id) => _ids.Enqueue(id);
    public Guid NewId() => _ids.Count > 0 ? _ids.Dequeue() : Guid.NewGuid();
}

public class TripServiceTests
{
    private readonly InMemoryEventStore _eventStore = new();
    private readonly InMemoryTripRepository _tripRepo;
    private readonly InMemoryDriverRepository _driverRepo = new();
    private readonly InMemoryVehicleRepository _vehicleRepo = new();
    private readonly InMemoryRouteRepository _routeRepo = new();
    private readonly FakeClock _clock = new();
    private readonly FakeIdGenerator _idGenerator = new();
    private readonly ILogger<TripService> _logger = NullLogger<TripService>.Instance;
    private readonly TripService _service;

    public TripServiceTests()
    {
        _tripRepo = new InMemoryTripRepository(_eventStore);
        _service = new TripService(
            _tripRepo, _driverRepo, _vehicleRepo, _routeRepo, _clock, _idGenerator, _logger);
    }

    #region CreateTrip Tests

    [Fact]
    public async Task CreateTrip_WithValidData_ShouldSucceed()
    {
        // Arrange
        var driver = await CreateAndSaveDriver();
        var vehicle = await CreateAndSaveVehicle();
        var route = await CreateAndSaveRoute();
        var expectedTripId = Guid.NewGuid();
        _idGenerator.SetNextId(expectedTripId);

        var request = new CreateTripRequest(driver.Id, vehicle.Id, route.Id, "Medical Supplies");

        // Act
        var result = await _service.CreateTripAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(expectedTripId, result.Data!.Id);
        Assert.Equal("InProgress", result.Data.Status);
        Assert.Equal(1, result.Data.Version);
    }

    [Fact]
    public async Task CreateTrip_WithNonExistentDriver_ShouldFail()
    {
        // Arrange
        var vehicle = await CreateAndSaveVehicle();
        var route = await CreateAndSaveRoute();
        var request = new CreateTripRequest(Guid.NewGuid(), vehicle.Id, route.Id, "Cargo");

        // Act
        var result = await _service.CreateTripAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("DRIVER_NOT_FOUND", result.ErrorCode);
    }

    [Fact]
    public async Task CreateTrip_WithUnavailableDriver_ShouldFail()
    {
        // Arrange
        var driver = await CreateAndSaveDriver();
        driver.MarkAsUnavailable();
        await _driverRepo.SaveAsync(driver);
        
        var vehicle = await CreateAndSaveVehicle();
        var route = await CreateAndSaveRoute();
        var request = new CreateTripRequest(driver.Id, vehicle.Id, route.Id, "Cargo");

        // Act
        var result = await _service.CreateTripAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("DRIVER_UNAVAILABLE", result.ErrorCode);
    }

    [Fact]
    public async Task CreateTrip_ShouldMarkDriverAndVehicleUnavailable()
    {
        // Arrange
        var driver = await CreateAndSaveDriver();
        var vehicle = await CreateAndSaveVehicle();
        var route = await CreateAndSaveRoute();
        var request = new CreateTripRequest(driver.Id, vehicle.Id, route.Id, "Cargo");

        // Act
        await _service.CreateTripAsync(request);

        // Assert
        var updatedDriver = await _driverRepo.GetByIdAsync(driver.Id);
        var updatedVehicle = await _vehicleRepo.GetByIdAsync(vehicle.Id);
        Assert.False(updatedDriver!.IsAvailable);
        Assert.False(updatedVehicle!.IsAvailable);
    }

    #endregion

    #region Checkpoint Tests

    [Fact]
    public async Task ReachCheckpoint_WithValidCheckpoint_ShouldSucceed()
    {
        // Arrange
        var (trip, route) = await CreateTripWithRoute();
        var checkpoint = route.Checkpoints.First();
        var request = new ReachCheckpointRequest(checkpoint.Id, 1);

        // Act
        var result = await _service.ReachCheckpointAsync(trip.Data!.Id, request);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(2, result.Data!.Version);
        Assert.Equal(1, result.Data.CheckpointsReached);
    }

    [Fact]
    public async Task ReachCheckpoint_WithInvalidCheckpoint_ShouldFail()
    {
        // Arrange
        var (trip, _) = await CreateTripWithRoute();
        var request = new ReachCheckpointRequest(Guid.NewGuid(), 1);

        // Act
        var result = await _service.ReachCheckpointAsync(trip.Data!.Id, request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("CHECKPOINT_NOT_FOUND", result.ErrorCode);
    }

    [Fact]
    public async Task ReachCheckpoint_WithWrongVersion_ShouldReturnConcurrencyConflict()
    {
        // Arrange
        var (trip, route) = await CreateTripWithRoute();
        var checkpoint = route.Checkpoints.First();
        var request = new ReachCheckpointRequest(checkpoint.Id, 99); // Wrong version

        // Act
        var result = await _service.ReachCheckpointAsync(trip.Data!.Id, request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("CONCURRENCY_CONFLICT", result.ErrorCode);
    }

    #endregion

    #region Incident Tests

    [Fact]
    public async Task RecordIncident_ShouldAddIncidentToTrip()
    {
        // Arrange
        var (trip, _) = await CreateTripWithRoute();
        var request = new RecordIncidentRequest(
            IncidentType.AsteroidField,
            "Minor collision",
            IncidentSeverity.Minor,
            1);

        // Act
        var result = await _service.RecordIncidentAsync(trip.Data!.Id, request);

        // Assert
        Assert.True(result.Success);
        Assert.Single(result.Data!.Incidents);
        Assert.Equal("AsteroidField", result.Data.Incidents[0].Type);
    }

    [Fact]
    public async Task ResolveIncident_ShouldMarkAsResolved()
    {
        // Arrange
        var (trip, _) = await CreateTripWithRoute();
        var incidentRequest = new RecordIncidentRequest(
            IncidentType.FuelLeak, "Leak detected", IncidentSeverity.Moderate, 1);
        var incidentResult = await _service.RecordIncidentAsync(trip.Data!.Id, incidentRequest);
        var incidentId = incidentResult.Data!.Incidents[0].Id;

        var resolveRequest = new ResolveIncidentRequest(incidentId, "Patched the leak", 2);

        // Act
        var result = await _service.ResolveIncidentAsync(trip.Data.Id, resolveRequest);

        // Assert
        Assert.True(result.Success);
        Assert.True(result.Data!.Incidents[0].IsResolved);
    }

    #endregion

    #region Completion Tests

    [Fact]
    public async Task CompleteTrip_ShouldReturnSummaryAndReleaseResources()
    {
        // Arrange
        var (trip, _) = await CreateTripWithRoute();
        var request = new CompleteTripRequest(1);

        // Act
        _clock.Advance(TimeSpan.FromHours(5));
        var result = await _service.CompleteTripAsync(trip.Data!.Id, request);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Completed", result.Data!.Status);
        Assert.NotNull(result.Data.CompletedAt);
        Assert.Contains("h", result.Data.Duration); // Duration formatted

        // Resources released
        var tripData = await _tripRepo.GetByIdAsync(trip.Data.Id);
        var driver = await _driverRepo.GetByIdAsync(tripData!.DriverId);
        var vehicle = await _vehicleRepo.GetByIdAsync(tripData.VehicleId);
        Assert.True(driver!.IsAvailable);
        Assert.True(vehicle!.IsAvailable);
    }

    [Fact]
    public async Task CompleteTrip_WithUnresolvedCritical_ShouldFail()
    {
        // Arrange
        var (trip, _) = await CreateTripWithRoute();
        await _service.RecordIncidentAsync(trip.Data!.Id,
            new RecordIncidentRequest(IncidentType.PirateEncounter, "Attack!", IncidentSeverity.Critical, 1));

        var request = new CompleteTripRequest(2);

        // Act
        var result = await _service.CompleteTripAsync(trip.Data.Id, request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("DOMAIN_ERROR", result.ErrorCode);
    }

    #endregion

    #region Cancellation Tests

    [Fact]
    public async Task CancelTrip_ShouldCancelAndReleaseResources()
    {
        // Arrange
        var (trip, _) = await CreateTripWithRoute();
        var request = new CancelTripRequest("Mission abort", 1);

        // Act
        var result = await _service.CancelTripAsync(trip.Data!.Id, request);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Cancelled", result.Data!.Status);

        // Resources released
        var tripData = await _tripRepo.GetByIdAsync(trip.Data.Id);
        var driver = await _driverRepo.GetByIdAsync(tripData!.DriverId);
        Assert.True(driver!.IsAvailable);
    }

    #endregion

    #region Concurrency Tests

    [Fact]
    public async Task ConcurrentUpdates_ShouldDetectConflict()
    {
        // Arrange
        var (trip, route) = await CreateTripWithRoute();
        var checkpoint = route.Checkpoints.First();

        // Simulate two concurrent requests with same expected version
        var request1 = new ReachCheckpointRequest(checkpoint.Id, 1);
        var request2 = new RecordIncidentRequest(IncidentType.CosmicStorm, "Storm!", IncidentSeverity.Moderate, 1);

        // Act - First request succeeds
        var result1 = await _service.ReachCheckpointAsync(trip.Data!.Id, request1);
        
        // Second request with stale version should fail
        var result2 = await _service.RecordIncidentAsync(trip.Data.Id, request2);

        // Assert
        Assert.True(result1.Success);
        Assert.False(result2.Success);
        Assert.Equal("CONCURRENCY_CONFLICT", result2.ErrorCode);
    }

    [Fact]
    public async Task ParallelTripCreation_ShouldHandleCorrectly()
    {
        // Arrange
        var driver1 = await CreateAndSaveDriver("Driver 1");
        var driver2 = await CreateAndSaveDriver("Driver 2");
        var vehicle1 = await CreateAndSaveVehicle("Vehicle 1");
        var vehicle2 = await CreateAndSaveVehicle("Vehicle 2");
        var route = await CreateAndSaveRoute();

        var request1 = new CreateTripRequest(driver1.Id, vehicle1.Id, route.Id, "Cargo 1");
        var request2 = new CreateTripRequest(driver2.Id, vehicle2.Id, route.Id, "Cargo 2");

        // Act - Create trips in parallel
        var tasks = new[]
        {
            _service.CreateTripAsync(request1),
            _service.CreateTripAsync(request2)
        };
        var results = await Task.WhenAll(tasks);

        // Assert - Both should succeed (different resources)
        Assert.All(results, r => Assert.True(r.Success));
        
        var activeTrips = await _tripRepo.GetActiveTripsAsync();
        Assert.Equal(2, activeTrips.Count);
    }

    #endregion

    #region Helper Methods

    private async Task<Driver> CreateAndSaveDriver(string name = "Test Driver")
    {
        var driver = new Driver(Guid.NewGuid(), name, $"LICENSE-{Guid.NewGuid():N}", 5);
        await _driverRepo.SaveAsync(driver);
        return driver;
    }

    private async Task<Vehicle> CreateAndSaveVehicle(string name = "Test Vehicle")
    {
        var vehicle = new Vehicle(Guid.NewGuid(), name, VehicleType.CargoFreighter, 10000, 1500);
        await _vehicleRepo.SaveAsync(vehicle);
        return vehicle;
    }

    private async Task<Route> CreateAndSaveRoute()
    {
        var origin = new SpaceLocation("Earth", "Sol", 0, 0, 0);
        var destination = new SpaceLocation("Mars", "Sol", 100, 0, 0);
        var route = new Route(Guid.NewGuid(), "Earth-Mars", origin, destination, 5);
        
        var checkpoint = new Checkpoint(
            Guid.NewGuid(),
            "Moon Station",
            new SpaceLocation("Moon", "Sol", 10, 0, 0),
            1,
            TimeSpan.FromMinutes(30));
        route.AddCheckpoint(checkpoint);
        
        await _routeRepo.SaveAsync(route);
        return route;
    }

    private async Task<(OperationResult<TripResponse> trip, Route route)> CreateTripWithRoute()
    {
        var driver = await CreateAndSaveDriver();
        var vehicle = await CreateAndSaveVehicle();
        var route = await CreateAndSaveRoute();
        
        var request = new CreateTripRequest(driver.Id, vehicle.Id, route.Id, "Test Cargo");
        var result = await _service.CreateTripAsync(request);
        
        return (result, route);
    }

    #endregion
}
