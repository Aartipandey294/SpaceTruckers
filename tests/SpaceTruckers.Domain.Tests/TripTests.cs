using Xunit;
using SpaceTruckers.Domain.Aggregates;
using SpaceTruckers.Domain.Events;
using SpaceTruckers.Domain.Exceptions;
using SpaceTruckers.Domain.ValueObjects;

namespace SpaceTruckers.Domain.Tests;

public class TripTests
{
    private static readonly Guid ValidDriverId = Guid.NewGuid();
    private static readonly Guid ValidVehicleId = Guid.NewGuid();
    private static readonly Guid ValidRouteId = Guid.NewGuid();
    private static readonly DateTimeOffset TestTime = DateTimeOffset.UtcNow;

    #region Trip Creation Tests

    [Fact]
    public void Create_WithValidParameters_ShouldCreateTripInProgressState()
    {
        // Arrange
        var tripId = Guid.NewGuid();
        const string cargo = "Medical Supplies";

        // Act
        var trip = Trip.Create(tripId, ValidDriverId, ValidVehicleId, ValidRouteId, cargo, TestTime);

        // Assert
        Assert.Equal(tripId, trip.Id);
        Assert.Equal(ValidDriverId, trip.DriverId);
        Assert.Equal(ValidVehicleId, trip.VehicleId);
        Assert.Equal(ValidRouteId, trip.RouteId);
        Assert.Equal(cargo, trip.CargoDescription);
        Assert.Equal(TripStatus.InProgress, trip.Status);
        Assert.Equal(TestTime, trip.StartedAt);
        Assert.Null(trip.CompletedAt);
        Assert.Equal(1, trip.Version);
    }

    [Fact]
    public void Create_ShouldRaiseTripStartedEvent()
    {
        // Arrange & Act
        var trip = Trip.Create(Guid.NewGuid(), ValidDriverId, ValidVehicleId, ValidRouteId, "Cargo", TestTime);

        // Assert
        var startedEvent = Assert.Single(trip.UncommittedEvents);
        var tripStarted = Assert.IsType<TripStarted>(startedEvent);
        Assert.Equal(trip.Id, tripStarted.TripId);
        Assert.Equal(ValidDriverId, tripStarted.DriverId);
    }

