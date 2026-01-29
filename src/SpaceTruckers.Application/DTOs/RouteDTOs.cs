namespace SpaceTruckers.Application.DTOs;

public sealed record CreateRouteRequest(
    string Name,
    SpaceLocationDto Origin,
    SpaceLocationDto Destination,
    int DangerRating,
    IReadOnlyList<CheckpointDto>? Checkpoints);

public sealed record SpaceLocationDto(
    string Name,
    string Sector,
    double X,
    double Y,
    double Z);

public sealed record CheckpointDto(
    string Name,
    SpaceLocationDto Location,
    int SequenceNumber,
    int EstimatedDurationMinutes);

public sealed record AddCheckpointRequest(CheckpointDto Checkpoint);

public sealed record RouteResponse(
    Guid Id,
    string Name,
    SpaceLocationDto Origin,
    SpaceLocationDto Destination,
    int DangerRating,
    double TotalDistance,
    string EstimatedDuration,
    IReadOnlyList<CheckpointResponse> Checkpoints);

public sealed record CheckpointResponse(
    Guid Id,
    string Name,
    SpaceLocationDto Location,
    int SequenceNumber,
    int EstimatedDurationMinutes);
