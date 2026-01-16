using FluentValidation;
using Parking.Application.Commands;

namespace Parking.Application.Validators;

public class ProcessAccessCommandValidator : AbstractValidator<ProcessAccessCommand>
{
    public ProcessAccessCommandValidator()
    {
        RuleFor(x => x.VehiclePlate)
            .NotEmpty()
            .WithMessage("La placa del vehículo es requerida")
            .MaximumLength(20)
            .WithMessage("La placa no puede exceder 20 caracteres")
            .Matches(@"^[A-Za-z0-9\-]+$")
            .WithMessage("La placa solo puede contener letras, números y guiones");

        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("El ID del usuario es requerido");

        RuleFor(x => x.AccessType)
            .IsInEnum()
            .WithMessage("El tipo de acceso debe ser Entry (0) o Exit (1)");

        RuleFor(x => x.Timestamp)
            .NotEmpty()
            .WithMessage("La fecha y hora es requerida")
            .LessThanOrEqualTo(DateTime.UtcNow.AddMinutes(5))
            .WithMessage("La fecha y hora no puede estar en el futuro");
    }
}
