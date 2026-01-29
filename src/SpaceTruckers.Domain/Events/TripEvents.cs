using SpaceTruckers.Domain.ValueObjects;

namespace SpaceTruckers.Domain.Events;

/// <summary>
/// Event raised when a delivery trip is started.
/// </summary>
public sealed record TripStarted(
    Guid TripId,
    Guid DriverId,
    Guid VehicleId,
    Guid RouteId,
    string CargoDescription,
    DateTimeOffset OccurredAt) : DomainEvent(OccurredAt);

/// <summary>
/// Event raised when a trip reaches a checkpoint on its route.
/// </summary>
public sealed record CheckpointReached(
    Guid TripId,
    Guid CheckpointId,
    string CheckpointName,
    int SequenceNumber,
    DateTimeOffset OccurredAt) : DomainEvent(OccurredAt);

/// <summary>
/// Event raised when an incident occurs during a trip.
/// </summary>
public sealed record IncidentOccurred(
    Guid TripId,
    Guid IncidentId,
    IncidentType IncidentType,
    string Description,
    IncidentSeverity Severity,
    DateTimeOffset OccurredAt) : DomainEvent(OccurredAt);

/// <summary>
/// Event raised when an incident is resolved.
/// </summary>
public sealed record IncidentResolved(
    Guid TripId,
    Guid IncidentId,
    string ResolutionNotes,
    DateTimeOffset OccurredAt) : DomainEvent(OccurredAt);

/// <summary>
/// Event raised when a trip is completed successfully.
/// </summary>
public sealed record TripCompleted(
    Guid TripId,
    DateTimeOffset OccurredAt) : DomainEvent(OccurredAt);

/// <summary>
/// Event raised when a trip is cancelled.
/// </summary>
public sealed record TripCancelled(
    Guid TripId,
    string Reason,
    DateTimeOffset OccurredAt) : DomainEvent(OccurredAt);
