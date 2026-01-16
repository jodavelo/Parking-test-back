using System.Net;
using System.Text.Json;
using FluentValidation;
using Parking.Domain.Exceptions;

namespace Parking.API.Middleware;

public class GlobalExceptionHandler
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(RequestDelegate next, ILogger<GlobalExceptionHandler> logger)
    {
        _next = next;
        _logger = logger;
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
                "Ha ocurrido un error interno. Por favor, intente nuevamente.")
        };

        if (statusCode == HttpStatusCode.InternalServerError)
        {
            _logger.LogError(exception, "Error no manejado: {Message}", exception.Message);
        }
        else
        {
            _logger.LogWarning("Excepcion controlada: {Code} - {Message}", errorCode, message);
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var response = new ErrorResponse(
            Error: message,
            Code: errorCode,
            StatusCode: (int)statusCode,
            Timestamp: DateTime.UtcNow
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
    DateTime Timestamp
);
