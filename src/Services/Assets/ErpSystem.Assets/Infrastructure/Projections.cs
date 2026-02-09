using MediatR;
using ErpSystem.Assets.Domain;

namespace ErpSystem.Assets.Infrastructure;

public class AssetProjectionHandler(AssetsReadDbContext db) :
    INotificationHandler<AssetRegisteredEvent>,
    INotificationHandler<AssetActivatedEvent>,
    INotificationHandler<AssetTransferredEvent>,
    INotificationHandler<MaintenanceRecordedEvent>,
    INotificationHandler<DepreciationCalculatedEvent>,
    INotificationHandler<AssetDisposedEvent>,
    INotificationHandler<AssetRevaluedEvent>
{
    public async Task Handle(AssetRegisteredEvent e, CancellationToken ct)
    {
        decimal monthlyDep = e.DepreciationMethod == DepreciationMethod.StraightLine
            ? (e.AcquisitionCost - e.SalvageValue) / e.UsefulLifeMonths
            : 0;

        AssetReadModel asset = new()
        {
            Id = e.AssetId,
            AssetNumber = e.AssetNumber,
            Name = e.Name,
            Description = e.Description,
            Type = e.Type.ToString(),
            Status = nameof(AssetStatus.Draft),
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
        db.Assets.Add(asset);
        await db.SaveChangesAsync(ct);
    }

    public async Task Handle(AssetActivatedEvent e, CancellationToken ct)
    {
        AssetReadModel? asset = await db.Assets.FindAsync([e.AssetId], ct);
        if (asset != null)
        {
            asset.Status = nameof(AssetStatus.Active);
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task Handle(AssetTransferredEvent e, CancellationToken ct)
    {
        AssetReadModel? asset = await db.Assets.FindAsync([e.AssetId], ct);
        if (asset != null)
        {
            asset.LocationId = e.ToLocationId;
            asset.DepartmentId = e.ToDepartmentId;
            asset.TransferCount++;
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task Handle(MaintenanceRecordedEvent e, CancellationToken ct)
    {
        AssetReadModel? asset = await db.Assets.FindAsync([e.AssetId], ct);
        
        MaintenanceReadModel maintenance = new()
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
        db.MaintenanceRecords.Add(maintenance);

        if (asset != null)
        {
            asset.TotalMaintenanceCost += e.Cost;
            asset.MaintenanceCount++;
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task Handle(DepreciationCalculatedEvent e, CancellationToken ct)
    {
        AssetReadModel? asset = await db.Assets.FindAsync([e.AssetId], ct);

        DepreciationReadModel depreciation = new()
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
        db.DepreciationRecords.Add(depreciation);

        if (asset != null)
        {
            asset.AccumulatedDepreciation = e.AccumulatedDepreciation;
            asset.CurrentValue = e.BookValue;
            asset.BookValue = e.BookValue;
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task Handle(AssetDisposedEvent e, CancellationToken ct)
    {
        AssetReadModel? asset = await db.Assets.FindAsync([e.AssetId], ct);
        if (asset != null)
        {
            asset.Status = nameof(AssetStatus.Disposed);
            asset.DisposedAt = e.DisposalDate;
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task Handle(AssetRevaluedEvent e, CancellationToken ct)
    {
        AssetReadModel? asset = await db.Assets.FindAsync([e.AssetId], ct);
        if (asset != null)
        {
            asset.CurrentValue = e.NewValue;
            await db.SaveChangesAsync(ct);
        }
    }
}
