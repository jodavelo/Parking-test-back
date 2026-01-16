namespace Parking.Domain.Entities;

public class AccessLog
{
    public Guid Id { get; set; }
    public string VehiclePlate { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public AccessType AccessType { get; set; }
    public DateTime Timestamp { get; set; }
    public bool Success { get; set; }
    public string? FailureReason { get; set; }
    public DateTime CreatedAt { get; set; }

    public Guid? VehicleId { get; set; }
    public Vehicle? Vehicle { get; set; }
}
