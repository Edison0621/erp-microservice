using MediatR;
using ErpSystem.BuildingBlocks.Domain;
using ErpSystem.BuildingBlocks.EventBus;
using ErpSystem.HR.Domain;
using ErpSystem.HR.Infrastructure;

namespace ErpSystem.HR.Application;

public record HireEmployeeCommand(
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
    string CostCenterId,
    string Email
) : IRequest<Guid>;

public record TransferEmployeeCommand(
    Guid EmployeeId,
    string ToDepartmentId,
    string ToPositionId,
    DateTime EffectiveDate,
    string Reason
) : IRequest<bool>;

public record PromoteEmployeeCommand(
    Guid EmployeeId,
    string ToPositionId,
    DateTime EffectiveDate,
    string Reason
) : IRequest<bool>;

public record TerminateEmployeeCommand(
    Guid EmployeeId,
    DateTime TerminationDate,
    string Reason,
    string Note
) : IRequest<bool>;

public class EmployeeCommandHandler : 
    IRequestHandler<HireEmployeeCommand, Guid>,
    IRequestHandler<TransferEmployeeCommand, bool>,
    IRequestHandler<PromoteEmployeeCommand, bool>,
    IRequestHandler<TerminateEmployeeCommand, bool>
{
    private readonly EventStoreRepository<Employee> _repo;
    private readonly IEventBus _eventBus;

    public EmployeeCommandHandler(EventStoreRepository<Employee> repo, IEventBus eventBus)
    {
        _repo = repo;
        _eventBus = eventBus;
    }

    public async Task<Guid> Handle(HireEmployeeCommand request, CancellationToken ct)
    {
        var id = Guid.NewGuid();
        var empNumber = $"EMP-{DateTime.UtcNow:yyyyMMdd}-{id.ToString()[..4]}";
        var emp = Employee.Hire(
            id, empNumber, request.FullName, request.Gender, request.DateOfBirth, 
            request.IdType, request.IdNumber, request.HireDate, request.EmploymentType, 
            request.CompanyId, request.DepartmentId, request.PositionId, 
            request.ManagerEmployeeId, request.CostCenterId);
            
        await _repo.SaveAsync(emp);

        // Publish Integration Event for Identity Account Creation
        await _eventBus.PublishAsync(new HRIntegrationEvents.EmployeeHiredIntegrationEvent(
            emp.Id,
            emp.EmployeeNumber,
            emp.FullName,
            emp.DepartmentId,
            emp.PositionId,
            request.Email
        ));

        return id;
    }

    public async Task<bool> Handle(TransferEmployeeCommand request, CancellationToken ct)
    {
        var emp = await _repo.LoadAsync(request.EmployeeId);
        if (emp == null) throw new KeyNotFoundException("Employee not found");
        emp.Transfer(request.ToDepartmentId, request.ToPositionId, request.EffectiveDate, request.Reason);
        await _repo.SaveAsync(emp);
        return true;
    }

    public async Task<bool> Handle(PromoteEmployeeCommand request, CancellationToken ct)
    {
        var emp = await _repo.LoadAsync(request.EmployeeId);
        if (emp == null) throw new KeyNotFoundException("Employee not found");
        emp.Promote(request.ToPositionId, request.EffectiveDate, request.Reason);
        await _repo.SaveAsync(emp);
        return true;
    }

    public async Task<bool> Handle(TerminateEmployeeCommand request, CancellationToken ct)
    {
        var emp = await _repo.LoadAsync(request.EmployeeId);
        if (emp == null) throw new KeyNotFoundException("Employee not found");
        emp.Terminate(request.TerminationDate, request.Reason, request.Note);
        await _repo.SaveAsync(emp);

        // Publish Integration Event for Account Disabling
        await _eventBus.PublishAsync(new HRIntegrationEvents.EmployeeTerminatedIntegrationEvent(
            emp.Id,
            emp.EmployeeNumber,
            emp.FullName
        ));

        return true;
    }
}
