using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ErpSystem.BuildingBlocks.Domain;
using ErpSystem.Payroll.Domain;
using ErpSystem.Payroll.Infrastructure;

namespace ErpSystem.Payroll.API;

[ApiController]
[Route("api/v1/payroll/salary-structures")]
public class SalaryStructuresController : ControllerBase
{
    private readonly IEventStore _eventStore;
    private readonly PayrollReadDbContext _readDb;

    public SalaryStructuresController(IEventStore eventStore, PayrollReadDbContext readDb)
    {
        _eventStore = eventStore;
        _readDb = readDb;
    }

    [HttpGet]
    public async Task<IActionResult> GetStructures([FromQuery] bool? isActive = null)
    {
        var query = _readDb.SalaryStructures.AsQueryable();
        if (isActive.HasValue)
            query = query.Where(s => s.IsActive == isActive.Value);

        var structures = await query.OrderBy(s => s.Name).ToListAsync();
        return Ok(new { items = structures, total = structures.Count });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetStructure(Guid id)
    {
        var structure = await _readDb.SalaryStructures.FindAsync(id);
        return structure == null ? NotFound() : Ok(structure);
    }

    [HttpPost]
    public async Task<IActionResult> CreateStructure([FromBody] CreateSalaryStructureRequest request)
    {
        var structure = SalaryStructure.Create(
            Guid.NewGuid(),
            request.Name,
            request.BaseSalary,
            request.Currency,
            request.Description
        );

        await _eventStore.SaveAggregateAsync(structure);
        return CreatedAtAction(nameof(GetStructure), new { id = structure.Id }, new { id = structure.Id });
    }

    [HttpPost("{id:guid}/components")]
    public async Task<IActionResult> AddComponent(Guid id, [FromBody] AddComponentRequest request)
    {
        var structure = await _eventStore.LoadAggregateAsync<SalaryStructure>(id);
        if (structure == null) return NotFound();

        structure.AddComponent(
            request.Name,
            Enum.Parse<SalaryComponentType>(request.Type),
            request.Amount,
            request.IsPercentage,
            request.IsTaxable
        );

        await _eventStore.SaveAggregateAsync(structure);
        return Ok();
    }

    [HttpPost("{id:guid}/deductions")]
    public async Task<IActionResult> AddDeduction(Guid id, [FromBody] AddDeductionRequest request)
    {
        var structure = await _eventStore.LoadAggregateAsync<SalaryStructure>(id);
        if (structure == null) return NotFound();

        structure.AddDeduction(
            request.Name,
            Enum.Parse<DeductionType>(request.Type),
            request.Amount,
            request.IsPercentage
        );

        await _eventStore.SaveAggregateAsync(structure);
        return Ok();
    }
}

[ApiController]
[Route("api/v1/payroll/payroll-runs")]
public class PayrollRunsController : ControllerBase
{
    private readonly IEventStore _eventStore;
    private readonly PayrollReadDbContext _readDb;

    public PayrollRunsController(IEventStore eventStore, PayrollReadDbContext readDb)
    {
        _eventStore = eventStore;
        _readDb = readDb;
    }

