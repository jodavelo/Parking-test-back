using MediatR;
using Parking.Domain.Interfaces;

namespace Parking.Application.Queries;

public class GetVehicleStatusQueryHandler : IRequestHandler<GetVehicleStatusQuery, VehicleStatusDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetVehicleStatusQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<VehicleStatusDto?> Handle(GetVehicleStatusQuery request, CancellationToken cancellationToken)
    {
        var vehicle = await _unitOfWork.Vehicles.GetByPlateAsync(
            request.VehiclePlate.ToUpperInvariant(), 
            cancellationToken);

        if (vehicle is null)
        {
            return null;
        }

        return new VehicleStatusDto(
            vehicle.Id,
            vehicle.Plate,
            vehicle.IsInside,
            vehicle.LastEntry,
            vehicle.LastExit,
            vehicle.CurrentUserId
        );
    }
}
