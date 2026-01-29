using Microsoft.Extensions.Logging;
using SpaceTruckers.Application.DTOs;
using SpaceTruckers.Domain.Aggregates;
using SpaceTruckers.Domain.Entities;
using SpaceTruckers.Domain.Exceptions;
using SpaceTruckers.Domain.Ports;
using SpaceTruckers.Domain.ValueObjects;

namespace SpaceTruckers.Application.Services;

/// <summary>
/// Application service for Trip operations.
/// Orchestrates domain operations and handles cross-cutting concerns.
/// </summary>
public sealed class TripService
{
    private readonly ITripRepository _tripRepository;
    private readonly IDriverRepository _driverRepository;
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IRouteRepository _routeRepository;
    private readonly IClock _clock;
    private readonly IIdGenerator _idGenerator;
    private readonly ILogger<TripService> _logger;

    public TripService(
        ITripRepository tripRepository,
        IDriverRepository driverRepository,
        IVehicleRepository vehicleRepository,
        IRouteRepository routeRepository,
        IClock clock,
        IIdGenerator idGenerator,
        ILogger<TripService> logger)
    {
        _tripRepository = tripRepository;
        _driverRepository = driverRepository;
        _vehicleRepository = vehicleRepository;
        _routeRepository = routeRepository;
        _clock = clock;
        _idGenerator = idGenerator;
        _logger = logger;
    }

    public async Task<OperationResult<TripResponse>> CreateTripAsync(
        CreateTripRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Creating trip for Driver {DriverId}, Vehicle {VehicleId}, Route {RouteId}",
            request.DriverId, request.VehicleId, request.RouteId);

