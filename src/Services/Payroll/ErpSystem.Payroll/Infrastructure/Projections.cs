using MediatR;
using System.Text.Json;
using ErpSystem.Payroll.Domain;

namespace ErpSystem.Payroll.Infrastructure;

#region Salary Structure Projections

public class SalaryStructureProjectionHandler :
    INotificationHandler<SalaryStructureCreatedEvent>,
    INotificationHandler<SalaryComponentAddedEvent>,
    INotificationHandler<DeductionAddedEvent>
{
    private readonly PayrollReadDbContext _db;

    public SalaryStructureProjectionHandler(PayrollReadDbContext db) => _db = db;

    public async Task Handle(SalaryStructureCreatedEvent e, CancellationToken ct)
    {
        var structure = new SalaryStructureReadModel
        {
            Id = e.StructureId,
            Name = e.Name,
            Description = e.Description,
            BaseSalary = e.BaseSalary,
            Currency = e.Currency,
            TotalEarnings = e.BaseSalary,
            IsActive = true,
            Components = "[]",
            Deductions = "[]",
            CreatedAt = e.OccurredOn
        };
        _db.SalaryStructures.Add(structure);
        await _db.SaveChangesAsync(ct);
    }

    public async Task Handle(SalaryComponentAddedEvent e, CancellationToken ct)
    {
        var structure = await _db.SalaryStructures.FindAsync([e.StructureId], ct);
        if (structure != null)
        {
            var components = JsonSerializer.Deserialize<List<object>>(structure.Components) ?? new();
            components.Add(new { e.ComponentId, e.Name, Type = e.Type.ToString(), e.Amount, e.IsPercentage, e.IsTaxable });
            structure.Components = JsonSerializer.Serialize(components);
            structure.ComponentCount++;
            
            // Recalculate total earnings
            var additionalAmount = e.IsPercentage ? structure.BaseSalary * e.Amount / 100 : e.Amount;
            structure.TotalEarnings += additionalAmount;
            
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task Handle(DeductionAddedEvent e, CancellationToken ct)
    {
        var structure = await _db.SalaryStructures.FindAsync([e.StructureId], ct);
        if (structure != null)
        {
            var deductions = JsonSerializer.Deserialize<List<object>>(structure.Deductions) ?? new();
            deductions.Add(new { e.DeductionId, e.Name, Type = e.Type.ToString(), e.Amount, e.IsPercentage });
            structure.Deductions = JsonSerializer.Serialize(deductions);
            structure.DeductionCount++;
            await _db.SaveChangesAsync(ct);
        }
    }
}

#endregion

#region Payroll Run Projections

public class PayrollRunProjectionHandler :
    INotificationHandler<PayrollRunCreatedEvent>,
    INotificationHandler<PayrollRunStatusChangedEvent>,
    INotificationHandler<PayslipGeneratedEvent>,
    INotificationHandler<PayrollApprovedEvent>,
    INotificationHandler<PayslipPaidEvent>
{
    private readonly PayrollReadDbContext _db;

    public PayrollRunProjectionHandler(PayrollReadDbContext db) => _db = db;

    public async Task Handle(PayrollRunCreatedEvent e, CancellationToken ct)
    {
        var run = new PayrollRunReadModel
        {
            Id = e.PayrollRunId,
            RunNumber = e.RunNumber,
            Year = e.Year,
            Month = e.Month,
            PaymentDate = e.PaymentDate,
            Description = e.Description,
            Status = PayrollRunStatus.Draft.ToString(),
            CreatedAt = e.OccurredOn
        };
        _db.PayrollRuns.Add(run);
        await _db.SaveChangesAsync(ct);
    }

    public async Task Handle(PayrollRunStatusChangedEvent e, CancellationToken ct)
    {
        var run = await _db.PayrollRuns.FindAsync([e.PayrollRunId], ct);
        if (run != null)
        {
            run.Status = e.NewStatus.ToString();
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task Handle(PayslipGeneratedEvent e, CancellationToken ct)
    {
        var run = await _db.PayrollRuns.FindAsync([e.PayrollRunId], ct);
        
        var payslip = new PayslipReadModel
        {
            Id = e.PayslipId,
            PayrollRunId = e.PayrollRunId,
            PayslipNumber = e.PayslipNumber,
            EmployeeId = e.EmployeeId,
            EmployeeName = e.EmployeeName,
            Year = run?.Year ?? DateTime.UtcNow.Year,
            Month = run?.Month ?? DateTime.UtcNow.Month,
            GrossAmount = e.GrossAmount,
            TotalDeductions = e.TotalDeductions,
            NetAmount = e.NetAmount,
            Status = PayslipStatus.Draft.ToString(),
            Lines = "[]"
        };
        _db.Payslips.Add(payslip);

        if (run != null)
        {
            run.EmployeeCount++;
            run.TotalGrossAmount += e.GrossAmount;
            run.TotalDeductions += e.TotalDeductions;
            run.TotalNetAmount += e.NetAmount;
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task Handle(PayrollApprovedEvent e, CancellationToken ct)
    {
        var run = await _db.PayrollRuns.FindAsync([e.PayrollRunId], ct);
        if (run != null)
        {
            run.Status = PayrollRunStatus.Approved.ToString();
            run.ApprovedAt = e.ApprovedAt;
            run.ApprovedByUserId = e.ApprovedByUserId;
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task Handle(PayslipPaidEvent e, CancellationToken ct)
    {
        var payslip = await _db.Payslips.FindAsync([e.PayslipId], ct);
        if (payslip != null)
        {
            payslip.Status = PayslipStatus.Paid.ToString();
            payslip.PaidAt = e.PaidAt;
            payslip.PaymentMethod = e.PaymentMethod;
            payslip.TransactionRef = e.TransactionRef;
        }

        var run = await _db.PayrollRuns.FindAsync([e.PayrollRunId], ct);
        if (run != null)
        {
            run.PaidCount++;
            if (run.PaidCount >= run.EmployeeCount)
                run.Status = PayrollRunStatus.Paid.ToString();
        }

        await _db.SaveChangesAsync(ct);
    }
}

#endregion
