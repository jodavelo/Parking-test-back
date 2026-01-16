using MediatR;

namespace Parking.Application.Queries;

public record GetVehicleStatusQuery(string VehiclePlate) : IRequest<VehicleStatusDto?>;

public record VehicleStatusDto(
    Guid Id,
    string Plate,
    bool IsInside,
    DateTime? LastEntry,
    DateTime? LastExit,
    Guid CurrentUserId
);