        try
        {
            // Validate references exist
            var driver = await _driverRepository.GetByIdAsync(request.DriverId, cancellationToken);
            if (driver is null)
            {
                _logger.LogWarning("Trip creation failed: Driver {DriverId} not found", request.DriverId);
                return OperationResult<TripResponse>.Fail($"Driver {request.DriverId} not found", "DRIVER_NOT_FOUND");
            }

            if (!driver.IsAvailable)
            {
                _logger.LogWarning("Trip creation failed: Driver {DriverId} ({DriverName}) is unavailable", driver.Id, driver.Name);
                return OperationResult<TripResponse>.Fail($"Driver {driver.Name} is not available", "DRIVER_UNAVAILABLE");
            }

            var vehicle = await _vehicleRepository.GetByIdAsync(request.VehicleId, cancellationToken);
            if (vehicle is null)
            {
                _logger.LogWarning("Trip creation failed: Vehicle {VehicleId} not found", request.VehicleId);
                return OperationResult<TripResponse>.Fail($"Vehicle {request.VehicleId} not found", "VEHICLE_NOT_FOUND");
            }

            if (!vehicle.IsAvailable)
            {
                _logger.LogWarning("Trip creation failed: Vehicle {VehicleId} ({VehicleName}) is unavailable", vehicle.Id, vehicle.Name);
                return OperationResult<TripResponse>.Fail($"Vehicle {vehicle.Name} is not available", "VEHICLE_UNAVAILABLE");
            }

            var route = await _routeRepository.GetByIdAsync(request.RouteId, cancellationToken);
            if (route is null)
            {
                _logger.LogWarning("Trip creation failed: Route {RouteId} not found", request.RouteId);
                return OperationResult<TripResponse>.Fail($"Route {request.RouteId} not found", "ROUTE_NOT_FOUND");
            }

            // Create trip
            var tripId = _idGenerator.NewId();
            var trip = Trip.Create(
                tripId,
                request.DriverId,
                request.VehicleId,
                request.RouteId,
                request.CargoDescription,
                _clock.UtcNow);

            // Mark driver and vehicle as unavailable
            driver.MarkAsUnavailable();
            vehicle.MarkAsUnavailable();

            // Persist changes
            await _tripRepository.SaveAsync(trip, 0, cancellationToken);
            await _driverRepository.SaveAsync(driver, cancellationToken);
            await _vehicleRepository.SaveAsync(vehicle, cancellationToken);

            trip.ClearUncommittedEvents();

            _logger.LogInformation(
                "Trip {TripId} created successfully. Driver: {DriverName}, Vehicle: {VehicleName}, Route: {RouteName}, Cargo: {CargoDescription}",
                tripId, driver.Name, vehicle.Name, route.Name, request.CargoDescription);

            return OperationResult<TripResponse>.Ok(MapToResponse(trip));
        }
        catch (DomainException ex)
        {
            _logger.LogError(ex, "Domain error creating trip for Driver {DriverId}", request.DriverId);
            return OperationResult<TripResponse>.Fail(ex.Message, "DOMAIN_ERROR");
        }
    }

    public async Task<OperationResult<TripResponse>> GetTripAsync(
        Guid tripId,
        CancellationToken cancellationToken = default)
    {
        var trip = await _tripRepository.GetByIdAsync(tripId, cancellationToken);
        if (trip is null)
        {
            _logger.LogWarning("Trip {TripId} not found", tripId);
            return OperationResult<TripResponse>.Fail($"Trip {tripId} not found", "TRIP_NOT_FOUND");
        }

        return OperationResult<TripResponse>.Ok(MapToResponse(trip));
    }

    public async Task<OperationResult<IReadOnlyList<TripResponse>>> GetActiveTripsAsync(
        CancellationToken cancellationToken = default)
    {
        var trips = await _tripRepository.GetActiveTripsAsync(cancellationToken);
        _logger.LogDebug("Retrieved {Count} active trips", trips.Count);
        return OperationResult<IReadOnlyList<TripResponse>>.Ok(
            trips.Select(MapToResponse).ToList());
    }

    public async Task<OperationResult<TripResponse>> ReachCheckpointAsync(
        Guid tripId,
        ReachCheckpointRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Recording checkpoint {CheckpointId} reached for Trip {TripId}",
            request.CheckpointId, tripId);

        try
        {
            var trip = await _tripRepository.GetByIdAsync(tripId, cancellationToken);
            if (trip is null)
            {
                _logger.LogWarning("Checkpoint recording failed: Trip {TripId} not found", tripId);
                return OperationResult<TripResponse>.Fail($"Trip {tripId} not found", "TRIP_NOT_FOUND");
            }

            // Get route to validate checkpoint
            var route = await _routeRepository.GetByIdAsync(trip.RouteId, cancellationToken);
            var checkpoint = route?.Checkpoints.FirstOrDefault(c => c.Id == request.CheckpointId);
            if (checkpoint is null)
            {
                _logger.LogWarning(
                    "Checkpoint recording failed: Checkpoint {CheckpointId} not found on route {RouteId}",
                    request.CheckpointId, trip.RouteId);
                return OperationResult<TripResponse>.Fail($"Checkpoint {request.CheckpointId} not found on route", "CHECKPOINT_NOT_FOUND");
            }

            trip.ReachCheckpoint(
                checkpoint.Id,
                checkpoint.Name,
                checkpoint.SequenceNumber,
                _clock.UtcNow);

            await _tripRepository.SaveAsync(trip, request.ExpectedVersion, cancellationToken);
            trip.ClearUncommittedEvents();

            _logger.LogInformation(
                "Checkpoint '{CheckpointName}' (seq: {SequenceNumber}) reached for Trip {TripId}. Total checkpoints: {TotalCheckpoints}",
                checkpoint.Name, checkpoint.SequenceNumber, tripId, trip.ReachedCheckpointIds.Count);

            return OperationResult<TripResponse>.Ok(MapToResponse(trip));
        }
        catch (ConcurrencyConflictException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict reaching checkpoint for Trip {TripId}", tripId);
            return OperationResult<TripResponse>.Fail(ex.Message, "CONCURRENCY_CONFLICT");
        }
        catch (DomainException ex)
        {
            _logger.LogError(ex, "Domain error reaching checkpoint for Trip {TripId}", tripId);
            return OperationResult<TripResponse>.Fail(ex.Message, "DOMAIN_ERROR");
        }
    }

    public async Task<OperationResult<TripResponse>> RecordIncidentAsync(
        Guid tripId,
        RecordIncidentRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Recording {IncidentType} incident (severity: {Severity}) for Trip {TripId}",
            request.Type, request.Severity, tripId);

        try
        {
            var trip = await _tripRepository.GetByIdAsync(tripId, cancellationToken);
            if (trip is null)
            {
                _logger.LogWarning("Incident recording failed: Trip {TripId} not found", tripId);
                return OperationResult<TripResponse>.Fail($"Trip {tripId} not found", "TRIP_NOT_FOUND");
            }

            trip.RecordIncident(
                request.Type,
                request.Description,
                request.Severity,
                _clock.UtcNow);

            await _tripRepository.SaveAsync(trip, request.ExpectedVersion, cancellationToken);
            trip.ClearUncommittedEvents();

            var latestIncident = trip.Incidents.LastOrDefault();
            _logger.LogWarning(
                "Incident {IncidentId} recorded for Trip {TripId}. Type: {Type}, Severity: {Severity}, Description: {Description}",
                latestIncident?.Id, tripId, request.Type, request.Severity, request.Description);

            return OperationResult<TripResponse>.Ok(MapToResponse(trip));
        }
        catch (ConcurrencyConflictException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict recording incident for Trip {TripId}", tripId);
            return OperationResult<TripResponse>.Fail(ex.Message, "CONCURRENCY_CONFLICT");
        }
        catch (DomainException ex)
        {
            _logger.LogError(ex, "Domain error recording incident for Trip {TripId}", tripId);
            return OperationResult<TripResponse>.Fail(ex.Message, "DOMAIN_ERROR");
        }
    }

    public async Task<OperationResult<TripResponse>> ResolveIncidentAsync(
        Guid tripId,
        ResolveIncidentRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Resolving incident {IncidentId} for Trip {TripId}",
            request.IncidentId, tripId);

        try
        {
            var trip = await _tripRepository.GetByIdAsync(tripId, cancellationToken);
            if (trip is null)
            {
                _logger.LogWarning("Incident resolution failed: Trip {TripId} not found", tripId);
                return OperationResult<TripResponse>.Fail($"Trip {tripId} not found", "TRIP_NOT_FOUND");
            }

            trip.ResolveIncident(request.IncidentId, request.ResolutionNotes, _clock.UtcNow);

            await _tripRepository.SaveAsync(trip, request.ExpectedVersion, cancellationToken);
            trip.ClearUncommittedEvents();

            _logger.LogInformation(
                "Incident {IncidentId} resolved for Trip {TripId}. Resolution: {ResolutionNotes}",
                request.IncidentId, tripId, request.ResolutionNotes);

            return OperationResult<TripResponse>.Ok(MapToResponse(trip));
        }
        catch (ConcurrencyConflictException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict resolving incident for Trip {TripId}", tripId);
            return OperationResult<TripResponse>.Fail(ex.Message, "CONCURRENCY_CONFLICT");
        }
        catch (DomainException ex)
        {
            _logger.LogError(ex, "Domain error resolving incident for Trip {TripId}", tripId);
            return OperationResult<TripResponse>.Fail(ex.Message, "DOMAIN_ERROR");
        }
    }

    public async Task<OperationResult<TripSummaryResponse>> CompleteTripAsync(
        Guid tripId,
        CompleteTripRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Completing Trip {TripId}", tripId);

        try
        {
            var trip = await _tripRepository.GetByIdAsync(tripId, cancellationToken);
            if (trip is null)
            {
                _logger.LogWarning("Trip completion failed: Trip {TripId} not found", tripId);
                return OperationResult<TripSummaryResponse>.Fail($"Trip {tripId} not found", "TRIP_NOT_FOUND");
            }

            trip.Complete(_clock.UtcNow);

            // Release driver and vehicle
            var driver = await _driverRepository.GetByIdAsync(trip.DriverId, cancellationToken);
            var vehicle = await _vehicleRepository.GetByIdAsync(trip.VehicleId, cancellationToken);

            driver?.MarkAsAvailable();
            vehicle?.MarkAsAvailable();

            await _tripRepository.SaveAsync(trip, request.ExpectedVersion, cancellationToken);
            if (driver is not null) await _driverRepository.SaveAsync(driver, cancellationToken);
            if (vehicle is not null) await _vehicleRepository.SaveAsync(vehicle, cancellationToken);

            trip.ClearUncommittedEvents();
            var summary = trip.GenerateSummary();

            _logger.LogInformation(
                "Trip {TripId} completed successfully. Duration: {Duration}, Checkpoints: {Checkpoints}, Incidents: {TotalIncidents} (resolved: {ResolvedIncidents})",
                tripId, FormatDuration(summary.Duration), summary.CheckpointsReached, summary.TotalIncidents, summary.ResolvedIncidents);

            return OperationResult<TripSummaryResponse>.Ok(MapToSummaryResponse(summary));
        }
        catch (ConcurrencyConflictException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict completing Trip {TripId}", tripId);
            return OperationResult<TripSummaryResponse>.Fail(ex.Message, "CONCURRENCY_CONFLICT");
        }
        catch (DomainException ex)
        {
            _logger.LogError(ex, "Domain error completing Trip {TripId}", tripId);
            return OperationResult<TripSummaryResponse>.Fail(ex.Message, "DOMAIN_ERROR");
        }
    }

    public async Task<OperationResult<TripSummaryResponse>> CancelTripAsync(
        Guid tripId,
        CancelTripRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Cancelling Trip {TripId}. Reason: {Reason}", tripId, request.Reason);

        try
        {
            var trip = await _tripRepository.GetByIdAsync(tripId, cancellationToken);
            if (trip is null)
            {
                _logger.LogWarning("Trip cancellation failed: Trip {TripId} not found", tripId);
                return OperationResult<TripSummaryResponse>.Fail($"Trip {tripId} not found", "TRIP_NOT_FOUND");
            }

            trip.Cancel(request.Reason, _clock.UtcNow);

            // Release driver and vehicle
            var driver = await _driverRepository.GetByIdAsync(trip.DriverId, cancellationToken);
            var vehicle = await _vehicleRepository.GetByIdAsync(trip.VehicleId, cancellationToken);

            driver?.MarkAsAvailable();
            vehicle?.MarkAsAvailable();

            await _tripRepository.SaveAsync(trip, request.ExpectedVersion, cancellationToken);
            if (driver is not null) await _driverRepository.SaveAsync(driver, cancellationToken);
            if (vehicle is not null) await _vehicleRepository.SaveAsync(vehicle, cancellationToken);

            trip.ClearUncommittedEvents();

            _logger.LogWarning(
                "Trip {TripId} cancelled. Reason: {Reason}. Checkpoints reached: {Checkpoints}, Incidents: {Incidents}",
                tripId, request.Reason, trip.ReachedCheckpointIds.Count, trip.Incidents.Count);

            return OperationResult<TripSummaryResponse>.Ok(MapToSummaryResponse(trip.GenerateSummary()));
        }
        catch (ConcurrencyConflictException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict cancelling Trip {TripId}", tripId);
            return OperationResult<TripSummaryResponse>.Fail(ex.Message, "CONCURRENCY_CONFLICT");
        }
        catch (DomainException ex)
        {
            _logger.LogError(ex, "Domain error cancelling Trip {TripId}", tripId);
            return OperationResult<TripSummaryResponse>.Fail(ex.Message, "DOMAIN_ERROR");
        }
    }

    public async Task<OperationResult<TripSummaryResponse>> GetTripSummaryAsync(
        Guid tripId,
        CancellationToken cancellationToken = default)
    {
        var trip = await _tripRepository.GetByIdAsync(tripId, cancellationToken);
        if (trip is null)
        {
            _logger.LogWarning("Trip summary request failed: Trip {TripId} not found", tripId);
            return OperationResult<TripSummaryResponse>.Fail($"Trip {tripId} not found", "TRIP_NOT_FOUND");
        }

        return OperationResult<TripSummaryResponse>.Ok(MapToSummaryResponse(trip.GenerateSummary()));
    }

    #region Mapping

    private static TripResponse MapToResponse(Trip trip)
    {
        return new TripResponse(
            trip.Id,
            trip.DriverId,
            trip.VehicleId,
            trip.RouteId,
            trip.CargoDescription,
            trip.Status.ToString(),
            trip.StartedAt,
            trip.CompletedAt,
            trip.Version,
            trip.ReachedCheckpointIds.Count,
            trip.Incidents.Select(MapToIncidentResponse).ToList());
    }

    private static TripSummaryResponse MapToSummaryResponse(TripSummary summary)
    {
        return new TripSummaryResponse(
            summary.TripId,
            summary.DriverId,
            summary.VehicleId,
            summary.RouteId,
            summary.CargoDescription,
            summary.Status.ToString(),
            summary.StartedAt,
            summary.CompletedAt,
            FormatDuration(summary.Duration),
            summary.CheckpointsReached,
            summary.TotalIncidents,
            summary.ResolvedIncidents,
            summary.HasCriticalIncidents,
            summary.Incidents.Select(MapToIncidentResponse).ToList());
    }

    private static IncidentResponse MapToIncidentResponse(TripIncident incident)
    {
        return new IncidentResponse(
            incident.Id,
            incident.Type.ToString(),
            incident.Description,
            incident.Severity.ToString(),
            incident.OccurredAt,
            incident.IsResolved,
            incident.ResolutionNotes,
            incident.ResolvedAt);
    }

    private static IncidentResponse MapToIncidentResponse(IncidentSummary incident)
    {
        return new IncidentResponse(
            incident.Id,
            incident.Type.ToString(),
            incident.Description,
            incident.Severity.ToString(),
            incident.OccurredAt,
            incident.IsResolved,
            incident.ResolutionNotes,
            incident.ResolvedAt);
    }

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalDays >= 1)
            return $"{(int)duration.TotalDays}d {duration.Hours}h {duration.Minutes}m";
        if (duration.TotalHours >= 1)
            return $"{(int)duration.TotalHours}h {duration.Minutes}m";
        return $"{duration.Minutes}m {duration.Seconds}s";
    }

    #endregion
}
