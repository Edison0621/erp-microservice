using MediatR;
using ErpSystem.HR.Domain;

namespace ErpSystem.HR.Infrastructure;

public class HrProjections(HrReadDbContext readDb) :
    INotificationHandler<EmployeeHiredEvent>,
    INotificationHandler<EmployeeTransferredEvent>,
    INotificationHandler<EmployeePromotedEvent>,
    INotificationHandler<EmployeeTerminatedEvent>
{
    public async Task Handle(EmployeeHiredEvent n, CancellationToken ct)
    {
        EmployeeReadModel model = new()
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
            Status = nameof(EmployeeStatus.Active),
            CreatedAt = n.OccurredOn
        };
        readDb.Employees.Add(model);

        readDb.EmployeeEvents.Add(new EmployeeEventReadModel
        {
            Id = Guid.NewGuid(),
            EmployeeId = n.EmployeeId,
            EventType = nameof(EmployeeEventType.Hired),
            OccurredAt = n.OccurredOn,
            Description = "Employee hired",
            ToDepartmentId = n.DepartmentId,
            ToPositionId = n.PositionId
        });

        await readDb.SaveChangesAsync(ct);
    }

    public async Task Handle(EmployeeTransferredEvent n, CancellationToken ct)
    {
        EmployeeReadModel? emp = await readDb.Employees.FindAsync([n.EmployeeId], ct);
        if (emp != null)
        {
            emp.DepartmentId = n.ToDepartmentId;
            emp.PositionId = n.ToPositionId;

            readDb.EmployeeEvents.Add(new EmployeeEventReadModel
            {
                Id = Guid.NewGuid(),
                EmployeeId = n.EmployeeId,
                EventType = nameof(EmployeeEventType.Transferred),
                OccurredAt = n.OccurredOn,
                Description = n.Reason,
                FromDepartmentId = n.FromDepartmentId,
                ToDepartmentId = n.ToDepartmentId,
                FromPositionId = n.FromPositionId,
                ToPositionId = n.ToPositionId
            });

            await readDb.SaveChangesAsync(ct);
        }
    }

    public async Task Handle(EmployeePromotedEvent n, CancellationToken ct)
    {
        EmployeeReadModel? emp = await readDb.Employees.FindAsync([n.EmployeeId], ct);
        if (emp != null)
        {
            emp.PositionId = n.ToPositionId;

            readDb.EmployeeEvents.Add(new EmployeeEventReadModel
            {
                Id = Guid.NewGuid(),
                EmployeeId = n.EmployeeId,
                EventType = nameof(EmployeeEventType.Promoted),
                OccurredAt = n.OccurredOn,
                Description = n.Reason,
                FromPositionId = n.FromPositionId,
                ToPositionId = n.ToPositionId
            });

            await readDb.SaveChangesAsync(ct);
        }
    }

    public async Task Handle(EmployeeTerminatedEvent n, CancellationToken ct)
    {
        EmployeeReadModel? emp = await readDb.Employees.FindAsync([n.EmployeeId], ct);
        if (emp != null)
        {
            emp.Status = nameof(EmployeeStatus.Terminated);

            readDb.EmployeeEvents.Add(new EmployeeEventReadModel
            {
                Id = Guid.NewGuid(),
                EmployeeId = n.EmployeeId,
                EventType = nameof(EmployeeEventType.Terminated),
                OccurredAt = n.OccurredOn,
                Description = $"{n.Reason}: {n.Note}"
            });

            await readDb.SaveChangesAsync(ct);
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
