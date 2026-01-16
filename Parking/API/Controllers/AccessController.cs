using MediatR;
using Microsoft.AspNetCore.Mvc;
using Parking.Application.Commands;
using Parking.Application.Queries;
using Parking.Domain.Entities;

namespace Parking.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccessController : ControllerBase
{
    private readonly IMediator _mediator;

    public AccessController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<ActionResult<ProcessAccessResult>> ProcessAccess(
        [FromBody] ProcessAccessRequest request,
        CancellationToken cancellationToken)
    {
        var command = new ProcessAccessCommand(
            request.VehiclePlate,
            request.UserId,
            request.AccessType,
            request.Timestamp ?? DateTime.UtcNow
        );

        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpGet("vehicle/{plate}/status")]
    public async Task<ActionResult<VehicleStatusDto>> GetVehicleStatus(
        string plate,
        CancellationToken cancellationToken)
    {
        var query = new GetVehicleStatusQuery(plate);
        var result = await _mediator.Send(query, cancellationToken);

        if (result is null)
        {
            return NotFound(new { message = $"Vehículo con placa {plate} no encontrado" });
        }

        return Ok(result);
    }

    [HttpGet("audit")]
    public async Task<ActionResult<IEnumerable<AuditLogDto>>> GetAuditLogs(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        var query = new GetAuditLogsQuery(skip, take);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }
}

public record ProcessAccessRequest(
    string VehiclePlate,
    Guid UserId,
    AccessType AccessType,
    DateTime? Timestamp = null
);
