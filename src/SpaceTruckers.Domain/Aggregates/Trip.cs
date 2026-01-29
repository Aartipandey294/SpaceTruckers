using SpaceTruckers.Domain.Entities;
using SpaceTruckers.Domain.Events;
using SpaceTruckers.Domain.Exceptions;
using SpaceTruckers.Domain.ValueObjects;

namespace SpaceTruckers.Domain.Aggregates;

/// <summary>
/// Aggregate root for a delivery trip.
/// Encapsulates all trip-related behavior and enforces invariants.
/// Uses event sourcing pattern with Apply/When for state transitions.
/// </summary>
public sealed class Trip
{
    public Guid Id { get; private set; }
    public Guid DriverId { get; private set; }
    public Guid VehicleId { get; private set; }
    public Guid RouteId { get; private set; }
    public string CargoDescription { get; private set; } = string.Empty;
    public TripStatus Status { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public int Version { get; private set; }

    private readonly List<Guid> _reachedCheckpointIds = new();
    public IReadOnlyList<Guid> ReachedCheckpointIds => _reachedCheckpointIds.AsReadOnly();

    private readonly List<TripIncident> _incidents = new();
    public IReadOnlyList<TripIncident> Incidents => _incidents.AsReadOnly();

    private readonly List<DomainEvent> _uncommittedEvents = new();
    public IReadOnlyList<DomainEvent> UncommittedEvents => _uncommittedEvents.AsReadOnly();

    private readonly HashSet<Guid> _processedEventIds = new();

    private Trip() { } // For persistence/rehydration

    /// <summary>
    /// Creates a new trip from a start event.
    /// </summary>
    public static Trip Create(
        Guid id,
        Guid driverId,
        Guid vehicleId,
        Guid routeId,
        string cargoDescription,
        DateTimeOffset occurredAt)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Trip ID cannot be empty", nameof(id));
        if (driverId == Guid.Empty)
            throw new ArgumentException("Driver ID cannot be empty", nameof(driverId));
        if (vehicleId == Guid.Empty)
            throw new ArgumentException("Vehicle ID cannot be empty", nameof(vehicleId));
        if (routeId == Guid.Empty)
            throw new ArgumentException("Route ID cannot be empty", nameof(routeId));
        if (string.IsNullOrWhiteSpace(cargoDescription))
            throw new ArgumentException("Cargo description cannot be empty", nameof(cargoDescription));

        var trip = new Trip();
        var @event = new TripStarted(id, driverId, vehicleId, routeId, cargoDescription, occurredAt);
        trip.Apply(@event);
        return trip;
    }

    /// <summary>
    /// Rehydrates a trip from a stream of events.
    /// </summary>
    public static Trip FromEvents(IEnumerable<DomainEvent> events)
    {
        var trip = new Trip();
        foreach (var @event in events)
        {
            trip.When(@event);
            trip._processedEventIds.Add(@event.EventId);
            trip.Version++;
        }
        return trip;
    }

    /// <summary>
    /// Records reaching a checkpoint on the route.
    /// </summary>
    public void ReachCheckpoint(
        Guid checkpointId,
        string checkpointName,
        int sequenceNumber,
        DateTimeOffset occurredAt)
    {
        EnsureInProgress("reach checkpoint");

        if (_reachedCheckpointIds.Contains(checkpointId))
        {
            // Idempotency: ignore duplicate checkpoint events
            return;
        }

        var @event = new CheckpointReached(Id, checkpointId, checkpointName, sequenceNumber, occurredAt);
        Apply(@event);
    }

    /// <summary>
    /// Records an incident that occurred during the trip.
    /// </summary>
    public Guid RecordIncident(
        IncidentType type,
        string description,
        IncidentSeverity severity,
        DateTimeOffset occurredAt)
    {
        EnsureInProgress("record incident");

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Incident description cannot be empty", nameof(description));

        var incidentId = Guid.NewGuid();
        var @event = new IncidentOccurred(Id, incidentId, type, description, severity, occurredAt);
        Apply(@event);
        return incidentId;
    }

    /// <summary>
    /// Resolves a previously recorded incident.
    /// </summary>
    public void ResolveIncident(Guid incidentId, string resolutionNotes, DateTimeOffset occurredAt)
    {
        EnsureInProgress("resolve incident");

        var incident = _incidents.FirstOrDefault(i => i.Id == incidentId);
        if (incident is null)
            throw new InvalidCheckpointOperationException($"Incident {incidentId} not found on this trip");

        if (incident.IsResolved)
            return; // Idempotency: already resolved

        var @event = new IncidentResolved(Id, incidentId, resolutionNotes, occurredAt);
        Apply(@event);
    }

    /// <summary>
    /// Completes the trip successfully.
    /// </summary>
    public void Complete(DateTimeOffset occurredAt)
    {
        EnsureInProgress("complete");

        // Check for unresolved critical incidents
        var unresolvedCritical = _incidents
            .Where(i => !i.IsResolved && i.Severity == IncidentSeverity.Critical)
            .ToList();

        if (unresolvedCritical.Any())
        {
            throw new DomainInvariantViolationException(
                $"Cannot complete trip with {unresolvedCritical.Count} unresolved critical incident(s)");
        }

        var @event = new TripCompleted(Id, occurredAt);
        Apply(@event);
    }

    /// <summary>
    /// Cancels the trip.
    /// </summary>
    public void Cancel(string reason, DateTimeOffset occurredAt)
    {
        if (Status == TripStatus.Completed)
            throw new InvalidTripStateException(Status.ToString(), "cancel");

        if (Status == TripStatus.Cancelled)
            return; // Idempotency: already cancelled

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Cancellation reason cannot be empty", nameof(reason));

        var @event = new TripCancelled(Id, reason, occurredAt);
        Apply(@event);
    }

