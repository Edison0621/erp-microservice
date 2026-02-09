using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ErpSystem.BuildingBlocks.Domain;
using ErpSystem.Payroll.Domain;
using ErpSystem.Payroll.Infrastructure;

namespace ErpSystem.Payroll.API;

[ApiController]
[Route("api/v1/payroll/salary-structures")]
public class SalaryStructuresController(IEventStore eventStore, PayrollReadDbContext readDb) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetStructures([FromQuery] bool? isActive = null)
    {
        IQueryable<SalaryStructureReadModel> query = readDb.SalaryStructures.AsQueryable();
        if (isActive.HasValue)
            query = query.Where(s => s.IsActive == isActive.Value);

        List<SalaryStructureReadModel> structures = await query.OrderBy(s => s.Name).ToListAsync();
        return this.Ok(new { items = structures, total = structures.Count });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetStructure(Guid id)
    {
        SalaryStructureReadModel? structure = await readDb.SalaryStructures.FindAsync(id);
        return structure == null ? this.NotFound() : this.Ok(structure);
    }

    [HttpPost]
    public async Task<IActionResult> CreateStructure([FromBody] CreateSalaryStructureRequest request)
    {
        SalaryStructure structure = SalaryStructure.Create(
            Guid.NewGuid(),
            request.Name,
            request.BaseSalary,
            request.Currency,
            request.Description
        );

        await eventStore.SaveAggregateAsync(structure);
        return this.CreatedAtAction(nameof(this.GetStructure), new { id = structure.Id }, new { id = structure.Id });
    }

    [HttpPost("{id:guid}/components")]
    public async Task<IActionResult> AddComponent(Guid id, [FromBody] AddComponentRequest request)
    {
        SalaryStructure? structure = await eventStore.LoadAggregateAsync<SalaryStructure>(id);
        if (structure == null) return this.NotFound();

        structure.AddComponent(
            request.Name,
            Enum.Parse<SalaryComponentType>(request.Type),
            request.Amount,
            request.IsPercentage,
            request.IsTaxable
        );

        await eventStore.SaveAggregateAsync(structure);
        return this.Ok();
    }

    [HttpPost("{id:guid}/deductions")]
    public async Task<IActionResult> AddDeduction(Guid id, [FromBody] AddDeductionRequest request)
    {
        SalaryStructure? structure = await eventStore.LoadAggregateAsync<SalaryStructure>(id);
        if (structure == null) return this.NotFound();

        structure.AddDeduction(
            request.Name,
            Enum.Parse<DeductionType>(request.Type),
            request.Amount,
            request.IsPercentage
        );

        await eventStore.SaveAggregateAsync(structure);
        return this.Ok();
    }
}

