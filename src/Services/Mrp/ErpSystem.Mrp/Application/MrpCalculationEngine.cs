using ErpSystem.BuildingBlocks.Domain;
using Microsoft.Extensions.Logging;
using ErpSystem.Mrp.Domain;

namespace ErpSystem.Mrp.Application;

/// <summary>
/// MRP Calculation Engine - Analyzes inventory and generates procurement suggestions
/// </summary>
public class MrpCalculationEngine
{
    private readonly IEventStore _eventStore;
    private readonly IInventoryQueryService _inventoryQuery;
    private readonly IProcurementQueryService _procurementQuery;
    private readonly IProductionQueryService _productionQuery;
    private readonly ILogger<MrpCalculationEngine> _logger;

    public MrpCalculationEngine(
        IEventStore eventStore,
        IInventoryQueryService inventoryQuery,
        IProcurementQueryService procurementQuery,
        IProductionQueryService productionQuery,
        ILogger<MrpCalculationEngine> logger)
    {
        _eventStore = eventStore;
        _inventoryQuery = inventoryQuery;
        _procurementQuery = procurementQuery;
        _productionQuery = productionQuery;
        _logger = logger;
    }

    /// <summary>
    /// Run MRP calculation for a specific reordering rule
    /// </summary>
    public async Task<ProcurementSuggestion?> CalculateForRule(ReorderingRule rule)
    {
        _logger.LogInformation(
            "Running MRP calculation for Material {MaterialId} in Warehouse {WarehouseId}",
            rule.MaterialId, rule.WarehouseId);

        // Step 1: Get current inventory status
        var inventory = await _inventoryQuery.GetInventoryStatus(rule.WarehouseId, rule.MaterialId);
        
        // Step 2: Get incoming procurement
        var incomingProcurement = await _procurementQuery.GetIncomingQuantity(rule.MaterialId, rule.WarehouseId);
        
        // Step 3: Get incoming production
        var incomingProduction = await _productionQuery.GetPlannedOutputQuantity(rule.MaterialId, rule.WarehouseId);
        
        // Step 4: Calculate forecasted available
        var forecastedAvailable = inventory.Available + incomingProcurement + incomingProduction;
        
        _logger.LogDebug(
            "MRP Calculation - OnHand: {OnHand}, Reserved: {Reserved}, Available: {Available}, " +
            "Incoming PO: {IncomingPO}, Incoming Prod: {IncomingProd}, Forecasted: {Forecasted}",
            inventory.OnHand, inventory.Reserved, inventory.Available,
            incomingProcurement, incomingProduction, forecastedAvailable);

        // Step 5: Check if reordering is needed
        if (forecastedAvailable >= rule.MinQuantity)
        {
            _logger.LogInformation(
                "No reordering needed. Forecasted available ({Forecasted}) >= Min ({Min})",
                forecastedAvailable, rule.MinQuantity);
            return null;
        }

        // Step 6: Calculate suggested quantity
        var suggestedQuantity = rule.MaxQuantity - forecastedAvailable;
        
        // Use reorder quantity if specified
        if (rule.ReorderQuantity > 0)
        {
            suggestedQuantity = rule.ReorderQuantity;
        }

        // Step 7: Calculate suggested date (current date + lead time)
        var suggestedDate = DateTime.UtcNow.AddDays(rule.LeadTimeDays);

        // Step 8: Create calculation record
        var calculation = new ProcurementCalculation(
            CurrentOnHand: inventory.OnHand,
            Reserved: inventory.Reserved,
            Available: inventory.Available,
            IncomingProcurement: incomingProcurement,
            IncomingProduction: incomingProduction,
            ForecastedAvailable: forecastedAvailable,
            MinQuantity: rule.MinQuantity,
            MaxQuantity: rule.MaxQuantity,
            Reason: $"Forecasted available ({forecastedAvailable}) below minimum ({rule.MinQuantity})");

        // Step 9: Create procurement suggestion
        var suggestionId = Guid.NewGuid();
        var suggestion = ProcurementSuggestion.Create(
            suggestionId,
            rule.TenantId,
            rule.MaterialId,
            rule.WarehouseId,
            suggestedQuantity,
            suggestedDate,
            rule.Id.ToString(),
            calculation);

        await _eventStore.SaveAggregateAsync(suggestion);

        _logger.LogInformation(
            "Created procurement suggestion {SuggestionId} for {Quantity} units of {MaterialId}",
            suggestionId, suggestedQuantity, rule.MaterialId);

        return suggestion;
    }

    /// <summary>
    /// Run MRP for all active reordering rules
    /// </summary>
    public async Task<List<ProcurementSuggestion>> RunMrpForAllRules(string tenantId)
    {
        _logger.LogInformation("Running MRP calculation for all active rules in tenant {TenantId}", tenantId);

        var suggestions = new List<ProcurementSuggestion>();
        
        // This would typically load from a read model
        // For now, this is a placeholder
        var activeRules = await GetActiveReorderingRules(tenantId);

        foreach (var rule in activeRules)
        {
            try
            {
                var suggestion = await CalculateForRule(rule);
                if (suggestion != null)
                {
                    suggestions.Add(suggestion);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Failed to calculate MRP for rule {RuleId}, Material {MaterialId}",
                    rule.Id, rule.MaterialId);
            }
        }

        _logger.LogInformation(
            "MRP calculation completed. Generated {Count} procurement suggestions",
            suggestions.Count);

        return suggestions;
    }

    private async Task<List<ReorderingRule>> GetActiveReorderingRules(string tenantId)
    {
        // TODO: Implement read model query
        // This is a placeholder
        return new List<ReorderingRule>();
    }
}

// Query Service Interfaces (to be implemented by respective services)
public interface IInventoryQueryService
{
    Task<InventoryStatus> GetInventoryStatus(string warehouseId, string materialId);
}

public record InventoryStatus(decimal OnHand, decimal Reserved, decimal Available);

public interface IProcurementQueryService
{
    Task<decimal> GetIncomingQuantity(string materialId, string warehouseId);
}

public interface IProductionQueryService
{
    Task<decimal> GetPlannedOutputQuantity(string materialId, string warehouseId);
}