    [Theory]
    [InlineData("00000000-0000-0000-0000-000000000000", "Trip ID cannot be empty")]
    public void Create_WithEmptyTripId_ShouldThrow(string id, string expectedMessage)
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            Trip.Create(Guid.Parse(id), ValidDriverId, ValidVehicleId, ValidRouteId, "Cargo", TestTime));
        Assert.Contains(expectedMessage, ex.Message);
    }

    [Fact]
    public void Create_WithEmptyDriverId_ShouldThrow()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            Trip.Create(Guid.NewGuid(), Guid.Empty, ValidVehicleId, ValidRouteId, "Cargo", TestTime));
        Assert.Contains("Driver ID cannot be empty", ex.Message);
    }

    [Fact]
    public void Create_WithEmptyCargoDescription_ShouldThrow()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            Trip.Create(Guid.NewGuid(), ValidDriverId, ValidVehicleId, ValidRouteId, "", TestTime));
        Assert.Contains("Cargo description cannot be empty", ex.Message);
    }

    #endregion

    #region Checkpoint Tests

    [Fact]
    public void ReachCheckpoint_WhenInProgress_ShouldRecordCheckpoint()
    {
        // Arrange
        var trip = CreateTestTrip();
        var checkpointId = Guid.NewGuid();

        // Act
        trip.ReachCheckpoint(checkpointId, "Lunar Waystation", 1, TestTime);

        // Assert
        Assert.Contains(checkpointId, trip.ReachedCheckpointIds);
        Assert.Equal(2, trip.Version);
    }

    [Fact]
    public void ReachCheckpoint_ShouldRaiseCheckpointReachedEvent()
    {
        // Arrange
        var trip = CreateTestTrip();
        trip.ClearUncommittedEvents();
        var checkpointId = Guid.NewGuid();

        // Act
        trip.ReachCheckpoint(checkpointId, "Lunar Waystation", 1, TestTime);

        // Assert
        var checkpointEvent = Assert.Single(trip.UncommittedEvents);
        var reachedEvent = Assert.IsType<CheckpointReached>(checkpointEvent);
        Assert.Equal(checkpointId, reachedEvent.CheckpointId);
        Assert.Equal("Lunar Waystation", reachedEvent.CheckpointName);
    }

    [Fact]
    public void ReachCheckpoint_Duplicate_ShouldBeIdempotent()
    {
        // Arrange
        var trip = CreateTestTrip();
        var checkpointId = Guid.NewGuid();
        trip.ReachCheckpoint(checkpointId, "Lunar Waystation", 1, TestTime);
        var versionAfterFirst = trip.Version;
        trip.ClearUncommittedEvents();

        // Act
        trip.ReachCheckpoint(checkpointId, "Lunar Waystation", 1, TestTime);

        // Assert
        Assert.Equal(versionAfterFirst, trip.Version);
        Assert.Empty(trip.UncommittedEvents);
        Assert.Single(trip.ReachedCheckpointIds);
    }

    [Fact]
    public void ReachCheckpoint_WhenCompleted_ShouldThrow()
    {
        // Arrange
        var trip = CreateTestTrip();
        trip.Complete(TestTime);

        // Act & Assert
        var ex = Assert.Throws<InvalidTripStateException>(() =>
            trip.ReachCheckpoint(Guid.NewGuid(), "Station", 1, TestTime));
        Assert.Equal("Completed", ex.CurrentState);
    }

    #endregion

    #region Incident Tests

    [Fact]
    public void RecordIncident_WhenInProgress_ShouldRecordIncident()
    {
        // Arrange
        var trip = CreateTestTrip();

        // Act
        var incidentId = trip.RecordIncident(
            IncidentType.AsteroidField,
            "Minor asteroid collision",
            IncidentSeverity.Minor,
            TestTime);

        // Assert
        Assert.NotEqual(Guid.Empty, incidentId);
        Assert.Single(trip.Incidents);
        Assert.Equal(IncidentType.AsteroidField, trip.Incidents[0].Type);
        Assert.Equal(IncidentSeverity.Minor, trip.Incidents[0].Severity);
    }

    [Fact]
    public void RecordIncident_ShouldRaiseIncidentOccurredEvent()
    {
        // Arrange
        var trip = CreateTestTrip();
        trip.ClearUncommittedEvents();

        // Act
        trip.RecordIncident(IncidentType.CosmicStorm, "Severe cosmic storm", IncidentSeverity.Major, TestTime);

        // Assert
        var incidentEvent = Assert.Single(trip.UncommittedEvents);
        var occurredEvent = Assert.IsType<IncidentOccurred>(incidentEvent);
        Assert.Equal(IncidentType.CosmicStorm, occurredEvent.IncidentType);
        Assert.Equal(IncidentSeverity.Major, occurredEvent.Severity);
    }

    [Fact]
    public void RecordIncident_WithEmptyDescription_ShouldThrow()
    {
        // Arrange
        var trip = CreateTestTrip();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            trip.RecordIncident(IncidentType.FuelLeak, "", IncidentSeverity.Minor, TestTime));
    }

    [Fact]
    public void ResolveIncident_WhenIncidentExists_ShouldMarkAsResolved()
    {
        // Arrange
        var trip = CreateTestTrip();
        var incidentId = trip.RecordIncident(
            IncidentType.EmergencyMaintenance,
            "Engine malfunction",
            IncidentSeverity.Moderate,
            TestTime);

        // Act
        trip.ResolveIncident(incidentId, "Replaced faulty component", TestTime);

        // Assert
        var incident = trip.Incidents.First(i => i.Id == incidentId);
        Assert.True(incident.IsResolved);
        Assert.Equal("Replaced faulty component", incident.ResolutionNotes);
    }

    [Fact]
    public void ResolveIncident_WhenIncidentNotFound_ShouldThrow()
    {
        // Arrange
        var trip = CreateTestTrip();

        // Act & Assert
        Assert.Throws<InvalidCheckpointOperationException>(() =>
            trip.ResolveIncident(Guid.NewGuid(), "Notes", TestTime));
    }

    [Fact]
    public void ResolveIncident_AlreadyResolved_ShouldBeIdempotent()
    {
        // Arrange
        var trip = CreateTestTrip();
        var incidentId = trip.RecordIncident(
            IncidentType.NavigationError,
            "GPS malfunction",
            IncidentSeverity.Minor,
            TestTime);
        trip.ResolveIncident(incidentId, "Recalibrated", TestTime);
        var versionAfterFirst = trip.Version;
        trip.ClearUncommittedEvents();

        // Act
        trip.ResolveIncident(incidentId, "Recalibrated again", TestTime);

        // Assert
        Assert.Equal(versionAfterFirst, trip.Version);
        Assert.Empty(trip.UncommittedEvents);
    }

    #endregion

    #region Completion Tests

    [Fact]
    public void Complete_WhenInProgress_ShouldSetStatusToCompleted()
    {
        // Arrange
        var trip = CreateTestTrip();
        var completedTime = TestTime.AddHours(5);

        // Act
        trip.Complete(completedTime);

        // Assert
        Assert.Equal(TripStatus.Completed, trip.Status);
        Assert.Equal(completedTime, trip.CompletedAt);
    }

    [Fact]
    public void Complete_ShouldRaiseTripCompletedEvent()
    {
        // Arrange
        var trip = CreateTestTrip();
        trip.ClearUncommittedEvents();

        // Act
        trip.Complete(TestTime);

        // Assert
        var completedEvent = Assert.Single(trip.UncommittedEvents);
        Assert.IsType<TripCompleted>(completedEvent);
    }

    [Fact]
    public void Complete_WithUnresolvedCriticalIncident_ShouldThrow()
    {
        // Arrange
        var trip = CreateTestTrip();
        trip.RecordIncident(
            IncidentType.PirateEncounter,
            "Pirate attack",
            IncidentSeverity.Critical,
            TestTime);

        // Act & Assert
        var ex = Assert.Throws<DomainInvariantViolationException>(() => trip.Complete(TestTime));
        Assert.Contains("unresolved critical incident", ex.Message);
    }

    [Fact]
    public void Complete_WithResolvedCriticalIncident_ShouldSucceed()
    {
        // Arrange
        var trip = CreateTestTrip();
        var incidentId = trip.RecordIncident(
            IncidentType.PirateEncounter,
            "Pirate attack",
            IncidentSeverity.Critical,
            TestTime);
        trip.ResolveIncident(incidentId, "Pirates defeated", TestTime);

        // Act
        trip.Complete(TestTime);

        // Assert
        Assert.Equal(TripStatus.Completed, trip.Status);
    }

    [Fact]
    public void Complete_WhenAlreadyCompleted_ShouldThrow()
    {
        // Arrange
        var trip = CreateTestTrip();
        trip.Complete(TestTime);

        // Act & Assert
        Assert.Throws<InvalidTripStateException>(() => trip.Complete(TestTime));
    }

    #endregion

    #region Cancellation Tests

    [Fact]
    public void Cancel_WhenInProgress_ShouldSetStatusToCancelled()
    {
        // Arrange
        var trip = CreateTestTrip();

        // Act
        trip.Cancel("Mission abort due to emergency", TestTime);

        // Assert
        Assert.Equal(TripStatus.Cancelled, trip.Status);
    }

    [Fact]
    public void Cancel_ShouldRaiseTripCancelledEvent()
    {
        // Arrange
        var trip = CreateTestTrip();
        trip.ClearUncommittedEvents();

        // Act
        trip.Cancel("Emergency abort", TestTime);

        // Assert
        var cancelledEvent = Assert.Single(trip.UncommittedEvents);
        var evt = Assert.IsType<TripCancelled>(cancelledEvent);
        Assert.Equal("Emergency abort", evt.Reason);
    }

    [Fact]
    public void Cancel_WhenAlreadyCancelled_ShouldBeIdempotent()
    {
        // Arrange
        var trip = CreateTestTrip();
        trip.Cancel("First cancel", TestTime);
        var versionAfterFirst = trip.Version;
        trip.ClearUncommittedEvents();

        // Act
        trip.Cancel("Second cancel", TestTime);

        // Assert
        Assert.Equal(versionAfterFirst, trip.Version);
        Assert.Empty(trip.UncommittedEvents);
    }

    [Fact]
    public void Cancel_WhenCompleted_ShouldThrow()
    {
        // Arrange
        var trip = CreateTestTrip();
        trip.Complete(TestTime);

        // Act & Assert
        Assert.Throws<InvalidTripStateException>(() => trip.Cancel("Too late", TestTime));
    }

    [Fact]
    public void Cancel_WithEmptyReason_ShouldThrow()
    {
        // Arrange
        var trip = CreateTestTrip();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => trip.Cancel("", TestTime));
    }

    #endregion

    #region Summary Tests

    [Fact]
    public void GenerateSummary_ShouldReturnCorrectSummary()
    {
        // Arrange
        var trip = CreateTestTrip();
        trip.ReachCheckpoint(Guid.NewGuid(), "Checkpoint 1", 1, TestTime);
        trip.ReachCheckpoint(Guid.NewGuid(), "Checkpoint 2", 2, TestTime);
        var incidentId = trip.RecordIncident(IncidentType.AsteroidField, "Minor damage", IncidentSeverity.Minor, TestTime);
        trip.ResolveIncident(incidentId, "Repaired", TestTime);
        trip.RecordIncident(IncidentType.CosmicStorm, "Storm", IncidentSeverity.Moderate, TestTime);

        // Act
        var summary = trip.GenerateSummary();

        // Assert
        Assert.Equal(trip.Id, summary.TripId);
        Assert.Equal(TripStatus.InProgress, summary.Status);
        Assert.Equal(2, summary.CheckpointsReached);
        Assert.Equal(2, summary.TotalIncidents);
        Assert.Equal(1, summary.ResolvedIncidents);
        Assert.False(summary.HasCriticalIncidents);
    }

    [Fact]
    public void GenerateSummary_WithCriticalIncident_ShouldFlagCritical()
    {
        // Arrange
        var trip = CreateTestTrip();
        trip.RecordIncident(IncidentType.PirateEncounter, "Attack", IncidentSeverity.Critical, TestTime);

        // Act
        var summary = trip.GenerateSummary();

        // Assert
        Assert.True(summary.HasCriticalIncidents);
    }

    #endregion

    #region Event Rehydration Tests

    [Fact]
    public void FromEvents_ShouldRehydrateTripState()
    {
        // Arrange
        var tripId = Guid.NewGuid();
        var checkpointId = Guid.NewGuid();
        var incidentId = Guid.NewGuid();
        var events = new List<DomainEvent>
        {
            new TripStarted(tripId, ValidDriverId, ValidVehicleId, ValidRouteId, "Cargo", TestTime),
            new CheckpointReached(tripId, checkpointId, "Moon", 1, TestTime),
            new IncidentOccurred(tripId, incidentId, IncidentType.CosmicStorm, "Storm", IncidentSeverity.Moderate, TestTime),
            new IncidentResolved(tripId, incidentId, "Passed through", TestTime),
            new TripCompleted(tripId, TestTime.AddHours(2))
        };

        // Act
        var trip = Trip.FromEvents(events);

        // Assert
        Assert.Equal(tripId, trip.Id);
        Assert.Equal(TripStatus.Completed, trip.Status);
        Assert.Single(trip.ReachedCheckpointIds);
        Assert.Single(trip.Incidents);
        Assert.True(trip.Incidents[0].IsResolved);
    }

    #endregion

    #region Helper Methods

    private static Trip CreateTestTrip()
    {
        return Trip.Create(
            Guid.NewGuid(),
            ValidDriverId,
            ValidVehicleId,
            ValidRouteId,
            "Test Cargo",
            TestTime);
    }

    #endregion
}
