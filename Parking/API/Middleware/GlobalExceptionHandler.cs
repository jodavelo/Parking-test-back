using System.Net;
using System.Text.Json;
using FluentValidation;
using Parking.Domain.Exceptions;

namespace Parking.API.Middleware;

public class GlobalExceptionHandler
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IHostEnvironment _env;

    public GlobalExceptionHandler(RequestDelegate next, ILogger<GlobalExceptionHandler> logger, IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, errorCode, message) = exception switch
        {
            ValidationException validationEx => (
                HttpStatusCode.BadRequest,
                "VALIDATION_ERROR",
                string.Join("; ", validationEx.Errors.Select(e => e.ErrorMessage))),
            
            VehicleAlreadyInsideException domainEx => (
                HttpStatusCode.Conflict,
                domainEx.Code,
                domainEx.Message),
            
            VehicleNotInsideException domainEx => (
                HttpStatusCode.Conflict,
                domainEx.Code,
                domainEx.Message),
            
            UserHasActiveVehicleException domainEx => (
                HttpStatusCode.Conflict,
                domainEx.Code,
                domainEx.Message),
            
            ConcurrencyConflictException domainEx => (
                HttpStatusCode.Conflict,
                domainEx.Code,
                domainEx.Message),
            
            DomainException domainEx => (
                HttpStatusCode.BadRequest,
                domainEx.Code,
                domainEx.Message),
            
            _ => (
                HttpStatusCode.InternalServerError,
                "INTERNAL_ERROR",
                _env.IsDevelopment() 
                    ? exception.Message 
                    : "Ha ocurrido un error interno. Por favor, intente nuevamente.")
        };

        _logger.LogError(exception, "Error: {Message}", exception.Message);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var response = new ErrorResponse(
            Error: message,
            Code: errorCode,
            StatusCode: (int)statusCode,
            Timestamp: DateTime.UtcNow,
            Detail: _env.IsDevelopment() ? exception.StackTrace : null
        );

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, jsonOptions));
    }
}

public record ErrorResponse(
    string Error,
    string Code,
    int StatusCode,
    DateTime Timestamp,
    string? Detail = null
);
