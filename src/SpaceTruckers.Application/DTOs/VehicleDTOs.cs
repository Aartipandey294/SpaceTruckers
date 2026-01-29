using SpaceTruckers.Domain.ValueObjects;

namespace SpaceTruckers.Application.DTOs;

public sealed record CreateVehicleRequest(
    string Name,
    VehicleType Type,
    double MaxCargoCapacityKg,
    double MaxSpeed);

public sealed record UpdateVehicleRequest(
    string Name,
    double MaxCargoCapacityKg,
    double MaxSpeed);

public sealed record VehicleResponse(
    Guid Id,
    string Name,
    string Type,
    double MaxCargoCapacityKg,
    double MaxSpeed,
    bool IsAvailable);
