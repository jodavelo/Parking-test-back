using MediatR;
using Parking.Domain.Entities;
using Parking.Domain.Exceptions;
using Parking.Domain.Interfaces;

namespace Parking.Application.Commands;

public class ProcessAccessCommandHandler : IRequestHandler<ProcessAccessCommand, ProcessAccessResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ProcessAccessCommandHandler> _logger;

    public ProcessAccessCommandHandler(IUnitOfWork unitOfWork, ILogger<ProcessAccessCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ProcessAccessResult> Handle(ProcessAccessCommand request, CancellationToken cancellationToken)
    {
        var normalizedPlate = request.VehiclePlate.ToUpperInvariant();
        
        _logger.LogInformation(
            "Procesando acceso: Placa={Plate}, Usuario={UserId}, Tipo={AccessType}",
            normalizedPlate, request.UserId, request.AccessType);

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        
        try
        {
            var vehicle = await _unitOfWork.Vehicles.GetByPlateAsync(normalizedPlate, cancellationToken);
            
            if (request.AccessType == AccessType.Entry)
            {
                await ValidateEntryRulesAsync(request, vehicle, cancellationToken);
            }
            else
            {
                ValidateExitRules(normalizedPlate, vehicle);
            }

            vehicle = await UpdateVehicleStateAsync(request, vehicle, cancellationToken);

            var log = await CreateAuditLogAsync(request, vehicle.Id, true, null, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation(
                "Acceso concedido: Placa={Plate}, Tipo={AccessType}, LogId={LogId}",
                normalizedPlate, request.AccessType, log.Id);

            var message = request.AccessType == AccessType.Entry 
                ? "Entrada registrada exitosamente" 
                : "Salida registrada exitosamente";

            return new ProcessAccessResult(true, message, log.Id);
        }
        catch (DomainException ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            
            var failedLog = await CreateAuditLogAsync(request, null, false, ex.Message, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogWarning(
                ex, "Acceso denegado: Placa={Plate}, Tipo={AccessType}, Razon={Reason}",
                normalizedPlate, request.AccessType, ex.Message);
            
            throw;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            
            _logger.LogError(ex, "Error procesando acceso para placa {Plate}", normalizedPlate);
            throw;
        }
    }

    private async Task ValidateEntryRulesAsync(
        ProcessAccessCommand request, 
        Vehicle? vehicle, 
        CancellationToken cancellationToken)
    {
        var normalizedPlate = request.VehiclePlate.ToUpperInvariant();
        
        if (vehicle?.IsInside == true)
        {
            throw new VehicleAlreadyInsideException(normalizedPlate);
        }

        var activeVehicle = await _unitOfWork.Vehicles.GetActiveVehicleByUserAsync(
            request.UserId, cancellationToken);
        
        if (activeVehicle is not null && activeVehicle.Plate != normalizedPlate)
        {
            throw new UserHasActiveVehicleException(request.UserId, activeVehicle.Plate);
        }
    }

    private static void ValidateExitRules(string plate, Vehicle? vehicle)
    {
        if (vehicle is null || !vehicle.IsInside)
        {
            throw new VehicleNotInsideException(plate);
        }
    }

    private async Task<Vehicle> UpdateVehicleStateAsync(
        ProcessAccessCommand request, 
        Vehicle? vehicle, 
        CancellationToken cancellationToken)
    {
        var normalizedPlate = request.VehiclePlate.ToUpperInvariant();
        
        if (vehicle is null)
        {
            vehicle = new Vehicle
            {
                Id = Guid.NewGuid(),
                Plate = normalizedPlate,
                CurrentUserId = request.UserId,
                IsInside = true,
                LastEntry = request.Timestamp
            };
            await _unitOfWork.Vehicles.AddAsync(vehicle, cancellationToken);
        }
        else
        {
            vehicle.CurrentUserId = request.UserId;
            vehicle.IsInside = request.AccessType == AccessType.Entry;
            
            if (request.AccessType == AccessType.Entry)
            {
                vehicle.LastEntry = request.Timestamp;
            }
            else
            {
                vehicle.LastExit = request.Timestamp;
            }
            
            await _unitOfWork.Vehicles.UpdateAsync(vehicle, cancellationToken);
        }

        return vehicle;
    }

    private async Task<AccessLog> CreateAuditLogAsync(
        ProcessAccessCommand request, 
        Guid? vehicleId, 
        bool success, 
        string? failureReason, 
        CancellationToken cancellationToken)
    {
        var log = new AccessLog
        {
            Id = Guid.NewGuid(),
            VehiclePlate = request.VehiclePlate.ToUpperInvariant(),
            UserId = request.UserId,
            AccessType = request.AccessType,
            Timestamp = request.Timestamp,
            Success = success,
            FailureReason = failureReason,
            VehicleId = vehicleId
        };

        return await _unitOfWork.AccessLogs.AddAsync(log, cancellationToken);
    }
}
