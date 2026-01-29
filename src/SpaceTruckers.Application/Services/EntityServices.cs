using SpaceTruckers.Application.DTOs;
using SpaceTruckers.Domain.Entities;
using SpaceTruckers.Domain.Exceptions;
using SpaceTruckers.Domain.Ports;
using SpaceTruckers.Domain.ValueObjects;

namespace SpaceTruckers.Application.Services;

/// <summary>
/// Application service for Driver operations.
/// </summary>
public sealed class DriverService
{
    private readonly IDriverRepository _driverRepository;
    private readonly IIdGenerator _idGenerator;

    public DriverService(IDriverRepository driverRepository, IIdGenerator idGenerator)
    {
        _driverRepository = driverRepository;
        _idGenerator = idGenerator;
    }

    public async Task<OperationResult<DriverResponse>> CreateDriverAsync(
        CreateDriverRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var driver = new Driver(
                _idGenerator.NewId(),
                request.Name,
                request.LicenseNumber,
                request.ExperienceYears);

            await _driverRepository.SaveAsync(driver, cancellationToken);
            return OperationResult<DriverResponse>.Ok(MapToResponse(driver));
        }
        catch (ArgumentException ex)
        {
            return OperationResult<DriverResponse>.Fail(ex.Message, "VALIDATION_ERROR");
        }
    }

    public async Task<OperationResult<DriverResponse>> GetDriverAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var driver = await _driverRepository.GetByIdAsync(id, cancellationToken);
        if (driver is null)
            return OperationResult<DriverResponse>.Fail($"Driver {id} not found", "DRIVER_NOT_FOUND");

        return OperationResult<DriverResponse>.Ok(MapToResponse(driver));
    }

    public async Task<OperationResult<IReadOnlyList<DriverResponse>>> GetAllDriversAsync(
        CancellationToken cancellationToken = default)
    {
        var drivers = await _driverRepository.GetAllAsync(cancellationToken);
        return OperationResult<IReadOnlyList<DriverResponse>>.Ok(
            drivers.Select(MapToResponse).ToList());
    }

    public async Task<OperationResult<IReadOnlyList<DriverResponse>>> GetAvailableDriversAsync(
        CancellationToken cancellationToken = default)
    {
        var drivers = await _driverRepository.GetAvailableDriversAsync(cancellationToken);
        return OperationResult<IReadOnlyList<DriverResponse>>.Ok(
            drivers.Select(MapToResponse).ToList());
    }

    public async Task<OperationResult<DriverResponse>> UpdateDriverAsync(
        Guid id,
        UpdateDriverRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var driver = await _driverRepository.GetByIdAsync(id, cancellationToken);
            if (driver is null)
                return OperationResult<DriverResponse>.Fail($"Driver {id} not found", "DRIVER_NOT_FOUND");

            driver.UpdateDetails(request.Name, request.ExperienceYears);
            await _driverRepository.SaveAsync(driver, cancellationToken);
            return OperationResult<DriverResponse>.Ok(MapToResponse(driver));
        }
        catch (ArgumentException ex)
        {
            return OperationResult<DriverResponse>.Fail(ex.Message, "VALIDATION_ERROR");
        }
    }

    public async Task<OperationResult> DeleteDriverAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var driver = await _driverRepository.GetByIdAsync(id, cancellationToken);
        if (driver is null)
            return OperationResult.Fail($"Driver {id} not found", "DRIVER_NOT_FOUND");

        if (!driver.IsAvailable)
            return OperationResult.Fail("Cannot delete driver currently on a trip", "DRIVER_ON_TRIP");

        await _driverRepository.DeleteAsync(id, cancellationToken);
        return OperationResult.Ok();
    }

    private static DriverResponse MapToResponse(Driver driver)
    {
        return new DriverResponse(
            driver.Id,
            driver.Name,
            driver.LicenseNumber,
            driver.ExperienceYears,
            driver.IsAvailable);
    }
}

/// <summary>
/// Application service for Vehicle operations.
/// </summary>
public sealed class VehicleService
{
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IIdGenerator _idGenerator;

    public VehicleService(IVehicleRepository vehicleRepository, IIdGenerator idGenerator)
    {
        _vehicleRepository = vehicleRepository;
        _idGenerator = idGenerator;
    }

