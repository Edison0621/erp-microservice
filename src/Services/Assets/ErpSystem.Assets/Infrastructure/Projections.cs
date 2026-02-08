using MediatR;
using ErpSystem.Assets.Domain;

namespace ErpSystem.Assets.Infrastructure;

public class AssetProjectionHandler :
    INotificationHandler<AssetRegisteredEvent>,
    INotificationHandler<AssetActivatedEvent>,
    INotificationHandler<AssetTransferredEvent>,
    INotificationHandler<MaintenanceRecordedEvent>,
    INotificationHandler<DepreciationCalculatedEvent>,
    INotificationHandler<AssetDisposedEvent>,
    INotificationHandler<AssetRevaluedEvent>
{
    private readonly AssetsReadDbContext _db;

    public AssetProjectionHandler(AssetsReadDbContext db) => _db = db;

    public async Task Handle(AssetRegisteredEvent e, CancellationToken ct)
    {
        var monthlyDep = e.DepreciationMethod == DepreciationMethod.StraightLine
            ? (e.AcquisitionCost - e.SalvageValue) / e.UsefulLifeMonths
            : 0;

        var asset = new AssetReadModel
        {
            Id = e.AssetId,
            AssetNumber = e.AssetNumber,
            Name = e.Name,
            Description = e.Description,
            Type = e.Type.ToString(),
            Status = AssetStatus.Draft.ToString(),
            AcquisitionCost = e.AcquisitionCost,
            AcquisitionDate = e.AcquisitionDate,
            CurrentValue = e.AcquisitionCost,
            BookValue = e.AcquisitionCost,
            SalvageValue = e.SalvageValue,
            LocationId = e.LocationId,
            DepreciationMethod = e.DepreciationMethod.ToString(),
            UsefulLifeMonths = e.UsefulLifeMonths,
            MonthlyDepreciation = monthlyDep,
            CreatedAt = e.OccurredOn
        };
        _db.Assets.Add(asset);
        await _db.SaveChangesAsync(ct);
    }

    public async Task Handle(AssetActivatedEvent e, CancellationToken ct)
    {
        var asset = await _db.Assets.FindAsync([e.AssetId], ct);
        if (asset != null)
        {
            asset.Status = AssetStatus.Active.ToString();
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task Handle(AssetTransferredEvent e, CancellationToken ct)
    {
        var asset = await _db.Assets.FindAsync([e.AssetId], ct);
        if (asset != null)
        {
            asset.LocationId = e.ToLocationId;
            asset.DepartmentId = e.ToDepartmentId;
            asset.TransferCount++;
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task Handle(MaintenanceRecordedEvent e, CancellationToken ct)
    {
        var asset = await _db.Assets.FindAsync([e.AssetId], ct);
        
        var maintenance = new MaintenanceReadModel
        {
            Id = e.MaintenanceId,
            AssetId = e.AssetId,
            AssetNumber = asset?.AssetNumber ?? "",
            AssetName = asset?.Name ?? "",
            Type = e.Type.ToString(),
            Description = e.Description,
            MaintenanceDate = e.MaintenanceDate,
            Cost = e.Cost,
            PerformedBy = e.PerformedBy
        };
        _db.MaintenanceRecords.Add(maintenance);

        if (asset != null)
        {
            asset.TotalMaintenanceCost += e.Cost;
            asset.MaintenanceCount++;
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task Handle(DepreciationCalculatedEvent e, CancellationToken ct)
    {
        var asset = await _db.Assets.FindAsync([e.AssetId], ct);

        var depreciation = new DepreciationReadModel
        {
            Id = Guid.NewGuid(),
            AssetId = e.AssetId,
            AssetNumber = asset?.AssetNumber ?? "",
            Year = e.Year,
            Month = e.Month,
            Amount = e.DepreciationAmount,
            AccumulatedDepreciation = e.AccumulatedDepreciation,
            BookValue = e.BookValue
        };
        _db.DepreciationRecords.Add(depreciation);

        if (asset != null)
        {
            asset.AccumulatedDepreciation = e.AccumulatedDepreciation;
            asset.CurrentValue = e.BookValue;
            asset.BookValue = e.BookValue;
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task Handle(AssetDisposedEvent e, CancellationToken ct)
    {
        var asset = await _db.Assets.FindAsync([e.AssetId], ct);
        if (asset != null)
        {
            asset.Status = AssetStatus.Disposed.ToString();
            asset.DisposedAt = e.DisposalDate;
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task Handle(AssetRevaluedEvent e, CancellationToken ct)
    {
        var asset = await _db.Assets.FindAsync([e.AssetId], ct);
        if (asset != null)
        {
            asset.CurrentValue = e.NewValue;
            await _db.SaveChangesAsync(ct);
        }
    }
}
