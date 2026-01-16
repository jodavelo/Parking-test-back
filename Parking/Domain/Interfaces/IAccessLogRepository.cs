using Parking.Domain.Entities;

namespace Parking.Domain.Interfaces;

public interface IAccessLogRepository
{
    Task<AccessLog> AddAsync(AccessLog log, CancellationToken cancellationToken = default);
    Task<IEnumerable<AccessLog>> GetByVehiclePlateAsync(string plate, CancellationToken cancellationToken = default);
    Task<IEnumerable<AccessLog>> GetAllAsync(int skip = 0, int take = 50, CancellationToken cancellationToken = default);
}