    public async Task<OperationResult<VehicleResponse>> CreateVehicleAsync(
        CreateVehicleRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var vehicle = new Vehicle(
                _idGenerator.NewId(),
                request.Name,
                request.Type,
                request.MaxCargoCapacityKg,
                request.MaxSpeed);

            await _vehicleRepository.SaveAsync(vehicle, cancellationToken);
            return OperationResult<VehicleResponse>.Ok(MapToResponse(vehicle));
        }
        catch (ArgumentException ex)
        {
            return OperationResult<VehicleResponse>.Fail(ex.Message, "VALIDATION_ERROR");
        }
    }

    public async Task<OperationResult<VehicleResponse>> GetVehicleAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var vehicle = await _vehicleRepository.GetByIdAsync(id, cancellationToken);
        if (vehicle is null)
            return OperationResult<VehicleResponse>.Fail($"Vehicle {id} not found", "VEHICLE_NOT_FOUND");

        return OperationResult<VehicleResponse>.Ok(MapToResponse(vehicle));
    }

    public async Task<OperationResult<IReadOnlyList<VehicleResponse>>> GetAllVehiclesAsync(
        CancellationToken cancellationToken = default)
    {
        var vehicles = await _vehicleRepository.GetAllAsync(cancellationToken);
        return OperationResult<IReadOnlyList<VehicleResponse>>.Ok(
            vehicles.Select(MapToResponse).ToList());
    }

    public async Task<OperationResult<IReadOnlyList<VehicleResponse>>> GetAvailableVehiclesAsync(
        CancellationToken cancellationToken = default)
    {
        var vehicles = await _vehicleRepository.GetAvailableVehiclesAsync(cancellationToken);
        return OperationResult<IReadOnlyList<VehicleResponse>>.Ok(
            vehicles.Select(MapToResponse).ToList());
    }

    public async Task<OperationResult<VehicleResponse>> UpdateVehicleAsync(
        Guid id,
        UpdateVehicleRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var vehicle = await _vehicleRepository.GetByIdAsync(id, cancellationToken);
            if (vehicle is null)
                return OperationResult<VehicleResponse>.Fail($"Vehicle {id} not found", "VEHICLE_NOT_FOUND");

            vehicle.UpdateDetails(request.Name, request.MaxCargoCapacityKg, request.MaxSpeed);
            await _vehicleRepository.SaveAsync(vehicle, cancellationToken);
            return OperationResult<VehicleResponse>.Ok(MapToResponse(vehicle));
        }
        catch (ArgumentException ex)
        {
            return OperationResult<VehicleResponse>.Fail(ex.Message, "VALIDATION_ERROR");
        }
    }

    public async Task<OperationResult> DeleteVehicleAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var vehicle = await _vehicleRepository.GetByIdAsync(id, cancellationToken);
        if (vehicle is null)
            return OperationResult.Fail($"Vehicle {id} not found", "VEHICLE_NOT_FOUND");

        if (!vehicle.IsAvailable)
            return OperationResult.Fail("Cannot delete vehicle currently on a trip", "VEHICLE_ON_TRIP");

        await _vehicleRepository.DeleteAsync(id, cancellationToken);
        return OperationResult.Ok();
    }

    private static VehicleResponse MapToResponse(Vehicle vehicle)
    {
        return new VehicleResponse(
            vehicle.Id,
            vehicle.Name,
            vehicle.Type.ToString(),
            vehicle.MaxCargoCapacityKg,
            vehicle.MaxSpeed,
            vehicle.IsAvailable);
    }
}

/// <summary>
/// Application service for Route operations.
/// </summary>
public sealed class RouteService
{
    private readonly IRouteRepository _routeRepository;
    private readonly IIdGenerator _idGenerator;

    public RouteService(IRouteRepository routeRepository, IIdGenerator idGenerator)
    {
        _routeRepository = routeRepository;
        _idGenerator = idGenerator;
    }

