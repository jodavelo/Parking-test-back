using Microsoft.EntityFrameworkCore;
using Parking.Domain.Entities;
using Parking.Domain.Interfaces;
using Parking.Infrastructure.Data;

namespace Parking.Infrastructure.Repositories;

public class AccessLogRepository : IAccessLogRepository
{
    private readonly ParkingDbContext _context;

    public AccessLogRepository(ParkingDbContext context)
    {
        _context = context;
    }

    public async Task<AccessLog> AddAsync(AccessLog log, CancellationToken cancellationToken = default)
    {
        log.VehiclePlate = log.VehiclePlate.ToUpperInvariant();
        log.CreatedAt = DateTime.UtcNow;
        await _context.AccessLogs.AddAsync(log, cancellationToken);
        return log;
    }

    public async Task<IEnumerable<AccessLog>> GetByVehiclePlateAsync(string plate, CancellationToken cancellationToken = default)
    {
        return await _context.AccessLogs
            .Where(a => a.VehiclePlate == plate.ToUpperInvariant())
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AccessLog>> GetAllAsync(int skip = 0, int take = 50, CancellationToken cancellationToken = default)
    {
        return await _context.AccessLogs
            .OrderByDescending(a => a.Timestamp)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }
}
