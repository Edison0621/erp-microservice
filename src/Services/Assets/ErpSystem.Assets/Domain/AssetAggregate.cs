using ErpSystem.BuildingBlocks.Domain;

namespace ErpSystem.Assets.Domain;

#region Enums

public enum AssetType
{
    FixedAsset = 0,      // 固定资产
    Equipment = 1,       // 设备
    Vehicle = 2,         // 车辆
    Furniture = 3,       // 办公家具
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
    public decimal MonthlyDepreciation => DepreciationMethod == DepreciationMethod.StraightLine
        ? (AcquisitionCost - SalvageValue) / UsefulLifeMonths
        : 0;
    
    // Location
    public string LocationId { get; private set; } = string.Empty;
    public string? DepartmentId { get; private set; }
    public string? AssignedToUserId { get; private set; }
    
    // History
    public List<MaintenanceRecord> MaintenanceRecords { get; private set; } = new();
    public List<DepreciationSchedule> DepreciationSchedules { get; private set; } = new();
    public List<AssetTransfer> TransferHistory { get; private set; } = new();
    
    public DateTime CreatedAt { get; private set; }
    public DateTime? DisposedAt { get; private set; }

    // Calculated properties
    public decimal BookValue => AcquisitionCost - AccumulatedDepreciation;
    public decimal TotalMaintenanceCost => MaintenanceRecords.Sum(m => m.Cost);
    public bool IsFullyDepreciated => BookValue <= SalvageValue;

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
        var asset = new Asset();
        asset.ApplyChange(new AssetRegisteredEvent(
            id, assetNumber, name, type, description, 
            acquisitionCost, acquisitionDate, locationId,
            depreciationMethod, usefulLifeMonths, salvageValue));
        return asset;
    }

    public void Activate()
    {
        if (Status != AssetStatus.Draft)
            throw new InvalidOperationException("Only draft assets can be activated");
        
        ApplyChange(new AssetActivatedEvent(Id, DateTime.UtcNow));
    }

    public void Transfer(string toLocationId, string? toDepartmentId, string reason)
    {
        if (Status == AssetStatus.Disposed)
            throw new InvalidOperationException("Cannot transfer disposed asset");

        ApplyChange(new AssetTransferredEvent(
            Id, LocationId, toLocationId, DepartmentId, toDepartmentId, reason, DateTime.UtcNow));
    }

    public void RecordMaintenance(MaintenanceType type, string description, DateTime maintenanceDate, decimal cost, string? performedBy)
    {
        if (Status == AssetStatus.Disposed)
            throw new InvalidOperationException("Cannot record maintenance for disposed asset");

        var maintenanceId = Guid.NewGuid();
        ApplyChange(new MaintenanceRecordedEvent(Id, maintenanceId, type, description, maintenanceDate, cost, performedBy));
    }

    public void CalculateDepreciation(int year, int month)
    {
        if (DepreciationMethod == DepreciationMethod.None || IsFullyDepreciated)
            return;

        var depreciationAmount = MonthlyDepreciation;
        var newAccumulated = AccumulatedDepreciation + depreciationAmount;
        var newBookValue = AcquisitionCost - newAccumulated;

        if (newBookValue < SalvageValue)
        {
            depreciationAmount = BookValue - SalvageValue;
            newAccumulated = AcquisitionCost - SalvageValue;
            newBookValue = SalvageValue;
        }

        ApplyChange(new DepreciationCalculatedEvent(Id, year, month, depreciationAmount, newAccumulated, newBookValue));
    }

    public void Dispose(decimal disposalValue, string disposalMethod, string reason)
    {
        if (Status == AssetStatus.Disposed)
            throw new InvalidOperationException("Asset already disposed");

        var gainOrLoss = disposalValue - BookValue;
        ApplyChange(new AssetDisposedEvent(Id, DateTime.UtcNow, disposalValue, disposalMethod, reason, gainOrLoss));
    }

    public void Revalue(decimal newValue, string reason)
    {
        if (Status == AssetStatus.Disposed)
            throw new InvalidOperationException("Cannot revalue disposed asset");

        ApplyChange(new AssetRevaluedEvent(Id, CurrentValue, newValue, reason, DateTime.UtcNow));
    }

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case AssetRegisteredEvent e:
                Id = e.AssetId;
                AssetNumber = e.AssetNumber;
                Name = e.Name;
                Type = e.Type;
                Description = e.Description;
                AcquisitionCost = e.AcquisitionCost;
                AcquisitionDate = e.AcquisitionDate;
                CurrentValue = e.AcquisitionCost;
                LocationId = e.LocationId;
                DepreciationMethod = e.DepreciationMethod;
                UsefulLifeMonths = e.UsefulLifeMonths;
                SalvageValue = e.SalvageValue;
                Status = AssetStatus.Draft;
                CreatedAt = e.OccurredOn;
                break;

            case AssetActivatedEvent e:
                Status = AssetStatus.Active;
                break;

            case AssetTransferredEvent e:
                LocationId = e.ToLocationId;
                DepartmentId = e.ToDepartmentId;
                TransferHistory.Add(new AssetTransfer(e.TransferDate, e.FromLocationId, e.ToLocationId, e.Reason));
                break;

            case MaintenanceRecordedEvent e:
                MaintenanceRecords.Add(new MaintenanceRecord(e.MaintenanceId, e.Type, e.Description, e.MaintenanceDate, e.Cost, e.PerformedBy));
                break;

            case DepreciationCalculatedEvent e:
                AccumulatedDepreciation = e.AccumulatedDepreciation;
                CurrentValue = e.BookValue;
                DepreciationSchedules.Add(new DepreciationSchedule(e.Year, e.Month, e.DepreciationAmount, e.AccumulatedDepreciation, e.BookValue));
                break;

            case AssetDisposedEvent e:
                Status = AssetStatus.Disposed;
                DisposedAt = e.DisposalDate;
                break;

            case AssetRevaluedEvent e:
                CurrentValue = e.NewValue;
                break;
        }
    }
}

#endregion
