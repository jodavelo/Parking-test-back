using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Parking.API.Middleware;
using Parking.Application.Behaviors;
using Parking.Application.Commands;
using Parking.Application.Validators;
using Parking.Domain.Interfaces;
using Parking.Infrastructure.Data;
using Parking.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<ParkingDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure(3)));

builder.Services.AddMediatR(cfg => 
    cfg.RegisterServicesFromAssembly(typeof(ProcessAccessCommand).Assembly));

builder.Services.AddValidatorsFromAssemblyContaining<ProcessAccessCommandValidator>();
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

builder.Services.AddScoped<IVehicleRepository, VehicleRepository>();
builder.Services.AddScoped<IAccessLogRepository, AccessLogRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                "http://localhost:4200",
                "http://localhost:3000",
                "http://localhost:5173",
                "https://localhost:5001")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ParkingDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
}

app.UseMiddleware<GlobalExceptionHandler>();

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }
