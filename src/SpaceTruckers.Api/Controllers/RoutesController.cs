using Microsoft.AspNetCore.Mvc;
using SpaceTruckers.Application.DTOs;
using SpaceTruckers.Application.Services;

namespace SpaceTruckers.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RoutesController : ControllerBase
{
    private readonly RouteService _routeService;

    public RoutesController(RouteService routeService)
    {
        _routeService = routeService;
    }

    /// <summary>
    /// Creates a new route.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(RouteResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateRoute(
        [FromBody] CreateRouteRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _routeService.CreateRouteAsync(request, cancellationToken);
        
        if (!result.Success)
            return BadRequest(CreateProblemDetails(result.ErrorMessage!, result.ErrorCode));
        
        return CreatedAtAction(nameof(GetRoute), new { routeId = result.Data!.Id }, result.Data);
    }

    /// <summary>
    /// Gets a route by ID.
    /// </summary>
    [HttpGet("{routeId:guid}")]
    [ProducesResponseType(typeof(RouteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRoute(Guid routeId, CancellationToken cancellationToken)
    {
        var result = await _routeService.GetRouteAsync(routeId, cancellationToken);
        
        if (!result.Success)
            return NotFound(CreateProblemDetails(result.ErrorMessage!, result.ErrorCode));
        
        return Ok(result.Data);
    }

    /// <summary>
    /// Gets all routes.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<RouteResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllRoutes(CancellationToken cancellationToken)
    {
        var result = await _routeService.GetAllRoutesAsync(cancellationToken);
        return Ok(result.Data);
    }

    /// <summary>
    /// Adds a checkpoint to a route.
    /// </summary>
    [HttpPost("{routeId:guid}/checkpoints")]
    [ProducesResponseType(typeof(RouteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddCheckpoint(
        Guid routeId,
        [FromBody] AddCheckpointRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _routeService.AddCheckpointAsync(routeId, request, cancellationToken);
        
        if (!result.Success)
        {
            return result.ErrorCode == "ROUTE_NOT_FOUND"
                ? NotFound(CreateProblemDetails(result.ErrorMessage!, result.ErrorCode))
                : BadRequest(CreateProblemDetails(result.ErrorMessage!, result.ErrorCode));
        }
        
        return Ok(result.Data);
    }

    /// <summary>
    /// Deletes a route.
    /// </summary>
    [HttpDelete("{routeId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteRoute(Guid routeId, CancellationToken cancellationToken)
    {
        var result = await _routeService.DeleteRouteAsync(routeId, cancellationToken);
        
        if (!result.Success)
            return NotFound(CreateProblemDetails(result.ErrorMessage!, result.ErrorCode));
        
        return NoContent();
    }

    private static ProblemDetails CreateProblemDetails(string detail, string? code) => new()
    {
        Detail = detail,
        Extensions = { ["code"] = code }
    };
}