[ApiController]
[Route("api/v1/payroll/payroll-runs")]
public class PayrollRunsController(IEventStore eventStore, PayrollReadDbContext readDb) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetPayrollRuns(
        [FromQuery] int? year = null,
        [FromQuery] int? month = null,
        [FromQuery] string? status = null)
    {
        IQueryable<PayrollRunReadModel> query = readDb.PayrollRuns.AsQueryable();
        if (year.HasValue) query = query.Where(r => r.Year == year.Value);
        if (month.HasValue) query = query.Where(r => r.Month == month.Value);
        if (!string.IsNullOrEmpty(status)) query = query.Where(r => r.Status == status);

        List<PayrollRunReadModel> runs = await query.OrderByDescending(r => r.CreatedAt).ToListAsync();
        return this.Ok(new { items = runs, total = runs.Count });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetPayrollRun(Guid id)
    {
        PayrollRunReadModel? run = await readDb.PayrollRuns.FindAsync(id);
        return run == null ? this.NotFound() : this.Ok(run);
    }

    [HttpPost]
    public async Task<IActionResult> CreatePayrollRun([FromBody] CreatePayrollRunRequest request)
    {
        string runNumber = $"PAY-{request.Year}{request.Month:D2}-{Guid.NewGuid().ToString()[..8].ToUpper()}";

        PayrollRun run = PayrollRun.Create(
            Guid.NewGuid(),
            runNumber,
            request.Year,
            request.Month,
            request.PaymentDate,
            request.Description
        );

        await eventStore.SaveAggregateAsync(run);
        return this.CreatedAtAction(nameof(this.GetPayrollRun), new { id = run.Id }, new { id = run.Id, runNumber });
    }

    [HttpPost("{id:guid}/payslips")]
    public async Task<IActionResult> AddPayslip(Guid id, [FromBody] AddPayslipRequest request)
    {
        PayrollRun? run = await eventStore.LoadAggregateAsync<PayrollRun>(id);
        if (run == null) return this.NotFound();

        string payslipNumber = $"PS-{run.Year}{run.Month:D2}-{Guid.NewGuid().ToString()[..8].ToUpper()}";

        Guid payslipId = run.AddPayslip(
            payslipNumber,
            request.EmployeeId,
            request.EmployeeName,
            request.GrossAmount,
            request.TotalDeductions,
            []
        );

        await eventStore.SaveAggregateAsync(run);
        return this.Ok(new { payslipId, payslipNumber });
    }

    [HttpPost("{id:guid}/start-processing")]
    public async Task<IActionResult> StartProcessing(Guid id)
    {
        PayrollRun? run = await eventStore.LoadAggregateAsync<PayrollRun>(id);
        if (run == null) return this.NotFound();

        run.StartProcessing();
        await eventStore.SaveAggregateAsync(run);
        return this.Ok(new { id, status = "Processing" });
    }

    [HttpPost("{id:guid}/submit")]
    public async Task<IActionResult> Submit(Guid id)
    {
        PayrollRun? run = await eventStore.LoadAggregateAsync<PayrollRun>(id);
        if (run == null) return this.NotFound();

        run.SubmitForApproval();
        await eventStore.SaveAggregateAsync(run);
        return this.Ok(new { id, status = "PendingApproval" });
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, [FromBody] ApprovePayrollRequest request)
    {
        PayrollRun? run = await eventStore.LoadAggregateAsync<PayrollRun>(id);
        if (run == null) return this.NotFound();

        run.Approve(request.ApprovedByUserId);
        await eventStore.SaveAggregateAsync(run);
        return this.Ok(new { id, status = "Approved" });
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] CancelPayrollRequest request)
    {
        PayrollRun? run = await eventStore.LoadAggregateAsync<PayrollRun>(id);
        if (run == null) return this.NotFound();

        run.Cancel(request.Reason);
        await eventStore.SaveAggregateAsync(run);
        return this.Ok(new { id, status = "Cancelled" });
    }

    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics([FromQuery] int year)
    {
        List<PayrollRunReadModel> runs = await readDb.PayrollRuns.Where(r => r.Year == year).ToListAsync();
        
        return this.Ok(new
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
public class PayslipsController(PayrollReadDbContext readDb) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetPayslips(
        [FromQuery] Guid? payrollRunId = null,
        [FromQuery] string? employeeId = null,
        [FromQuery] int? year = null,
        [FromQuery] int? month = null)
    {
        IQueryable<PayslipReadModel> query = readDb.Payslips.AsQueryable();
        if (payrollRunId.HasValue) query = query.Where(p => p.PayrollRunId == payrollRunId.Value);
        if (!string.IsNullOrEmpty(employeeId)) query = query.Where(p => p.EmployeeId == employeeId);
        if (year.HasValue) query = query.Where(p => p.Year == year.Value);
        if (month.HasValue) query = query.Where(p => p.Month == month.Value);

        List<PayslipReadModel> payslips = await query.OrderByDescending(p => p.Year).ThenByDescending(p => p.Month).ToListAsync();
        return this.Ok(new { items = payslips, total = payslips.Count });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetPayslip(Guid id)
    {
        PayslipReadModel? payslip = await readDb.Payslips.FindAsync(id);
        return payslip == null ? this.NotFound() : this.Ok(payslip);
    }

    [HttpGet("employee/{employeeId}")]
    public async Task<IActionResult> GetEmployeePayslips(string employeeId, [FromQuery] int? year = null)
    {
        IQueryable<PayslipReadModel> query = readDb.Payslips.Where(p => p.EmployeeId == employeeId);
        if (year.HasValue) query = query.Where(p => p.Year == year.Value);

        List<PayslipReadModel> payslips = await query.OrderByDescending(p => p.Year).ThenByDescending(p => p.Month).ToListAsync();
        
        var summary = new
        {
            employeeId,
            totalPayslips = payslips.Count,
            totalEarnings = payslips.Sum(p => p.NetAmount),
            avgMonthlyNet = payslips.Any() ? payslips.Average(p => p.NetAmount) : 0,
            payslips
        };
        
        return this.Ok(summary);
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
