using MediatR;
using ErpSystem.HR.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ErpSystem.HR.Application;

public record GetEmployeeByIdQuery(Guid Id) : IRequest<EmployeeReadModel?>;
public record SearchEmployeesQuery(string? FullName, string? DepartmentId, string? Status, int Page = 1, int PageSize = 20) : IRequest<List<EmployeeReadModel>>;
public record GetEmployeeEventsQuery(Guid EmployeeId) : IRequest<List<EmployeeEventReadModel>>;

public class EmployeeQueryHandler : 
    IRequestHandler<GetEmployeeByIdQuery, EmployeeReadModel?>,
    IRequestHandler<SearchEmployeesQuery, List<EmployeeReadModel>>,
    IRequestHandler<GetEmployeeEventsQuery, List<EmployeeEventReadModel>>
{
    private readonly HRReadDbContext _readDb;

    public EmployeeQueryHandler(HRReadDbContext readDb)
    {
        _readDb = readDb;
    }

    public async Task<EmployeeReadModel?> Handle(GetEmployeeByIdQuery request, CancellationToken ct)
    {
        return await _readDb.Employees.FindAsync(new object[] { request.Id }, ct);
    }

    public async Task<List<EmployeeReadModel>> Handle(SearchEmployeesQuery request, CancellationToken ct)
    {
        var query = _readDb.Employees.AsNoTracking().AsQueryable();
        if (!string.IsNullOrEmpty(request.FullName)) query = query.Where(x => EF.Functions.Like(x.FullName, $"%{request.FullName}%"));
        if (!string.IsNullOrEmpty(request.DepartmentId)) query = query.Where(x => x.DepartmentId == request.DepartmentId);
        if (!string.IsNullOrEmpty(request.Status)) query = query.Where(x => x.Status == request.Status);
        
        return await query.OrderBy(x => x.EmployeeNumber)
                          .Skip((request.Page - 1) * request.PageSize)
                          .Take(request.PageSize)
                          .ToListAsync(ct);
    }

    public async Task<List<EmployeeEventReadModel>> Handle(GetEmployeeEventsQuery request, CancellationToken ct)
    {
        return await _readDb.EmployeeEvents.AsNoTracking()
                          .Where(x => x.EmployeeId == request.EmployeeId)
                          .OrderByDescending(x => x.OccurredAt)
                          .ToListAsync(ct);
    }
}
