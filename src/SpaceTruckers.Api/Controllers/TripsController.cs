using Microsoft.AspNetCore.Mvc;
using SpaceTruckers.Application.DTOs;
using SpaceTruckers.Application.Services;

namespace SpaceTruckers.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TripsController : ControllerBase
{
    private readonly TripService _tripService;

    public TripsController(TripService tripService)
    {
        _tripService = tripService;
    }

    /// <summary>
    /// Creates a new delivery trip.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(TripResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateTrip(
        [FromBody] CreateTripRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _tripService.CreateTripAsync(request, cancellationToken);
        
        if (!result.Success)
            return BadRequest(CreateProblemDetails(result.ErrorMessage!, result.ErrorCode));
        
        return CreatedAtAction(nameof(GetTrip), new { tripId = result.Data!.Id }, result.Data);
    }

    /// <summary>
    /// Gets a trip by ID.
    /// </summary>
    [HttpGet("{tripId:guid}")]
    [ProducesResponseType(typeof(TripResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTrip(Guid tripId, CancellationToken cancellationToken)
    {
        var result = await _tripService.GetTripAsync(tripId, cancellationToken);
        
        if (!result.Success)
            return NotFound(CreateProblemDetails(result.ErrorMessage!, result.ErrorCode));
        
        return Ok(result.Data);
    }

    /// <summary>
    /// Gets all active (in-progress) trips.
    /// </summary>
    [HttpGet("active")]
    [ProducesResponseType(typeof(IReadOnlyList<TripResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActiveTrips(CancellationToken cancellationToken)
    {
        var result = await _tripService.GetActiveTripsAsync(cancellationToken);
        return Ok(result.Data);
    }

    /// <summary>
    /// Records reaching a checkpoint on the trip.
    /// </summary>
    [HttpPost("{tripId:guid}/checkpoints")]
    [ProducesResponseType(typeof(TripResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ReachCheckpoint(
        Guid tripId,
        [FromBody] ReachCheckpointRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _tripService.ReachCheckpointAsync(tripId, request, cancellationToken);
        
        if (!result.Success)
        {
            return result.ErrorCode == "CONCURRENCY_CONFLICT"
                ? Conflict(CreateProblemDetails(result.ErrorMessage!, result.ErrorCode))
                : BadRequest(CreateProblemDetails(result.ErrorMessage!, result.ErrorCode));
        }
        
        return Ok(result.Data);
    }

    /// <summary>
    /// Records an incident during the trip.
    /// </summary>
    [HttpPost("{tripId:guid}/incidents")]
    [ProducesResponseType(typeof(TripResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RecordIncident(
        Guid tripId,
        [FromBody] RecordIncidentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _tripService.RecordIncidentAsync(tripId, request, cancellationToken);
        
        if (!result.Success)
        {
            return result.ErrorCode == "CONCURRENCY_CONFLICT"
                ? Conflict(CreateProblemDetails(result.ErrorMessage!, result.ErrorCode))
                : BadRequest(CreateProblemDetails(result.ErrorMessage!, result.ErrorCode));
        }
        
        return Ok(result.Data);
    }

    /// <summary>
    /// Resolves an incident on the trip.
    /// </summary>
    [HttpPost("{tripId:guid}/incidents/resolve")]
    [ProducesResponseType(typeof(TripResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ResolveIncident(
        Guid tripId,
        [FromBody] ResolveIncidentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _tripService.ResolveIncidentAsync(tripId, request, cancellationToken);
        
        if (!result.Success)
        {
            return result.ErrorCode == "CONCURRENCY_CONFLICT"
                ? Conflict(CreateProblemDetails(result.ErrorMessage!, result.ErrorCode))
                : BadRequest(CreateProblemDetails(result.ErrorMessage!, result.ErrorCode));
        }
        
        return Ok(result.Data);
    }

    /// <summary>
    /// Completes a trip successfully.
    /// </summary>
    [HttpPost("{tripId:guid}/complete")]
    [ProducesResponseType(typeof(TripSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CompleteTrip(
        Guid tripId,
        [FromBody] CompleteTripRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _tripService.CompleteTripAsync(tripId, request, cancellationToken);
        
        if (!result.Success)
        {
            return result.ErrorCode == "CONCURRENCY_CONFLICT"
                ? Conflict(CreateProblemDetails(result.ErrorMessage!, result.ErrorCode))
                : BadRequest(CreateProblemDetails(result.ErrorMessage!, result.ErrorCode));
        }
        
        return Ok(result.Data);
    }

    /// <summary>
    /// Cancels a trip.
    /// </summary>
    [HttpPost("{tripId:guid}/cancel")]
    [ProducesResponseType(typeof(TripSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CancelTrip(
        Guid tripId,
        [FromBody] CancelTripRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _tripService.CancelTripAsync(tripId, request, cancellationToken);
        
        if (!result.Success)
        {
            return result.ErrorCode == "CONCURRENCY_CONFLICT"
                ? Conflict(CreateProblemDetails(result.ErrorMessage!, result.ErrorCode))
                : BadRequest(CreateProblemDetails(result.ErrorMessage!, result.ErrorCode));
        }
        
        return Ok(result.Data);
    }

    /// <summary>
    /// Gets a detailed summary of a trip.
    /// </summary>
    [HttpGet("{tripId:guid}/summary")]
    [ProducesResponseType(typeof(TripSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTripSummary(Guid tripId, CancellationToken cancellationToken)
    {
        var result = await _tripService.GetTripSummaryAsync(tripId, cancellationToken);
        
        if (!result.Success)
            return NotFound(CreateProblemDetails(result.ErrorMessage!, result.ErrorCode));
        
        return Ok(result.Data);
    }

    private static ProblemDetails CreateProblemDetails(string detail, string? code) => new()
    {
        Detail = detail,
        Extensions = { ["code"] = code }
    };
}
