using ErpSystem.BuildingBlocks.Domain;

namespace ErpSystem.Payroll.Domain;

#region Enums

public enum SalaryComponentType
{
    Basic = 0,
    HRA = 1,           // 住房补贴
    Transport = 2,     // 交通补贴
    Meal = 3,          // 餐补
    Performance = 4,   // 绩效奖金
    Overtime = 5,      // 加班费
    Commission = 6,    // 销售提成
    Other = 7
}

public enum DeductionType
{
    IncomeTax = 0,          // 个税
    SocialSecurity = 1,      // 社保
    HousingFund = 2,         // 公积金
    MedicalInsurance = 3,    // 医保
    UnemploymentInsurance = 4, // 失业保险
    Loan = 5,                // 贷款扣除
    Other = 6
}

public enum PayrollRunStatus
{
    Draft = 0,
    Processing = 1,
    PendingApproval = 2,
    Approved = 3,
    Paid = 4,
    Cancelled = 5
}

public enum PayslipStatus
{
    Draft = 0,
    Finalized = 1,
    Paid = 2,
    Corrected = 3
}

#endregion

#region Domain Events

public record SalaryStructureCreatedEvent(
    Guid StructureId,
    string Name,
    string Description,
    decimal BaseSalary,
    string Currency
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record SalaryComponentAddedEvent(
    Guid StructureId,
    Guid ComponentId,
    string Name,
    SalaryComponentType Type,
    decimal Amount,
    bool IsPercentage,
    bool IsTaxable
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record DeductionAddedEvent(
    Guid StructureId,
    Guid DeductionId,
    string Name,
    DeductionType Type,
    decimal Amount,
    bool IsPercentage
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record PayrollRunCreatedEvent(
    Guid PayrollRunId,
    string RunNumber,
    int Year,
    int Month,
    DateTime PaymentDate,
    string Description
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record PayrollRunStatusChangedEvent(
    Guid PayrollRunId,
    PayrollRunStatus OldStatus,
    PayrollRunStatus NewStatus,
    string? Reason
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record PayslipGeneratedEvent(
    Guid PayrollRunId,
    Guid PayslipId,
    string PayslipNumber,
    string EmployeeId,
    string EmployeeName,
    decimal GrossAmount,
    decimal TotalDeductions,
    decimal NetAmount
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record PayslipPaidEvent(
    Guid PayrollRunId,
    Guid PayslipId,
    DateTime PaidAt,
    string PaymentMethod,
    string? TransactionRef
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record PayrollApprovedEvent(
    Guid PayrollRunId,
    string ApprovedByUserId,
    DateTime ApprovedAt,
    decimal TotalAmount,
    int EmployeeCount
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

#endregion

#region Value Objects

public record SalaryComponent(
    Guid Id,
    string Name,
    SalaryComponentType Type,
    decimal Amount,
    bool IsPercentage,
    bool IsTaxable
);

public record Deduction(
    Guid Id,
    string Name,
    DeductionType Type,
    decimal Amount,
    bool IsPercentage
);

public record PayslipLine(
    string Description,
    decimal Amount,
    bool IsDeduction
);

#endregion

#region Salary Structure Aggregate

public class SalaryStructure : AggregateRoot<Guid>
{
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public decimal BaseSalary { get; private set; }
    public string Currency { get; private set; } = "CNY";
    public bool IsActive { get; private set; } = true;
    public List<SalaryComponent> Components { get; private set; } = new();
    public List<Deduction> Deductions { get; private set; } = new();
    public DateTime CreatedAt { get; private set; }

    public decimal TotalEarnings => BaseSalary + Components
        .Where(c => !c.IsPercentage)
        .Sum(c => c.Amount) + Components
        .Where(c => c.IsPercentage)
        .Sum(c => BaseSalary * c.Amount / 100);

    public static SalaryStructure Create(
        Guid id,
        string name,
        decimal baseSalary,
        string currency,
        string? description = null)
    {
        var structure = new SalaryStructure();
        structure.ApplyChange(new SalaryStructureCreatedEvent(id, name, description ?? "", baseSalary, currency));
        return structure;
    }

    public void AddComponent(string name, SalaryComponentType type, decimal amount, bool isPercentage, bool isTaxable)
    {
        var componentId = Guid.NewGuid();
        ApplyChange(new SalaryComponentAddedEvent(Id, componentId, name, type, amount, isPercentage, isTaxable));
    }

    public void AddDeduction(string name, DeductionType type, decimal amount, bool isPercentage)
    {
        var deductionId = Guid.NewGuid();
        ApplyChange(new DeductionAddedEvent(Id, deductionId, name, type, amount, isPercentage));
    }

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case SalaryStructureCreatedEvent e:
                Id = e.StructureId;
                Name = e.Name;
                Description = e.Description;
                BaseSalary = e.BaseSalary;
                Currency = e.Currency;
                CreatedAt = e.OccurredOn;
                break;

            case SalaryComponentAddedEvent e:
                Components.Add(new SalaryComponent(e.ComponentId, e.Name, e.Type, e.Amount, e.IsPercentage, e.IsTaxable));
                break;

            case DeductionAddedEvent e:
                Deductions.Add(new Deduction(e.DeductionId, e.Name, e.Type, e.Amount, e.IsPercentage));
                break;
        }
    }
}

#endregion

#region Payroll Run Aggregate

public class PayrollRun : AggregateRoot<Guid>
{
    public string RunNumber { get; private set; } = string.Empty;
    public int Year { get; private set; }
    public int Month { get; private set; }
    public DateTime PaymentDate { get; private set; }
    public string? Description { get; private set; }
    public PayrollRunStatus Status { get; private set; }
    public List<Payslip> Payslips { get; private set; } = new();
    public DateTime CreatedAt { get; private set; }
    public DateTime? ApprovedAt { get; private set; }
    public string? ApprovedByUserId { get; private set; }

    public decimal TotalGrossAmount => Payslips.Sum(p => p.GrossAmount);
    public decimal TotalDeductions => Payslips.Sum(p => p.TotalDeductions);
    public decimal TotalNetAmount => Payslips.Sum(p => p.NetAmount);
    public int EmployeeCount => Payslips.Count;
    public int PaidCount => Payslips.Count(p => p.Status == PayslipStatus.Paid);

    public static PayrollRun Create(
        Guid id,
        string runNumber,
        int year,
        int month,
        DateTime paymentDate,
        string? description = null)
    {
        var run = new PayrollRun();
        run.ApplyChange(new PayrollRunCreatedEvent(id, runNumber, year, month, paymentDate, description ?? ""));
        return run;
    }

    public Guid AddPayslip(
        string payslipNumber,
        string employeeId,
        string employeeName,
        decimal grossAmount,
        decimal totalDeductions,
        List<PayslipLine> lines)
    {
        if (Status != PayrollRunStatus.Draft && Status != PayrollRunStatus.Processing)
            throw new InvalidOperationException("Cannot add payslips to non-draft payroll run");

        var netAmount = grossAmount - totalDeductions;
        var payslipId = Guid.NewGuid();
        ApplyChange(new PayslipGeneratedEvent(Id, payslipId, payslipNumber, employeeId, employeeName, grossAmount, totalDeductions, netAmount));
        return payslipId;
    }

    public void StartProcessing()
    {
        if (Status != PayrollRunStatus.Draft)
            throw new InvalidOperationException("Can only start processing from draft status");

        ApplyChange(new PayrollRunStatusChangedEvent(Id, Status, PayrollRunStatus.Processing, null));
    }

    public void SubmitForApproval()
    {
        if (Status != PayrollRunStatus.Processing)
            throw new InvalidOperationException("Can only submit for approval from processing status");

        if (Payslips.Count == 0)
            throw new InvalidOperationException("Cannot submit empty payroll run");

        ApplyChange(new PayrollRunStatusChangedEvent(Id, Status, PayrollRunStatus.PendingApproval, null));
    }

    public void Approve(string approvedByUserId)
    {
        if (Status != PayrollRunStatus.PendingApproval)
            throw new InvalidOperationException("Can only approve from pending approval status");

        ApplyChange(new PayrollApprovedEvent(Id, approvedByUserId, DateTime.UtcNow, TotalNetAmount, EmployeeCount));
    }

    public void MarkPayslipAsPaid(Guid payslipId, string paymentMethod, string? transactionRef)
    {
        if (Status != PayrollRunStatus.Approved && Status != PayrollRunStatus.Paid)
            throw new InvalidOperationException("Can only mark payslips as paid after approval");

        var payslip = Payslips.FirstOrDefault(p => p.Id == payslipId);
        if (payslip == null)
            throw new InvalidOperationException($"Payslip {payslipId} not found");

        ApplyChange(new PayslipPaidEvent(Id, payslipId, DateTime.UtcNow, paymentMethod, transactionRef));
    }

    public void Cancel(string reason)
    {
        if (Status == PayrollRunStatus.Paid)
            throw new InvalidOperationException("Cannot cancel paid payroll run");

        ApplyChange(new PayrollRunStatusChangedEvent(Id, Status, PayrollRunStatus.Cancelled, reason));
    }

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case PayrollRunCreatedEvent e:
                Id = e.PayrollRunId;
                RunNumber = e.RunNumber;
                Year = e.Year;
                Month = e.Month;
                PaymentDate = e.PaymentDate;
                Description = e.Description;
                Status = PayrollRunStatus.Draft;
                CreatedAt = e.OccurredOn;
                break;

            case PayrollRunStatusChangedEvent e:
                Status = e.NewStatus;
                break;

            case PayslipGeneratedEvent e:
                Payslips.Add(new Payslip
                {
                    Id = e.PayslipId,
                    PayslipNumber = e.PayslipNumber,
                    EmployeeId = e.EmployeeId,
                    EmployeeName = e.EmployeeName,
                    GrossAmount = e.GrossAmount,
                    TotalDeductions = e.TotalDeductions,
                    NetAmount = e.NetAmount,
                    Status = PayslipStatus.Draft
                });
                break;

            case PayrollApprovedEvent e:
                Status = PayrollRunStatus.Approved;
                ApprovedAt = e.ApprovedAt;
                ApprovedByUserId = e.ApprovedByUserId;
                foreach (var payslip in Payslips)
                    payslip.Status = PayslipStatus.Finalized;
                break;

            case PayslipPaidEvent e:
                var paidPayslip = Payslips.FirstOrDefault(p => p.Id == e.PayslipId);
                if (paidPayslip != null)
                {
                    paidPayslip.Status = PayslipStatus.Paid;
                    paidPayslip.PaidAt = e.PaidAt;
                    paidPayslip.PaymentMethod = e.PaymentMethod;
                    paidPayslip.TransactionRef = e.TransactionRef;
                }
                if (Payslips.All(p => p.Status == PayslipStatus.Paid))
                    Status = PayrollRunStatus.Paid;
                break;
        }
    }
}

#endregion

#region Payslip Entity

public class Payslip
{
    public Guid Id { get; set; }
    public string PayslipNumber { get; set; } = string.Empty;
    public string EmployeeId { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public decimal GrossAmount { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal NetAmount { get; set; }
    public PayslipStatus Status { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? PaymentMethod { get; set; }
    public string? TransactionRef { get; set; }
    public List<PayslipLine> Lines { get; set; } = new();
}

#endregion
