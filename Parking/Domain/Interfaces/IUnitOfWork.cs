namespace Parking.Domain.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IVehicleRepository Vehicles { get; }
    IAccessLogRepository AccessLogs { get; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
