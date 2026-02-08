using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ErpSystem.BuildingBlocks.Domain;
using ErpSystem.Assets.Domain;
using ErpSystem.Assets.Infrastructure;

namespace ErpSystem.Assets.API;

[ApiController]
[Route("api/v1/assets")]
public class AssetsController : ControllerBase
{
    private readonly IEventStore _eventStore;
    private readonly AssetsReadDbContext _readDb;

    public AssetsController(IEventStore eventStore, AssetsReadDbContext readDb)
    {
        _eventStore = eventStore;
        _readDb = readDb;
    }

    [HttpGet]
    public async Task<IActionResult> GetAssets(
        [FromQuery] string? type = null,
        [FromQuery] string? status = null,
        [FromQuery] string? locationId = null)
    {
        var query = _readDb.Assets.AsQueryable();
        if (!string.IsNullOrEmpty(type)) query = query.Where(a => a.Type == type);
        if (!string.IsNullOrEmpty(status)) query = query.Where(a => a.Status == status);
        if (!string.IsNullOrEmpty(locationId)) query = query.Where(a => a.LocationId == locationId);

        var assets = await query.OrderBy(a => a.AssetNumber).ToListAsync();
        return Ok(new { items = assets, total = assets.Count });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetAsset(Guid id)
    {
        var asset = await _readDb.Assets.FindAsync(id);
        return asset == null ? NotFound() : Ok(asset);
    }

    [HttpPost]
    public async Task<IActionResult> RegisterAsset([FromBody] RegisterAssetRequest request)
    {
        var assetNumber = $"AST-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}";

        var asset = Asset.Register(
            Guid.NewGuid(),
            assetNumber,
            request.Name,
            Enum.Parse<AssetType>(request.Type),
            request.AcquisitionCost,
            request.AcquisitionDate,
            request.LocationId,
            Enum.Parse<DepreciationMethod>(request.DepreciationMethod),
            request.UsefulLifeMonths,
            request.SalvageValue,
            request.Description
        );

        await _eventStore.SaveAggregateAsync(asset);
        return CreatedAtAction(nameof(GetAsset), new { id = asset.Id }, new { id = asset.Id, assetNumber });
    }

    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id)
    {
        var asset = await _eventStore.LoadAggregateAsync<Asset>(id);
        if (asset == null) return NotFound();

        asset.Activate();
        await _eventStore.SaveAggregateAsync(asset);
        return Ok(new { id, status = "Active" });
    }

    [HttpPost("{id:guid}/transfer")]
    public async Task<IActionResult> Transfer(Guid id, [FromBody] TransferAssetRequest request)
    {
        var asset = await _eventStore.LoadAggregateAsync<Asset>(id);
        if (asset == null) return NotFound();

        asset.Transfer(request.ToLocationId, request.ToDepartmentId, request.Reason);
        await _eventStore.SaveAggregateAsync(asset);
        return Ok(new { id, locationId = request.ToLocationId });
    }

    [HttpPost("{id:guid}/maintenance")]
    public async Task<IActionResult> RecordMaintenance(Guid id, [FromBody] RecordMaintenanceRequest request)
    {
        var asset = await _eventStore.LoadAggregateAsync<Asset>(id);
        if (asset == null) return NotFound();

        asset.RecordMaintenance(
            Enum.Parse<MaintenanceType>(request.Type),
            request.Description,
            request.MaintenanceDate,
            request.Cost,
            request.PerformedBy
        );

        await _eventStore.SaveAggregateAsync(asset);
        return Ok();
    }

    [HttpPost("{id:guid}/depreciate")]
    public async Task<IActionResult> CalculateDepreciation(Guid id, [FromBody] DepreciateRequest request)
    {
        var asset = await _eventStore.LoadAggregateAsync<Asset>(id);
        if (asset == null) return NotFound();

        asset.CalculateDepreciation(request.Year, request.Month);
        await _eventStore.SaveAggregateAsync(asset);
        return Ok();
    }

    [HttpPost("{id:guid}/dispose")]
    public async Task<IActionResult> Dispose(Guid id, [FromBody] DisposeAssetRequest request)
    {
        var asset = await _eventStore.LoadAggregateAsync<Asset>(id);
        if (asset == null) return NotFound();

        asset.Dispose(request.DisposalValue, request.DisposalMethod, request.Reason);
        await _eventStore.SaveAggregateAsync(asset);
        return Ok(new { id, status = "Disposed" });
    }

    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics()
    {
        var assets = await _readDb.Assets.ToListAsync();
        var byType = assets.GroupBy(a => a.Type).Select(g => new { type = g.Key, count = g.Count(), value = g.Sum(a => a.BookValue) });
        var byStatus = assets.GroupBy(a => a.Status).Select(g => new { status = g.Key, count = g.Count() });

        return Ok(new
        {
            totalAssets = assets.Count,
            totalAcquisitionValue = assets.Sum(a => a.AcquisitionCost),
            totalBookValue = assets.Sum(a => a.BookValue),
            totalAccumulatedDepreciation = assets.Sum(a => a.AccumulatedDepreciation),
            byType,
            byStatus
        });
    }
}

