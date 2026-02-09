using MediatR;
using System.Text.Json;
using ErpSystem.Payroll.Domain;

namespace ErpSystem.Payroll.Infrastructure;

#region Salary Structure Projections

public class SalaryStructureProjectionHandler(PayrollReadDbContext db) :
    INotificationHandler<SalaryStructureCreatedEvent>,
    INotificationHandler<SalaryComponentAddedEvent>,
    INotificationHandler<DeductionAddedEvent>
{
    public async Task Handle(SalaryStructureCreatedEvent e, CancellationToken ct)
    {
        SalaryStructureReadModel structure = new SalaryStructureReadModel
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
        db.SalaryStructures.Add(structure);
        await db.SaveChangesAsync(ct);
    }

    public async Task Handle(SalaryComponentAddedEvent e, CancellationToken ct)
    {
        SalaryStructureReadModel? structure = await db.SalaryStructures.FindAsync([e.StructureId], ct);
        if (structure != null)
        {
            List<object> components = JsonSerializer.Deserialize<List<object>>(structure.Components) ?? [];
            components.Add(new { e.ComponentId, e.Name, Type = e.Type.ToString(), e.Amount, e.IsPercentage, e.IsTaxable });
            structure.Components = JsonSerializer.Serialize(components);
            structure.ComponentCount++;
            
            // Recalculate total earnings
            decimal additionalAmount = e.IsPercentage ? structure.BaseSalary * e.Amount / 100 : e.Amount;
            structure.TotalEarnings += additionalAmount;
            
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task Handle(DeductionAddedEvent e, CancellationToken ct)
    {
        SalaryStructureReadModel? structure = await db.SalaryStructures.FindAsync([e.StructureId], ct);
        if (structure != null)
        {
            List<object> deductions = JsonSerializer.Deserialize<List<object>>(structure.Deductions) ?? [];
            deductions.Add(new { e.DeductionId, e.Name, Type = e.Type.ToString(), e.Amount, e.IsPercentage });
            structure.Deductions = JsonSerializer.Serialize(deductions);
            structure.DeductionCount++;
            await db.SaveChangesAsync(ct);
        }
    }
}

#endregion

#region Payroll Run Projections

public class PayrollRunProjectionHandler(PayrollReadDbContext db) :
    INotificationHandler<PayrollRunCreatedEvent>,
    INotificationHandler<PayrollRunStatusChangedEvent>,
    INotificationHandler<PayslipGeneratedEvent>,
    INotificationHandler<PayrollApprovedEvent>,
    INotificationHandler<PayslipPaidEvent>
{
    public async Task Handle(PayrollRunCreatedEvent e, CancellationToken ct)
    {
        PayrollRunReadModel run = new PayrollRunReadModel
        {
            Id = e.PayrollRunId,
            RunNumber = e.RunNumber,
            Year = e.Year,
            Month = e.Month,
            PaymentDate = e.PaymentDate,
            Description = e.Description,
            Status = nameof(PayrollRunStatus.Draft),
            CreatedAt = e.OccurredOn
        };
        db.PayrollRuns.Add(run);
        await db.SaveChangesAsync(ct);
    }

    public async Task Handle(PayrollRunStatusChangedEvent e, CancellationToken ct)
    {
        PayrollRunReadModel? run = await db.PayrollRuns.FindAsync([e.PayrollRunId], ct);
        if (run != null)
        {
            run.Status = e.NewStatus.ToString();
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task Handle(PayslipGeneratedEvent e, CancellationToken ct)
    {
        PayrollRunReadModel? run = await db.PayrollRuns.FindAsync([e.PayrollRunId], ct);
        
        PayslipReadModel payslip = new PayslipReadModel
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
            Status = nameof(PayslipStatus.Draft),
            Lines = "[]"
        };
        db.Payslips.Add(payslip);

        if (run != null)
        {
            run.EmployeeCount++;
            run.TotalGrossAmount += e.GrossAmount;
            run.TotalDeductions += e.TotalDeductions;
            run.TotalNetAmount += e.NetAmount;
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task Handle(PayrollApprovedEvent e, CancellationToken ct)
    {
        PayrollRunReadModel? run = await db.PayrollRuns.FindAsync([e.PayrollRunId], ct);
        if (run != null)
        {
            run.Status = nameof(PayrollRunStatus.Approved);
            run.ApprovedAt = e.ApprovedAt;
            run.ApprovedByUserId = e.ApprovedByUserId;
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task Handle(PayslipPaidEvent e, CancellationToken ct)
    {
        PayslipReadModel? payslip = await db.Payslips.FindAsync([e.PayslipId], ct);
        if (payslip != null)
        {
            payslip.Status = nameof(PayslipStatus.Paid);
            payslip.PaidAt = e.PaidAt;
            payslip.PaymentMethod = e.PaymentMethod;
            payslip.TransactionRef = e.TransactionRef;
        }

        PayrollRunReadModel? run = await db.PayrollRuns.FindAsync([e.PayrollRunId], ct);
        if (run != null)
        {
            run.PaidCount++;
            if (run.PaidCount >= run.EmployeeCount)
                run.Status = nameof(PayrollRunStatus.Paid);
        }

        await db.SaveChangesAsync(ct);
    }
}

#endregion