    public async Task<OperationResult<RouteResponse>> CreateRouteAsync(
        CreateRouteRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var origin = MapToLocation(request.Origin);
            var destination = MapToLocation(request.Destination);

            var route = new Route(
                _idGenerator.NewId(),
                request.Name,
                origin,
                destination,
                request.DangerRating);

            if (request.Checkpoints is not null)
            {
                foreach (var cp in request.Checkpoints)
                {
                    var checkpoint = new Checkpoint(
                        _idGenerator.NewId(),
                        cp.Name,
                        MapToLocation(cp.Location),
                        cp.SequenceNumber,
                        TimeSpan.FromMinutes(cp.EstimatedDurationMinutes));
                    route.AddCheckpoint(checkpoint);
                }
            }

            await _routeRepository.SaveAsync(route, cancellationToken);
            return OperationResult<RouteResponse>.Ok(MapToResponse(route));
        }
        catch (ArgumentException ex)
        {
            return OperationResult<RouteResponse>.Fail(ex.Message, "VALIDATION_ERROR");
        }
    }

    public async Task<OperationResult<RouteResponse>> GetRouteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var route = await _routeRepository.GetByIdAsync(id, cancellationToken);
        if (route is null)
            return OperationResult<RouteResponse>.Fail($"Route {id} not found", "ROUTE_NOT_FOUND");

        return OperationResult<RouteResponse>.Ok(MapToResponse(route));
    }

    public async Task<OperationResult<IReadOnlyList<RouteResponse>>> GetAllRoutesAsync(
        CancellationToken cancellationToken = default)
    {
        var routes = await _routeRepository.GetAllAsync(cancellationToken);
        return OperationResult<IReadOnlyList<RouteResponse>>.Ok(
            routes.Select(MapToResponse).ToList());
    }

    public async Task<OperationResult<RouteResponse>> AddCheckpointAsync(
        Guid routeId,
        AddCheckpointRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var route = await _routeRepository.GetByIdAsync(routeId, cancellationToken);
            if (route is null)
                return OperationResult<RouteResponse>.Fail($"Route {routeId} not found", "ROUTE_NOT_FOUND");

            var checkpoint = new Checkpoint(
                _idGenerator.NewId(),
                request.Checkpoint.Name,
                MapToLocation(request.Checkpoint.Location),
                request.Checkpoint.SequenceNumber,
                TimeSpan.FromMinutes(request.Checkpoint.EstimatedDurationMinutes));

            route.AddCheckpoint(checkpoint);
            await _routeRepository.SaveAsync(route, cancellationToken);
            return OperationResult<RouteResponse>.Ok(MapToResponse(route));
        }
        catch (InvalidOperationException ex)
        {
            return OperationResult<RouteResponse>.Fail(ex.Message, "VALIDATION_ERROR");
        }
    }

    public async Task<OperationResult> DeleteRouteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (!await _routeRepository.ExistsAsync(id, cancellationToken))
            return OperationResult.Fail($"Route {id} not found", "ROUTE_NOT_FOUND");

        await _routeRepository.DeleteAsync(id, cancellationToken);
        return OperationResult.Ok();
    }

    private static SpaceLocation MapToLocation(SpaceLocationDto dto)
    {
        return new SpaceLocation(dto.Name, dto.Sector, dto.X, dto.Y, dto.Z);
    }

    private static SpaceLocationDto MapToLocationDto(SpaceLocation location)
    {
        return new SpaceLocationDto(location.Name, location.Sector, location.X, location.Y, location.Z);
    }

    private static RouteResponse MapToResponse(Route route)
    {
        return new RouteResponse(
            route.Id,
            route.Name,
            MapToLocationDto(route.Origin),
            MapToLocationDto(route.Destination),
            route.DangerRating,
            route.TotalDistance(),
            FormatDuration(route.EstimatedTotalDuration()),
            route.Checkpoints.Select(MapToCheckpointResponse).ToList());
    }

    private static CheckpointResponse MapToCheckpointResponse(Checkpoint checkpoint)
    {
        return new CheckpointResponse(
            checkpoint.Id,
            checkpoint.Name,
            MapToLocationDto(checkpoint.Location),
            checkpoint.SequenceNumber,
            (int)checkpoint.EstimatedDuration.TotalMinutes);
    }

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalHours >= 1)
            return $"{(int)duration.TotalHours}h {duration.Minutes}m";
        return $"{duration.Minutes}m";
    }
}
