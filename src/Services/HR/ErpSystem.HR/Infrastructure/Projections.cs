using MediatR;
using ErpSystem.HR.Domain;
using Microsoft.EntityFrameworkCore;

namespace ErpSystem.HR.Infrastructure;

public class HRProjections : 
    INotificationHandler<EmployeeHiredEvent>,
    INotificationHandler<EmployeeTransferredEvent>,
    INotificationHandler<EmployeePromotedEvent>,
    INotificationHandler<EmployeeTerminatedEvent>
{
    private readonly HRReadDbContext _readDb;

    public HRProjections(HRReadDbContext readDb)
    {
        _readDb = readDb;
    }

    public async Task Handle(EmployeeHiredEvent n, CancellationToken ct)
    {
        var model = new EmployeeReadModel
        {
            Id = n.EmployeeId,
            EmployeeNumber = n.EmployeeNumber,
            FullName = n.FullName,
            Gender = n.Gender,
            DateOfBirth = n.DateOfBirth,
            IdType = n.IdType,
            IdNumber = n.IdNumber,
            HireDate = n.HireDate,
            EmploymentType = n.EmploymentType.ToString(),
            CompanyId = n.CompanyId,
            DepartmentId = n.DepartmentId,
            PositionId = n.PositionId,
            ManagerEmployeeId = n.ManagerEmployeeId,
            CostCenterId = n.CostCenterId,
            Status = EmployeeStatus.Active.ToString(),
            CreatedAt = n.OccurredOn
        };
        _readDb.Employees.Add(model);

        _readDb.EmployeeEvents.Add(new EmployeeEventReadModel
        {
            Id = Guid.NewGuid(),
            EmployeeId = n.EmployeeId,
            EventType = EmployeeEventType.Hired.ToString(),
            OccurredAt = n.OccurredOn,
            Description = "Employee hired",
            ToDepartmentId = n.DepartmentId,
            ToPositionId = n.PositionId
        });

        await _readDb.SaveChangesAsync(ct);
    }

    public async Task Handle(EmployeeTransferredEvent n, CancellationToken ct)
    {
        var emp = await _readDb.Employees.FindAsync(new object[] { n.EmployeeId }, ct);
        if (emp != null)
        {
            emp.DepartmentId = n.ToDepartmentId;
            emp.PositionId = n.ToPositionId;

            _readDb.EmployeeEvents.Add(new EmployeeEventReadModel
            {
                Id = Guid.NewGuid(),
                EmployeeId = n.EmployeeId,
                EventType = EmployeeEventType.Transferred.ToString(),
                OccurredAt = n.OccurredOn,
                Description = n.Reason,
                FromDepartmentId = n.FromDepartmentId,
                ToDepartmentId = n.ToDepartmentId,
                FromPositionId = n.FromPositionId,
                ToPositionId = n.ToPositionId
            });

            await _readDb.SaveChangesAsync(ct);
        }
    }

    public async Task Handle(EmployeePromotedEvent n, CancellationToken ct)
    {
        var emp = await _readDb.Employees.FindAsync(new object[] { n.EmployeeId }, ct);
        if (emp != null)
        {
            emp.PositionId = n.ToPositionId;

            _readDb.EmployeeEvents.Add(new EmployeeEventReadModel
            {
                Id = Guid.NewGuid(),
                EmployeeId = n.EmployeeId,
                EventType = EmployeeEventType.Promoted.ToString(),
                OccurredAt = n.OccurredOn,
                Description = n.Reason,
                FromPositionId = n.FromPositionId,
                ToPositionId = n.ToPositionId
            });

            await _readDb.SaveChangesAsync(ct);
        }
    }

    public async Task Handle(EmployeeTerminatedEvent n, CancellationToken ct)
    {
        var emp = await _readDb.Employees.FindAsync(new object[] { n.EmployeeId }, ct);
        if (emp != null)
        {
            emp.Status = EmployeeStatus.Terminated.ToString();

            _readDb.EmployeeEvents.Add(new EmployeeEventReadModel
            {
                Id = Guid.NewGuid(),
                EmployeeId = n.EmployeeId,
                EventType = EmployeeEventType.Terminated.ToString(),
                OccurredAt = n.OccurredOn,
                Description = $"{n.Reason}: {n.Note}"
            });

            await _readDb.SaveChangesAsync(ct);
        }
    }
}

public enum EmployeeEventType
{
    Hired = 1,
    Transferred = 2,
    Promoted = 3,
    Terminated = 4,
    InfoChanged = 5
}
