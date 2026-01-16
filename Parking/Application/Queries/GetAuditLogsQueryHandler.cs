using MediatR;
using Parking.Domain.Interfaces;

namespace Parking.Application.Queries;

public class GetAuditLogsQueryHandler : IRequestHandler<GetAuditLogsQuery, IEnumerable<AuditLogDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAuditLogsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<AuditLogDto>> Handle(GetAuditLogsQuery request, CancellationToken cancellationToken)
    {
        var logs = await _unitOfWork.AccessLogs.GetAllAsync(
            request.Skip, 
            request.Take, 
            cancellationToken);

        return logs.Select(log => new AuditLogDto(
            log.Id,
            log.VehiclePlate,
            log.UserId,
            log.AccessType,
            log.Timestamp,
            log.Success,
            log.FailureReason,
            log.CreatedAt
        ));
    }
}
