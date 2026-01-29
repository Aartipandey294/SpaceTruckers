using Microsoft.AspNetCore.Mvc;
using SpaceTruckers.Application.DTOs;
using SpaceTruckers.Application.Services;

namespace SpaceTruckers.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VehiclesController : ControllerBase
{
    private readonly VehicleService _vehicleService;

    public VehiclesController(VehicleService vehicleService)
    {
        _vehicleService = vehicleService;
    }

    /// <summary>
    /// Creates a new vehicle.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(VehicleResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateVehicle(
        [FromBody] CreateVehicleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _vehicleService.CreateVehicleAsync(request, cancellationToken);
        
        if (!result.Success)
            return BadRequest(CreateProblemDetails(result.ErrorMessage!, result.ErrorCode));
        
        return CreatedAtAction(nameof(GetVehicle), new { vehicleId = result.Data!.Id }, result.Data);
    }

    /// <summary>
    /// Gets a vehicle by ID.
    /// </summary>
    [HttpGet("{vehicleId:guid}")]
    [ProducesResponseType(typeof(VehicleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetVehicle(Guid vehicleId, CancellationToken cancellationToken)
    {
        var result = await _vehicleService.GetVehicleAsync(vehicleId, cancellationToken);
        
        if (!result.Success)
            return NotFound(CreateProblemDetails(result.ErrorMessage!, result.ErrorCode));
        
        return Ok(result.Data);
    }

    /// <summary>
    /// Gets all vehicles.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<VehicleResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllVehicles(CancellationToken cancellationToken)
    {
        var result = await _vehicleService.GetAllVehiclesAsync(cancellationToken);
        return Ok(result.Data);
    }

    /// <summary>
    /// Gets all available vehicles.
    /// </summary>
    [HttpGet("available")]
    [ProducesResponseType(typeof(IReadOnlyList<VehicleResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAvailableVehicles(CancellationToken cancellationToken)
    {
        var result = await _vehicleService.GetAvailableVehiclesAsync(cancellationToken);
        return Ok(result.Data);
    }

    /// <summary>
    /// Updates a vehicle.
    /// </summary>
    [HttpPut("{vehicleId:guid}")]
    [ProducesResponseType(typeof(VehicleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateVehicle(
        Guid vehicleId,
        [FromBody] UpdateVehicleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _vehicleService.UpdateVehicleAsync(vehicleId, request, cancellationToken);
        
        if (!result.Success)
        {
            return result.ErrorCode == "VEHICLE_NOT_FOUND"
                ? NotFound(CreateProblemDetails(result.ErrorMessage!, result.ErrorCode))
                : BadRequest(CreateProblemDetails(result.ErrorMessage!, result.ErrorCode));
        }
        
        return Ok(result.Data);
    }

    /// <summary>
    /// Deletes a vehicle.
    /// </summary>
    [HttpDelete("{vehicleId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteVehicle(Guid vehicleId, CancellationToken cancellationToken)
    {
        var result = await _vehicleService.DeleteVehicleAsync(vehicleId, cancellationToken);
        
        if (!result.Success)
        {
            return result.ErrorCode == "VEHICLE_NOT_FOUND"
                ? NotFound(CreateProblemDetails(result.ErrorMessage!, result.ErrorCode))
                : BadRequest(CreateProblemDetails(result.ErrorMessage!, result.ErrorCode));
        }
        
        return NoContent();
    }

    private static ProblemDetails CreateProblemDetails(string detail, string? code) => new()
    {
        Detail = detail,
        Extensions = { ["code"] = code }
    };
}
