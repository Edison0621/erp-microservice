using ErpSystem.BuildingBlocks.Domain;
using ErpSystem.Finance.Domain;
using Microsoft.Extensions.Logging;

namespace ErpSystem.Finance.Application;

/// <summary>
/// Integration Event Handlers for Cost Calculation
/// Subscribes to inventory events to update material cost valuations
/// </summary>
public class CostCalculationEventHandlers
{
    private readonly IEventStore _eventStore;
    private readonly ILogger<CostCalculationEventHandlers> _logger;

    public CostCalculationEventHandlers(
        IEventStore eventStore,
        ILogger<CostCalculationEventHandlers> logger)
    {
        _eventStore = eventStore;
        _logger = logger;
    }

    /// <summary>
    /// Handle goods received from procurement
    /// </summary>
    public async Task HandleGoodsReceivedEvent(GoodsReceivedIntegrationEvent @event)
    {
        _logger.LogInformation("Processing goods receipt for cost calculation: {SourceId}", @event.SourceId);

        foreach (var item in @event.Items)
        {
            try
            {
                // Generate a deterministic GUID from the WarehouseId and MaterialId
                var key = $"{item.WarehouseId}_{item.MaterialId}";
                var valuationId = Guid.Parse(string.Format("{0:X32}", key.GetHashCode()));
                
                // Load or create valuation aggregate
                var valuation = await _eventStore.LoadAggregateAsync<MaterialCostValuation>(valuationId);
                
                if (valuation == null)
                {
                    // Create new valuation with initial cost from purchase order
                    valuation = MaterialCostValuation.Create(
                        valuationId,
                        @event.TenantId,
                        item.MaterialId,
                        item.WarehouseId,
                        item.UnitCost);
                }

                // Process receipt to update moving average
                valuation.ProcessReceipt(
                    @event.SourceId,
                    "PO_RECEIPT",
                    item.Quantity,
                    item.UnitCost,
                    @event.OccurredAt);

                await _eventStore.SaveAggregateAsync(valuation);
                
                _logger.LogInformation(
                    "Updated cost valuation for Material {MaterialId} in Warehouse {WarehouseId}. New Avg Cost: {AvgCost}",
                    item.MaterialId, item.WarehouseId, valuation.CurrentAverageCost);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process cost calculation for material {MaterialId}", item.MaterialId);
                throw;
            }
        }
    }

    /// <summary>
    /// Handle material issued (sales shipment or production consumption)
    /// </summary>
    public async Task HandleMaterialIssuedEvent(MaterialIssuedIntegrationEvent @event)
    {
        _logger.LogInformation("Processing material issue for cost calculation: {SourceId}", @event.SourceId);

        foreach (var item in @event.Items)
        {
            try
            {
                var key = $"{item.WarehouseId}_{item.MaterialId}";
                var valuationId = Guid.Parse(string.Format("{0:X32}", key.GetHashCode()));
                
                var valuation = await _eventStore.LoadAggregateAsync<MaterialCostValuation>(valuationId);
                
                if (valuation == null)
                {
                    _logger.LogWarning(
                        "No cost valuation found for Material {MaterialId} in Warehouse {WarehouseId}. Creating with zero cost.",
                        item.MaterialId, item.WarehouseId);
                    
                    valuation = MaterialCostValuation.Create(
                        valuationId,
                        @event.TenantId,
                        item.MaterialId,
                        item.WarehouseId,
                        0);
                }

                // Process issue using current average cost
                valuation.ProcessIssue(
                    @event.SourceId,
                    @event.SourceType, // SO_SHIPMENT or PROD_ISSUE
                    item.Quantity,
                    @event.OccurredAt);

                await _eventStore.SaveAggregateAsync(valuation);
                
                _logger.LogInformation(
                    "Processed cost for material issue {MaterialId}. Cost: {Cost}, Qty: {Qty}, Total: {Total}",
                    item.MaterialId, valuation.CurrentAverageCost, item.Quantity, 
                    valuation.CurrentAverageCost * item.Quantity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process cost calculation for material issue {MaterialId}", item.MaterialId);
                throw;
            }
        }
    }
}

// Integration Events (these would be published by other services)
public record GoodsReceivedIntegrationEvent(
    string TenantId,
    string SourceId,
    string SourceType,
    DateTime OccurredAt,
    List<GoodsReceivedItem> Items);

public record GoodsReceivedItem(
    string MaterialId,
    string WarehouseId,
    decimal Quantity,
    decimal UnitCost);

public record MaterialIssuedIntegrationEvent(
    string TenantId,
    string SourceId,
    string SourceType,
    DateTime OccurredAt,
    List<MaterialIssuedItem> Items);

public record MaterialIssuedItem(
    string MaterialId,
    string WarehouseId,
    decimal Quantity);