    [HttpGet]
    public async Task<IActionResult> GetPayrollRuns(
        [FromQuery] int? year = null,
        [FromQuery] int? month = null,
        [FromQuery] string? status = null)
    {
        var query = _readDb.PayrollRuns.AsQueryable();
        if (year.HasValue) query = query.Where(r => r.Year == year.Value);
        if (month.HasValue) query = query.Where(r => r.Month == month.Value);
        if (!string.IsNullOrEmpty(status)) query = query.Where(r => r.Status == status);

        var runs = await query.OrderByDescending(r => r.CreatedAt).ToListAsync();
        return Ok(new { items = runs, total = runs.Count });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetPayrollRun(Guid id)
    {
        var run = await _readDb.PayrollRuns.FindAsync(id);
        return run == null ? NotFound() : Ok(run);
    }

    [HttpPost]
    public async Task<IActionResult> CreatePayrollRun([FromBody] CreatePayrollRunRequest request)
    {
        var runNumber = $"PAY-{request.Year}{request.Month:D2}-{Guid.NewGuid().ToString()[..8].ToUpper()}";

        var run = PayrollRun.Create(
            Guid.NewGuid(),
            runNumber,
            request.Year,
            request.Month,
            request.PaymentDate,
            request.Description
        );

        await _eventStore.SaveAggregateAsync(run);
        return CreatedAtAction(nameof(GetPayrollRun), new { id = run.Id }, new { id = run.Id, runNumber });
    }

    [HttpPost("{id:guid}/payslips")]
    public async Task<IActionResult> AddPayslip(Guid id, [FromBody] AddPayslipRequest request)
    {
        var run = await _eventStore.LoadAggregateAsync<PayrollRun>(id);
        if (run == null) return NotFound();

        var payslipNumber = $"PS-{run.Year}{run.Month:D2}-{Guid.NewGuid().ToString()[..8].ToUpper()}";

        var payslipId = run.AddPayslip(
            payslipNumber,
            request.EmployeeId,
            request.EmployeeName,
            request.GrossAmount,
            request.TotalDeductions,
            new List<PayslipLine>()
        );

        await _eventStore.SaveAggregateAsync(run);
        return Ok(new { payslipId, payslipNumber });
    }

    [HttpPost("{id:guid}/start-processing")]
    public async Task<IActionResult> StartProcessing(Guid id)
    {
        var run = await _eventStore.LoadAggregateAsync<PayrollRun>(id);
        if (run == null) return NotFound();

        run.StartProcessing();
        await _eventStore.SaveAggregateAsync(run);
        return Ok(new { id, status = "Processing" });
    }

    [HttpPost("{id:guid}/submit")]
    public async Task<IActionResult> Submit(Guid id)
    {
        var run = await _eventStore.LoadAggregateAsync<PayrollRun>(id);
        if (run == null) return NotFound();

        run.SubmitForApproval();
        await _eventStore.SaveAggregateAsync(run);
        return Ok(new { id, status = "PendingApproval" });
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, [FromBody] ApprovePayrollRequest request)
    {
        var run = await _eventStore.LoadAggregateAsync<PayrollRun>(id);
        if (run == null) return NotFound();

        run.Approve(request.ApprovedByUserId);
        await _eventStore.SaveAggregateAsync(run);
        return Ok(new { id, status = "Approved" });
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] CancelPayrollRequest request)
    {
        var run = await _eventStore.LoadAggregateAsync<PayrollRun>(id);
        if (run == null) return NotFound();

        run.Cancel(request.Reason);
        await _eventStore.SaveAggregateAsync(run);
        return Ok(new { id, status = "Cancelled" });
    }

    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics([FromQuery] int year)
    {
        var runs = await _readDb.PayrollRuns.Where(r => r.Year == year).ToListAsync();
        
        return Ok(new
        {
            year,
            totalRuns = runs.Count,
            paidRuns = runs.Count(r => r.Status == "Paid"),
            totalAmount = runs.Sum(r => r.TotalNetAmount),
            byMonth = runs.GroupBy(r => r.Month).Select(g => new
            {
                month = g.Key,
                amount = g.Sum(r => r.TotalNetAmount),
                employees = g.Sum(r => r.EmployeeCount)
            }).OrderBy(x => x.month)
        });
    }
}

[ApiController]
[Route("api/v1/payroll/payslips")]
public class PayslipsController : ControllerBase
{
    private readonly PayrollReadDbContext _readDb;

    public PayslipsController(PayrollReadDbContext readDb) => _readDb = readDb;

    [HttpGet]
    public async Task<IActionResult> GetPayslips(
        [FromQuery] Guid? payrollRunId = null,
        [FromQuery] string? employeeId = null,
        [FromQuery] int? year = null,
        [FromQuery] int? month = null)
    {
        var query = _readDb.Payslips.AsQueryable();
        if (payrollRunId.HasValue) query = query.Where(p => p.PayrollRunId == payrollRunId.Value);
        if (!string.IsNullOrEmpty(employeeId)) query = query.Where(p => p.EmployeeId == employeeId);
        if (year.HasValue) query = query.Where(p => p.Year == year.Value);
        if (month.HasValue) query = query.Where(p => p.Month == month.Value);

        var payslips = await query.OrderByDescending(p => p.Year).ThenByDescending(p => p.Month).ToListAsync();
        return Ok(new { items = payslips, total = payslips.Count });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetPayslip(Guid id)
    {
        var payslip = await _readDb.Payslips.FindAsync(id);
        return payslip == null ? NotFound() : Ok(payslip);
    }

    [HttpGet("employee/{employeeId}")]
    public async Task<IActionResult> GetEmployeePayslips(string employeeId, [FromQuery] int? year = null)
    {
        var query = _readDb.Payslips.Where(p => p.EmployeeId == employeeId);
        if (year.HasValue) query = query.Where(p => p.Year == year.Value);

        var payslips = await query.OrderByDescending(p => p.Year).ThenByDescending(p => p.Month).ToListAsync();
        
        var summary = new
        {
            employeeId,
            totalPayslips = payslips.Count,
            totalEarnings = payslips.Sum(p => p.NetAmount),
            avgMonthlyNet = payslips.Any() ? payslips.Average(p => p.NetAmount) : 0,
            payslips
        };
        
        return Ok(summary);
    }
}

#region Request DTOs

public record CreateSalaryStructureRequest(string Name, decimal BaseSalary, string Currency, string? Description);
public record AddComponentRequest(string Name, string Type, decimal Amount, bool IsPercentage, bool IsTaxable);
public record AddDeductionRequest(string Name, string Type, decimal Amount, bool IsPercentage);
public record CreatePayrollRunRequest(int Year, int Month, DateTime PaymentDate, string? Description);
public record AddPayslipRequest(string EmployeeId, string EmployeeName, decimal GrossAmount, decimal TotalDeductions);
public record ApprovePayrollRequest(string ApprovedByUserId);
public record CancelPayrollRequest(string Reason);

#endregion
