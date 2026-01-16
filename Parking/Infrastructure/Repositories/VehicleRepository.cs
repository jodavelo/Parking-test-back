using Microsoft.EntityFrameworkCore;
using Parking.Domain.Entities;
using Parking.Domain.Interfaces;
using Parking.Infrastructure.Data;

namespace Parking.Infrastructure.Repositories;

public class VehicleRepository : IVehicleRepository
{
    private readonly ParkingDbContext _context;

    public VehicleRepository(ParkingDbContext context)
    {
        _context = context;
    }

    public async Task<Vehicle?> GetByPlateAsync(string plate, CancellationToken cancellationToken = default)
    {
        return await _context.Vehicles
            .FirstOrDefaultAsync(v => v.Plate == plate.ToUpperInvariant(), cancellationToken);
    }

    public async Task<Vehicle?> GetActiveVehicleByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Vehicles
            .FirstOrDefaultAsync(v => v.CurrentUserId == userId && v.IsInside, cancellationToken);
    }

    public async Task<Vehicle> AddAsync(Vehicle vehicle, CancellationToken cancellationToken = default)
    {
        vehicle.Plate = vehicle.Plate.ToUpperInvariant();
        await _context.Vehicles.AddAsync(vehicle, cancellationToken);
        return vehicle;
    }

    public Task UpdateAsync(Vehicle vehicle, CancellationToken cancellationToken = default)
    {
        _context.Vehicles.Update(vehicle);
        return Task.CompletedTask;
    }
}
