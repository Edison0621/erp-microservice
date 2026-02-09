using ErpSystem.BuildingBlocks.Domain;

namespace ErpSystem.Assets.Domain;

#region Enums

public enum AssetType
{
    FixedAsset = 0,      // 固定资产
    Equipment = 1,       // 设备
    Vehicle = 2,         // 车辆
    Furniture = 3,       // 办公家具

    // ReSharper disable once InconsistentNaming
    IT = 4,              // IT设备
    Building = 5,        // 建筑物
    Land = 6,            // 土地
    Software = 7,        // 软件
    Other = 8
}

public enum AssetStatus
{
    Draft = 0,
    Active = 1,
    InMaintenance = 2,
    Disposed = 3,
    Lost = 4,
    Transferred = 5
}

public enum DepreciationMethod
{
    StraightLine = 0,        // 直线法
    DecliningBalance = 1,    // 余额递减法
    DoubleDeclining = 2,     // 双倍余额递减法
    UnitsOfProduction = 3,   // 产量法
    None = 4                 // 不计提折旧
}

public enum MaintenanceType
{
    Preventive = 0,      // 预防性维护
    Corrective = 1,      // 纠正性维护
    Emergency = 2,       // 紧急维护
    Inspection = 3,      // 检查
    Calibration = 4      // 校准
}

#endregion

#region Domain Events