    /// <summary>
    /// Generates a summary of the trip.
    /// </summary>
    public TripSummary GenerateSummary()
    {
        var duration = Status switch
        {
            TripStatus.Completed when StartedAt.HasValue && CompletedAt.HasValue
                => CompletedAt.Value - StartedAt.Value,
            TripStatus.InProgress when StartedAt.HasValue
                => DateTimeOffset.UtcNow - StartedAt.Value,
            _ => TimeSpan.Zero
        };

        return new TripSummary(
            Id,
            DriverId,
            VehicleId,
            RouteId,
            CargoDescription,
            Status,
            StartedAt,
            CompletedAt,
            duration,
            _reachedCheckpointIds.Count,
            _incidents.Count,
            _incidents.Count(i => i.IsResolved),
            _incidents.Any(i => i.Severity == IncidentSeverity.Critical),
            _incidents.Select(i => i.ToSummary()).ToList());
    }

    /// <summary>
    /// Clears uncommitted events after persistence.
    /// </summary>
    public void ClearUncommittedEvents()
    {
        foreach (var @event in _uncommittedEvents)
        {
            _processedEventIds.Add(@event.EventId);
        }
        _uncommittedEvents.Clear();
    }

    #region Event Application

    private void Apply(DomainEvent @event)
    {
        // Check for duplicate events (idempotency)
        if (_processedEventIds.Contains(@event.EventId))
            return;

        When(@event);
        _uncommittedEvents.Add(@event);
        Version++;
    }

    private void When(DomainEvent @event)
    {
        switch (@event)
        {
            case TripStarted e:
                WhenTripStarted(e);
                break;
            case CheckpointReached e:
                WhenCheckpointReached(e);
                break;
            case IncidentOccurred e:
                WhenIncidentOccurred(e);
                break;
            case IncidentResolved e:
                WhenIncidentResolved(e);
                break;
            case TripCompleted e:
                WhenTripCompleted(e);
                break;
            case TripCancelled e:
                WhenTripCancelled(e);
                break;
            default:
                throw new InvalidOperationException($"Unknown event type: {@event.GetType().Name}");
        }
    }

    private void WhenTripStarted(TripStarted e)
    {
        Id = e.TripId;
        DriverId = e.DriverId;
        VehicleId = e.VehicleId;
        RouteId = e.RouteId;
        CargoDescription = e.CargoDescription;
        Status = TripStatus.InProgress;
        StartedAt = e.OccurredAt;
    }

    private void WhenCheckpointReached(CheckpointReached e)
    {
        if (!_reachedCheckpointIds.Contains(e.CheckpointId))
        {
            _reachedCheckpointIds.Add(e.CheckpointId);
        }
    }

    private void WhenIncidentOccurred(IncidentOccurred e)
    {
        _incidents.Add(new TripIncident(
            e.IncidentId,
            e.IncidentType,
            e.Description,
            e.Severity,
            e.OccurredAt));
    }

    private void WhenIncidentResolved(IncidentResolved e)
    {
        var incident = _incidents.FirstOrDefault(i => i.Id == e.IncidentId);
        incident?.Resolve(e.ResolutionNotes, e.OccurredAt);
    }

    private void WhenTripCompleted(TripCompleted e)
    {
        Status = TripStatus.Completed;
        CompletedAt = e.OccurredAt;
    }

    private void WhenTripCancelled(TripCancelled e)
    {
        Status = TripStatus.Cancelled;
        CompletedAt = e.OccurredAt;
    }

    #endregion

    private void EnsureInProgress(string operation)
    {
        if (Status != TripStatus.InProgress)
        {
            throw new InvalidTripStateException(Status.ToString(), operation);
        }
    }
}

/// <summary>
/// Represents an incident that occurred during a trip.
/// </summary>
public sealed class TripIncident
{
    public Guid Id { get; }
    public IncidentType Type { get; }
    public string Description { get; }
    public IncidentSeverity Severity { get; }
    public DateTimeOffset OccurredAt { get; }
    public bool IsResolved { get; private set; }
    public string? ResolutionNotes { get; private set; }
    public DateTimeOffset? ResolvedAt { get; private set; }

    public TripIncident(
        Guid id,
        IncidentType type,
        string description,
        IncidentSeverity severity,
        DateTimeOffset occurredAt)
    {
        Id = id;
        Type = type;
        Description = description;
        Severity = severity;
        OccurredAt = occurredAt;
    }

    internal void Resolve(string notes, DateTimeOffset resolvedAt)
    {
        IsResolved = true;
        ResolutionNotes = notes;
        ResolvedAt = resolvedAt;
    }

    public IncidentSummary ToSummary() => new(
        Id, Type, Description, Severity, OccurredAt, IsResolved, ResolutionNotes, ResolvedAt);
}

/// <summary>
/// Summary of a trip's current state.
/// </summary>
public sealed record TripSummary(
    Guid TripId,
    Guid DriverId,
    Guid VehicleId,
    Guid RouteId,
    string CargoDescription,
    TripStatus Status,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    TimeSpan Duration,
    int CheckpointsReached,
    int TotalIncidents,
    int ResolvedIncidents,
    bool HasCriticalIncidents,
    IReadOnlyList<IncidentSummary> Incidents);

/// <summary>
/// Summary of an incident.
/// </summary>
public sealed record IncidentSummary(
    Guid Id,
    IncidentType Type,
    string Description,
    IncidentSeverity Severity,
    DateTimeOffset OccurredAt,
    bool IsResolved,
    string? ResolutionNotes,
    DateTimeOffset? ResolvedAt);
