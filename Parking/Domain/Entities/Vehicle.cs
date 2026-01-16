namespace Parking.Domain.Entities;

public class Vehicle
{
    public Guid Id { get; set; }
    public string Plate { get; set; } = string.Empty;
    public Guid CurrentUserId { get; set; }
    public bool IsInside { get; set; }
    public DateTime? LastEntry { get; set; }
    public DateTime? LastExit { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public ICollection<AccessLog> AccessLogs { get; set; } = [];
}