public record AssetRegisteredEvent(
    Guid AssetId,
    string AssetNumber,
    string Name,
    AssetType Type,
    string? Description,
    decimal AcquisitionCost,
    DateTime AcquisitionDate,
    string LocationId,
    DepreciationMethod DepreciationMethod,
    int UsefulLifeMonths,
    decimal SalvageValue
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record AssetActivatedEvent(
    Guid AssetId,
    DateTime ActivatedAt
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record AssetTransferredEvent(
    Guid AssetId,
    string FromLocationId,
    string ToLocationId,
    string? FromDepartmentId,
    string? ToDepartmentId,
    string Reason,
    DateTime TransferDate
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record MaintenanceRecordedEvent(
    Guid AssetId,
    Guid MaintenanceId,
    MaintenanceType Type,
    string Description,
    DateTime MaintenanceDate,
    decimal Cost,
    string? PerformedBy
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record DepreciationCalculatedEvent(
    Guid AssetId,
    int Year,
    int Month,
    decimal DepreciationAmount,
    decimal AccumulatedDepreciation,
    decimal BookValue
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record AssetDisposedEvent(
    Guid AssetId,
    DateTime DisposalDate,
    decimal DisposalValue,
    string DisposalMethod,
    string Reason,
    decimal GainOrLoss
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record AssetRevaluedEvent(
    Guid AssetId,
    decimal OldValue,
    decimal NewValue,
    string Reason,
    DateTime RevaluationDate
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

#endregion

#region Value Objects

public record MaintenanceRecord(
    Guid Id,
    MaintenanceType Type,
    string Description,
    DateTime MaintenanceDate,
    decimal Cost,
    string? PerformedBy
);

public record DepreciationSchedule(
    int Year,
    int Month,
    decimal Amount,
    decimal AccumulatedDepreciation,
    decimal BookValue
);

public record AssetTransfer(
    DateTime TransferDate,
    string FromLocationId,
    string ToLocationId,
    string Reason
);

#endregion

#region Asset Aggregate

public class Asset : AggregateRoot<Guid>
{
    public string AssetNumber { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public AssetType Type { get; private set; }
    public AssetStatus Status { get; private set; }
    
    // Financial
    public decimal AcquisitionCost { get; private set; }
    public DateTime AcquisitionDate { get; private set; }
    public decimal CurrentValue { get; private set; }
    public decimal AccumulatedDepreciation { get; private set; }
    public decimal SalvageValue { get; private set; }
    
    // Depreciation
    public DepreciationMethod DepreciationMethod { get; private set; }
    public int UsefulLifeMonths { get; private set; }

    public decimal MonthlyDepreciation =>
        this.DepreciationMethod == DepreciationMethod.StraightLine
        ? (this.AcquisitionCost - this.SalvageValue) / this.UsefulLifeMonths
        : 0;
    
    // Location
    public string LocationId { get; private set; } = string.Empty;
    public string? DepartmentId { get; private set; }
    public string? AssignedToUserId { get; private set; }
    
    // History
    public List<MaintenanceRecord> MaintenanceRecords { get; private set; } = [];
    public List<DepreciationSchedule> DepreciationSchedules { get; private set; } = [];
    public List<AssetTransfer> TransferHistory { get; private set; } = [];
    
    public DateTime CreatedAt { get; private set; }
    public DateTime? DisposedAt { get; private set; }

    // Calculated properties
    public decimal BookValue => this.AcquisitionCost - this.AccumulatedDepreciation;
    public decimal TotalMaintenanceCost => this.MaintenanceRecords.Sum(m => m.Cost);
    public bool IsFullyDepreciated => this.BookValue <= this.SalvageValue;

    public static Asset Register(
        Guid id,
        string assetNumber,
        string name,
        AssetType type,
        decimal acquisitionCost,
        DateTime acquisitionDate,
        string locationId,
        DepreciationMethod depreciationMethod,
        int usefulLifeMonths,
        decimal salvageValue,
        string? description = null)
    {
        Asset asset = new();
        asset.ApplyChange(new AssetRegisteredEvent(
            id, assetNumber, name, type, description, 
            acquisitionCost, acquisitionDate, locationId,
            depreciationMethod, usefulLifeMonths, salvageValue));
        return asset;
    }

    public void Activate()
    {
        if (this.Status != AssetStatus.Draft)
            throw new InvalidOperationException("Only draft assets can be activated");

        this.ApplyChange(new AssetActivatedEvent(this.Id, DateTime.UtcNow));
    }

    public void Transfer(string toLocationId, string? toDepartmentId, string reason)
    {
        if (this.Status == AssetStatus.Disposed)
            throw new InvalidOperationException("Cannot transfer disposed asset");

        this.ApplyChange(new AssetTransferredEvent(this.Id, this.LocationId, toLocationId, this.DepartmentId, toDepartmentId, reason, DateTime.UtcNow));
    }

    public void RecordMaintenance(MaintenanceType type, string description, DateTime maintenanceDate, decimal cost, string? performedBy)
    {
        if (this.Status == AssetStatus.Disposed)
            throw new InvalidOperationException("Cannot record maintenance for disposed asset");

        Guid maintenanceId = Guid.NewGuid();
        this.ApplyChange(new MaintenanceRecordedEvent(this.Id, maintenanceId, type, description, maintenanceDate, cost, performedBy));
    }

    public void CalculateDepreciation(int year, int month)
    {
        if (this.DepreciationMethod == DepreciationMethod.None || this.IsFullyDepreciated)
            return;

        decimal depreciationAmount = this.MonthlyDepreciation;
        decimal newAccumulated = this.AccumulatedDepreciation + depreciationAmount;
        decimal newBookValue = this.AcquisitionCost - newAccumulated;

        if (newBookValue < this.SalvageValue)
        {
            depreciationAmount = this.BookValue - this.SalvageValue;
            newAccumulated = this.AcquisitionCost - this.SalvageValue;
            newBookValue = this.SalvageValue;
        }

        this.ApplyChange(new DepreciationCalculatedEvent(this.Id, year, month, depreciationAmount, newAccumulated, newBookValue));
    }

    public void Dispose(decimal disposalValue, string disposalMethod, string reason)
    {
        if (this.Status == AssetStatus.Disposed)
            throw new InvalidOperationException("Asset already disposed");

        decimal gainOrLoss = disposalValue - this.BookValue;
        this.ApplyChange(new AssetDisposedEvent(this.Id, DateTime.UtcNow, disposalValue, disposalMethod, reason, gainOrLoss));
    }

    public void Revalue(decimal newValue, string reason)
    {
        if (this.Status == AssetStatus.Disposed)
            throw new InvalidOperationException("Cannot revalue disposed asset");

        this.ApplyChange(new AssetRevaluedEvent(this.Id, this.CurrentValue, newValue, reason, DateTime.UtcNow));
    }

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case AssetRegisteredEvent e:
                this.Id = e.AssetId;
                this.AssetNumber = e.AssetNumber;
                this.Name = e.Name;
                this.Type = e.Type;
                this.Description = e.Description;
                this.AcquisitionCost = e.AcquisitionCost;
                this.AcquisitionDate = e.AcquisitionDate;
                this.CurrentValue = e.AcquisitionCost;
                this.LocationId = e.LocationId;
                this.DepreciationMethod = e.DepreciationMethod;
                this.UsefulLifeMonths = e.UsefulLifeMonths;
                this.SalvageValue = e.SalvageValue;
                this.Status = AssetStatus.Draft;
                this.CreatedAt = e.OccurredOn;
                break;

            case AssetActivatedEvent:
                this.Status = AssetStatus.Active;
                break;

            case AssetTransferredEvent e:
                this.LocationId = e.ToLocationId;
                this.DepartmentId = e.ToDepartmentId;
                this.TransferHistory.Add(new AssetTransfer(e.TransferDate, e.FromLocationId, e.ToLocationId, e.Reason));
                break;

            case MaintenanceRecordedEvent e:
                this.MaintenanceRecords.Add(new MaintenanceRecord(e.MaintenanceId, e.Type, e.Description, e.MaintenanceDate, e.Cost, e.PerformedBy));
                break;

            case DepreciationCalculatedEvent e:
                this.AccumulatedDepreciation = e.AccumulatedDepreciation;
                this.CurrentValue = e.BookValue;
                this.DepreciationSchedules.Add(new DepreciationSchedule(e.Year, e.Month, e.DepreciationAmount, e.AccumulatedDepreciation, e.BookValue));
                break;

            case AssetDisposedEvent e:
                this.Status = AssetStatus.Disposed;
                this.DisposedAt = e.DisposalDate;
                break;

            case AssetRevaluedEvent e:
                this.CurrentValue = e.NewValue;
                break;
        }
    }
}

#endregion
