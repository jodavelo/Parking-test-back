using MediatR;
using Parking.Domain.Entities;

namespace Parking.Application.Commands;

public record ProcessAccessCommand(
    string VehiclePlate,
    Guid UserId,
    AccessType AccessType,
    DateTime Timestamp
) : IRequest<ProcessAccessResult>;

public record ProcessAccessResult(
    bool Success,
    string Message,
    Guid? LogId = null,
    string? ErrorCode = null
);
