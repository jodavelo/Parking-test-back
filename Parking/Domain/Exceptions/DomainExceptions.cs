namespace Parking.Domain.Exceptions;

public abstract class DomainException : Exception
{
    public string Code { get; }
    
    protected DomainException(string code, string message) : base(message)
    {
        Code = code;
    }
}

public class VehicleAlreadyInsideException : DomainException
{
    public string Plate { get; }
    
    public VehicleAlreadyInsideException(string plate) 
        : base("VEHICLE_ALREADY_INSIDE", $"El vehículo {plate} ya se encuentra dentro del estacionamiento")
    {
        Plate = plate;
    }
}

public class VehicleNotInsideException : DomainException
{
    public string Plate { get; }
    
    public VehicleNotInsideException(string plate) 
        : base("VEHICLE_NOT_INSIDE", $"El vehículo {plate} no se encuentra dentro del estacionamiento")
    {
        Plate = plate;
    }
}

public class UserHasActiveVehicleException : DomainException
{
    public Guid UserId { get; }
    public string ActivePlate { get; }
    
    public UserHasActiveVehicleException(Guid userId, string activePlate) 
        : base("USER_HAS_ACTIVE_VEHICLE", $"El usuario ya tiene un vehículo activo ({activePlate}) dentro del estacionamiento")
    {
        UserId = userId;
        ActivePlate = activePlate;
    }
}

public class ConcurrencyConflictException : DomainException
{
    public ConcurrencyConflictException() 
        : base("CONCURRENCY_CONFLICT", "El registro fue modificado por otro proceso. Por favor, intente nuevamente.")
    {
    }
}
