using Microsoft.AspNetCore.Mvc;
using SpaceTruckers.Application.DTOs;
using SpaceTruckers.Application.Services;

namespace SpaceTruckers.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DriversController : ControllerBase
{
    private readonly DriverService _driverService;

    public DriversController(DriverService driverService)
    {
        _driverService = driverService;
    }

    /// <summary>
    /// Creates a new driver.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(DriverResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateDriver(
        [FromBody] CreateDriverRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _driverService.CreateDriverAsync(request, cancellationToken);
        
        if (!result.Success)
            return BadRequest(CreateProblemDetails(result.ErrorMessage!, result.ErrorCode));
        
        return CreatedAtAction(nameof(GetDriver), new { driverId = result.Data!.Id }, result.Data);
    }

    /// <summary>
    /// Gets a driver by ID.
    /// </summary>
    [HttpGet("{driverId:guid}")]
    [ProducesResponseType(typeof(DriverResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDriver(Guid driverId, CancellationToken cancellationToken)
    {
        var result = await _driverService.GetDriverAsync(driverId, cancellationToken);
        
        if (!result.Success)
            return NotFound(CreateProblemDetails(result.ErrorMessage!, result.ErrorCode));
        
        return Ok(result.Data);
    }

    /// <summary>
    /// Gets all drivers.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<DriverResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllDrivers(CancellationToken cancellationToken)
    {
        var result = await _driverService.GetAllDriversAsync(cancellationToken);
        return Ok(result.Data);
    }

    /// <summary>
    /// Gets all available drivers.
    /// </summary>
    [HttpGet("available")]
    [ProducesResponseType(typeof(IReadOnlyList<DriverResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAvailableDrivers(CancellationToken cancellationToken)
    {
        var result = await _driverService.GetAvailableDriversAsync(cancellationToken);
        return Ok(result.Data);
    }

    /// <summary>
    /// Updates a driver.
    /// </summary>
    [HttpPut("{driverId:guid}")]
    [ProducesResponseType(typeof(DriverResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateDriver(
        Guid driverId,
        [FromBody] UpdateDriverRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _driverService.UpdateDriverAsync(driverId, request, cancellationToken);
        
        if (!result.Success)
        {
            return result.ErrorCode == "DRIVER_NOT_FOUND"
                ? NotFound(CreateProblemDetails(result.ErrorMessage!, result.ErrorCode))
                : BadRequest(CreateProblemDetails(result.ErrorMessage!, result.ErrorCode));
        }
        
        return Ok(result.Data);
    }

    /// <summary>
    /// Deletes a driver.
    /// </summary>
    [HttpDelete("{driverId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteDriver(Guid driverId, CancellationToken cancellationToken)
    {
        var result = await _driverService.DeleteDriverAsync(driverId, cancellationToken);
        
        if (!result.Success)
        {
            return result.ErrorCode == "DRIVER_NOT_FOUND"
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
