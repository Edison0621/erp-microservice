using ErpSystem.BuildingBlocks.Domain;

namespace ErpSystem.HR.Domain;

public enum EmploymentType
{
    FullTime = 1,
    PartTime = 2,
    Intern = 3,
    Contractor = 4
}

public enum EmployeeStatus
{
    Active = 1,
    Inactive = 2,
    Terminated = 3
}

// Events
public record EmployeeHiredEvent(
    Guid EmployeeId,
    string EmployeeNumber,
    string FullName,
    string Gender,
    DateTime? DateOfBirth,
    string IdType,
    string IdNumber,
    DateTime HireDate,
    EmploymentType EmploymentType,
    string CompanyId,
    string DepartmentId,
    string PositionId,
    string ManagerEmployeeId,
    string CostCenterId
) : IDomainEvent {
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record EmployeeTransferredEvent(
    Guid EmployeeId,
    string FromDepartmentId,
    string ToDepartmentId,
    string FromPositionId,
    string ToPositionId,
    DateTime EffectiveDate,
    string Reason
) : IDomainEvent {
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record EmployeePromotedEvent(
    Guid EmployeeId,
    string FromPositionId,
    string ToPositionId,
    DateTime EffectiveDate,
    string Reason
) : IDomainEvent {
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record EmployeeTerminatedEvent(
    Guid EmployeeId,
    DateTime TerminationDate,
    string Reason,
    string Note
) : IDomainEvent {
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

// Aggregate
public class Employee : AggregateRoot<Guid>
{
    public string EmployeeNumber { get; private set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;
    public string DepartmentId { get; private set; } = string.Empty;
    public string PositionId { get; private set; } = string.Empty;
    public EmployeeStatus Status { get; private set; }
    public string Email { get; private set; } = string.Empty;

    public static Employee Hire(
        Guid id, 
        string employeeNumber, 
        string fullName, 
        string gender, 
        DateTime? dateOfBirth, 
        string idType, 
        string idNumber, 
        DateTime hireDate, 
        EmploymentType employmentType, 
        string companyId, 
        string departmentId, 
        string positionId, 
        string managerEmployeeId, 
        string costCenterId)
    {
        var employee = new Employee();
        employee.ApplyChange(new EmployeeHiredEvent(
            id, employeeNumber, fullName, gender, dateOfBirth, idType, idNumber, 
            hireDate, employmentType, companyId, departmentId, positionId, managerEmployeeId, costCenterId));
        return employee;
    }

    public void Transfer(string toDepartmentId, string toPositionId, DateTime effectiveDate, string reason)
    {
        if (Status == EmployeeStatus.Terminated) throw new InvalidOperationException("Cannot transfer a terminated employee");
        ApplyChange(new EmployeeTransferredEvent(Id, DepartmentId, toDepartmentId, PositionId, toPositionId, effectiveDate, reason));
    }

    public void Promote(string toPositionId, DateTime effectiveDate, string reason)
    {
        if (Status == EmployeeStatus.Terminated) throw new InvalidOperationException("Cannot promote a terminated employee");
        ApplyChange(new EmployeePromotedEvent(Id, PositionId, toPositionId, effectiveDate, reason));
    }

    public void Terminate(DateTime terminationDate, string reason, string note)
    {
        if (Status == EmployeeStatus.Terminated) throw new InvalidOperationException("Employee already terminated");
        ApplyChange(new EmployeeTerminatedEvent(Id, terminationDate, reason, note));
    }

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case EmployeeHiredEvent e:
                Id = e.EmployeeId;
                EmployeeNumber = e.EmployeeNumber;
                FullName = e.FullName;
                DepartmentId = e.DepartmentId;
                PositionId = e.PositionId;
                Status = EmployeeStatus.Active;
                break;
            case EmployeeTransferredEvent e:
                DepartmentId = e.ToDepartmentId;
                PositionId = e.ToPositionId;
                break;
            case EmployeePromotedEvent e:
                PositionId = e.ToPositionId;
                break;
            case EmployeeTerminatedEvent:
                Status = EmployeeStatus.Terminated;
                break;
        }
    }
}
