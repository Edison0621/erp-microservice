using MediatR;

namespace ErpSystem.Identity.Application.IntegrationEvents;

public class HRIntegrationEvents
{
    public record EmployeeHiredIntegrationEvent(
        Guid EmployeeId,
        string EmployeeNumber,
        string FullName,
        string DepartmentId,
        string PositionId,
        string Email
    ) : INotification;

    public record EmployeeTerminatedIntegrationEvent(
        Guid EmployeeId,
        string EmployeeNumber,
        string FullName
    ) : INotification;
}
