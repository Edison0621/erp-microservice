using ErpSystem.BuildingBlocks.Domain;
using ErpSystem.Mrp.Domain;

namespace ErpSystem.Mrp.Application;

/// <summary>
/// MRP Calculation Engine - Analyzes inventory and generates procurement suggestions
/// </summary>
public class MrpCalculationEngine(
    IEventStore eventStore,
    IInventoryQueryService inventoryQuery,
    IProcurementQueryService procurementQuery,
    IProductionQueryService productionQuery,
    ILogger<MrpCalculationEngine> logger)
{
    /// <summary>
    /// Run MRP calculation for a specific reordering rule
    /// </summary>
    public async Task<ProcurementSuggestion?> CalculateForRule(ReorderingRule rule)
    {
        logger.LogInformation(
            "Running MRP calculation for Material {MaterialId} in Warehouse {WarehouseId}",
            rule.MaterialId, rule.WarehouseId);

        // Step 1: Get current inventory status
        InventoryStatus inventory = await inventoryQuery.GetInventoryStatus(rule.WarehouseId, rule.MaterialId);
        
        // Step 2: Get incoming procurement
        decimal incomingProcurement = await procurementQuery.GetIncomingQuantity(rule.MaterialId, rule.WarehouseId);
        
        // Step 3: Get incoming production
        decimal incomingProduction = await productionQuery.GetPlannedOutputQuantity(rule.MaterialId, rule.WarehouseId);
        
        // Step 4: Calculate forecasted available
        decimal forecastedAvailable = inventory.Available + incomingProcurement + incomingProduction;

        logger.LogDebug(
            "MRP Calculation - OnHand: {OnHand}, Reserved: {Reserved}, Available: {Available}, " +
            "Incoming PO: {IncomingPO}, Incoming Prod: {IncomingProd}, Forecasted: {Forecasted}",
            inventory.OnHand, inventory.Reserved, inventory.Available,
            incomingProcurement, incomingProduction, forecastedAvailable);

        // Step 5: Check if reordering is needed
        if (forecastedAvailable >= rule.MinQuantity)
        {
            logger.LogInformation(
                "No reordering needed. Forecasted available ({Forecasted}) >= Min ({Min})",
                forecastedAvailable, rule.MinQuantity);
            return null;
        }

        // Step 6: Calculate suggested quantity
        decimal suggestedQuantity = rule.MaxQuantity - forecastedAvailable;
        
        // Use reorder quantity if specified
        if (rule.ReorderQuantity > 0)
        {
            suggestedQuantity = rule.ReorderQuantity;
        }

        // Step 7: Calculate suggested date (current date + lead time)
        DateTime suggestedDate = DateTime.UtcNow.AddDays(rule.LeadTimeDays);

        // Step 8: Create calculation record
        ProcurementCalculation calculation = new ProcurementCalculation(
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
        Guid suggestionId = Guid.NewGuid();
        ProcurementSuggestion suggestion = ProcurementSuggestion.Create(
            suggestionId,
            rule.TenantId,
            rule.MaterialId,
            rule.WarehouseId,
            suggestedQuantity,
            suggestedDate,
            rule.Id.ToString(),
            calculation);

        await eventStore.SaveAggregateAsync(suggestion);

        logger.LogInformation(
            "Created procurement suggestion {SuggestionId} for {Quantity} units of {MaterialId}",
            suggestionId, suggestedQuantity, rule.MaterialId);

        return suggestion;
    }

    /// <summary>
    /// Run MRP for all active reordering rules
    /// </summary>
    public async Task<List<ProcurementSuggestion>> RunMrpForAllRules(string tenantId)
    {
        logger.LogInformation("Running MRP calculation for all active rules in tenant {TenantId}", tenantId);

        List<ProcurementSuggestion> suggestions = [];
        
        // This would typically load from a read model
        // For now, this is a placeholder
        List<ReorderingRule> activeRules = await this.GetActiveReorderingRules(tenantId);

        foreach (ReorderingRule rule in activeRules)
        {
            try
            {
                ProcurementSuggestion? suggestion = await this.CalculateForRule(rule);
                if (suggestion != null)
                {
                    suggestions.Add(suggestion);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, 
                    "Failed to calculate MRP for rule {RuleId}, Material {MaterialId}",
                    rule.Id, rule.MaterialId);
            }
        }

        logger.LogInformation(
            "MRP calculation completed. Generated {Count} procurement suggestions",
            suggestions.Count);

        return suggestions;
    }

    private async Task<List<ReorderingRule>> GetActiveReorderingRules(string tenantId)
    {
        // TODO: Implement read model query
        // This is a placeholder
        return [];
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
