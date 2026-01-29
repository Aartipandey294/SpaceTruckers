using SpaceTruckers.Domain.ValueObjects;

namespace SpaceTruckers.Application.DTOs;

public sealed record CreateTripRequest(
    Guid DriverId,
    Guid VehicleId,
    Guid RouteId,
    string CargoDescription);

public sealed record TripResponse(
    Guid Id,
    Guid DriverId,
    Guid VehicleId,
    Guid RouteId,
    string CargoDescription,
    string Status,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    int Version,
    int CheckpointsReached,
    IReadOnlyList<IncidentResponse> Incidents);

public sealed record ReachCheckpointRequest(
    Guid CheckpointId,
    int ExpectedVersion);

public sealed record RecordIncidentRequest(
    IncidentType Type,
    string Description,
    IncidentSeverity Severity,
    int ExpectedVersion);

public sealed record ResolveIncidentRequest(
    Guid IncidentId,
    string ResolutionNotes,
    int ExpectedVersion);

public sealed record CompleteTripRequest(int ExpectedVersion);

public sealed record CancelTripRequest(
    string Reason,
    int ExpectedVersion);

public sealed record IncidentResponse(
    Guid Id,
    string Type,
    string Description,
    string Severity,
    DateTimeOffset OccurredAt,
    bool IsResolved,
    string? ResolutionNotes,
    DateTimeOffset? ResolvedAt);

public sealed record TripSummaryResponse(
    Guid TripId,
    Guid DriverId,
    Guid VehicleId,
    Guid RouteId,
    string CargoDescription,
    string Status,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    string Duration,
    int CheckpointsReached,
    int TotalIncidents,
    int ResolvedIncidents,
    bool HasCriticalIncidents,
    IReadOnlyList<IncidentResponse> Incidents);
