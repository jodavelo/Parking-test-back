using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Parking.Domain.Exceptions;
using Parking.Domain.Interfaces;
using Parking.Infrastructure.Data;

namespace Parking.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly ParkingDbContext _context;
    private IDbContextTransaction? _transaction;
    
    public IVehicleRepository Vehicles { get; }
    public IAccessLogRepository AccessLogs { get; }

    public UnitOfWork(
        ParkingDbContext context, 
        IVehicleRepository vehicleRepository, 
        IAccessLogRepository accessLogRepository)
    {
        _context = context;
        Vehicles = vehicleRepository;
        AccessLogs = accessLogRepository;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyConflictException();
        }
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is not null)
        {
            await _transaction.CommitAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is not null)
        {
            await _transaction.RollbackAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}
