using MediatR;

namespace ErpSystem.HR.Domain;

public class HrIntegrationEvents
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
