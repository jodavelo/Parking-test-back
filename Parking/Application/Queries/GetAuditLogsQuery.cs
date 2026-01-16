using MediatR;
using Parking.Domain.Entities;

namespace Parking.Application.Queries;

public record GetAuditLogsQuery(int Skip = 0, int Take = 50) : IRequest<IEnumerable<AuditLogDto>>;

public record AuditLogDto(
    Guid Id,
    string VehiclePlate,
    Guid UserId,
    AccessType AccessType,
    DateTime Timestamp,
    bool Success,
    string? FailureReason,
    DateTime CreatedAt
);
