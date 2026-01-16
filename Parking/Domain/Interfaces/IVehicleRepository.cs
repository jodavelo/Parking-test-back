using Parking.Domain.Entities;

namespace Parking.Domain.Interfaces;

public interface IVehicleRepository
{
    Task<Vehicle?> GetByPlateAsync(string plate, CancellationToken cancellationToken = default);
    Task<Vehicle?> GetActiveVehicleByUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Vehicle> AddAsync(Vehicle vehicle, CancellationToken cancellationToken = default);
    Task UpdateAsync(Vehicle vehicle, CancellationToken cancellationToken = default);
}