[ApiController]
[Route("api/v1/assets/maintenance")]
public class MaintenanceController : ControllerBase
{
    private readonly AssetsReadDbContext _readDb;

    public MaintenanceController(AssetsReadDbContext readDb) => _readDb = readDb;

    [HttpGet]
    public async Task<IActionResult> GetMaintenanceRecords(
        [FromQuery] Guid? assetId = null,
        [FromQuery] string? type = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        var query = _readDb.MaintenanceRecords.AsQueryable();
        if (assetId.HasValue) query = query.Where(m => m.AssetId == assetId.Value);
        if (!string.IsNullOrEmpty(type)) query = query.Where(m => m.Type == type);
        if (fromDate.HasValue) query = query.Where(m => m.MaintenanceDate >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(m => m.MaintenanceDate <= toDate.Value);

        var records = await query.OrderByDescending(m => m.MaintenanceDate).ToListAsync();
        return Ok(new { items = records, total = records.Count, totalCost = records.Sum(r => r.Cost) });
    }

    [HttpGet("schedule")]
    public async Task<IActionResult> GetMaintenanceSchedule()
    {
        // Return assets due for maintenance (e.g., based on last maintenance date)
        var activeAssets = await _readDb.Assets.Where(a => a.Status == "Active").ToListAsync();
        
        return Ok(new
        {
            dueSoon = activeAssets.Count(a => a.MaintenanceCount == 0),
            totalActiveAssets = activeAssets.Count,
            averageMaintenanceCost = activeAssets.Where(a => a.MaintenanceCount > 0)
                .DefaultIfEmpty().Average(a => a?.TotalMaintenanceCost ?? 0)
        });
    }
}

[ApiController]
[Route("api/v1/assets/depreciation")]
public class DepreciationController : ControllerBase
{
    private readonly AssetsReadDbContext _readDb;

    public DepreciationController(AssetsReadDbContext readDb) => _readDb = readDb;

    [HttpGet]
    public async Task<IActionResult> GetDepreciationRecords(
        [FromQuery] Guid? assetId = null,
        [FromQuery] int? year = null,
        [FromQuery] int? month = null)
    {
        var query = _readDb.DepreciationRecords.AsQueryable();
        if (assetId.HasValue) query = query.Where(d => d.AssetId == assetId.Value);
        if (year.HasValue) query = query.Where(d => d.Year == year.Value);
        if (month.HasValue) query = query.Where(d => d.Month == month.Value);

        var records = await query.OrderByDescending(d => d.Year).ThenByDescending(d => d.Month).ToListAsync();
        return Ok(new { items = records, total = records.Count, totalDepreciation = records.Sum(r => r.Amount) });
    }

    [HttpGet("summary/{year:int}")]
    public async Task<IActionResult> GetYearlySummary(int year)
    {
        var records = await _readDb.DepreciationRecords.Where(d => d.Year == year).ToListAsync();
        
        var byMonth = records.GroupBy(r => r.Month)
            .Select(g => new { month = g.Key, amount = g.Sum(r => r.Amount), assetCount = g.Count() })
            .OrderBy(x => x.month);

        return Ok(new
        {
            year,
            totalDepreciation = records.Sum(r => r.Amount),
            assetsCovered = records.Select(r => r.AssetId).Distinct().Count(),
            byMonth
        });
    }

    [HttpPost("run-batch")]
    public async Task<IActionResult> RunBatchDepreciation([FromBody] BatchDepreciationRequest request)
    {
        // This would typically process all active assets for a given month
        // In a real implementation, this would loop through assets and calculate depreciation
        return Ok(new { 
            message = $"Batch depreciation scheduled for {request.Year}-{request.Month:D2}",
            status = "Processing"
        });
    }
}

#region Request DTOs

public record RegisterAssetRequest(
    string Name,
    string Type,
    string? Description,
    decimal AcquisitionCost,
    DateTime AcquisitionDate,
    string LocationId,
    string DepreciationMethod,
    int UsefulLifeMonths,
    decimal SalvageValue
);

public record TransferAssetRequest(string ToLocationId, string? ToDepartmentId, string Reason);
public record RecordMaintenanceRequest(string Type, string Description, DateTime MaintenanceDate, decimal Cost, string? PerformedBy);
public record DepreciateRequest(int Year, int Month);
public record DisposeAssetRequest(decimal DisposalValue, string DisposalMethod, string Reason);
public record BatchDepreciationRequest(int Year, int Month);

#endregion
