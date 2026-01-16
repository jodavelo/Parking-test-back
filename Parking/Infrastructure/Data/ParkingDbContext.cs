using Microsoft.EntityFrameworkCore;
using Parking.Domain.Entities;

namespace Parking.Infrastructure.Data;

public class ParkingDbContext : DbContext
{
    public ParkingDbContext(DbContextOptions<ParkingDbContext> options) : base(options)
    {
    }

    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<AccessLog> AccessLogs => Set<AccessLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Vehicle>(entity =>
        {
            entity.ToTable("Vehicles");
            entity.HasKey(v => v.Id);
            
            entity.HasIndex(v => v.Plate).IsUnique();
            
            entity.Property(v => v.Plate)
                  .HasMaxLength(20)
                  .IsRequired();
            
            entity.Property(v => v.RowVersion)
                  .IsRowVersion()
                  .IsConcurrencyToken();
            
            entity.HasIndex(v => new { v.CurrentUserId, v.IsInside });

            entity.HasMany(v => v.AccessLogs)
                  .WithOne(a => a.Vehicle)
                  .HasForeignKey(a => a.VehicleId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AccessLog>(entity =>
        {
            entity.ToTable("AccessLogs");
            entity.HasKey(a => a.Id);
            
            entity.Property(a => a.VehiclePlate)
                  .HasMaxLength(20)
                  .IsRequired();
            
            entity.Property(a => a.FailureReason)
                  .HasMaxLength(500);
            
            entity.Property(a => a.AccessType)
                  .HasConversion<string>()
                  .HasMaxLength(10);
            
            entity.HasIndex(a => a.VehiclePlate);
            entity.HasIndex(a => a.Timestamp);
            entity.HasIndex(a => a.UserId);
        });
    }
}
