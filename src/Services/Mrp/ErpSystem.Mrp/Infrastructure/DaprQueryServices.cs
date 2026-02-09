using Dapr.Client;
using ErpSystem.Mrp.Application;

namespace ErpSystem.Mrp.Infrastructure;

/// <summary>
/// Implementation of IInventoryQueryService using Dapr Service Invocation
/// </summary>
public class DaprInventoryQueryService(DaprClient daprClient) : IInventoryQueryService
{
    private const string InventoryAppId = "inventory-api";

    public async Task<InventoryStatus> GetInventoryStatus(string warehouseId, string materialId)
    {
        // Invoke Inventory Service to get current stock levels
        InventoryResponse response = await daprClient.InvokeMethodAsync<object, InventoryResponse>(
            InventoryAppId,
            $"api/v1/inventory/status/{warehouseId}/{materialId}",
            null);

        return new InventoryStatus(
            response.OnHand,
            response.Reserved,
            response.Available);
    }

    private record InventoryResponse(decimal OnHand, decimal Reserved, decimal Available);
}

/// <summary>
/// Implementation of IProcurementQueryService using Dapr Service Invocation
/// </summary>
public class DaprProcurementQueryService(DaprClient daprClient) : IProcurementQueryService
{
    private const string ProcurementAppId = "procurement-api";

    public async Task<decimal> GetIncomingQuantity(string materialId, string warehouseId)
    {
        // Invoke Procurement Service to get confirmed but not yet received quantities
        decimal response = await daprClient.InvokeMethodAsync<object, decimal>(
            ProcurementAppId,
            $"api/v1/procurement/incoming/{warehouseId}/{materialId}",
            null);

        return response;
    }
}

/// <summary>
/// Implementation of IProductionQueryService using Dapr Service Invocation
/// </summary>
public class DaprProductionQueryService(DaprClient daprClient) : IProductionQueryService
{
    private const string ProductionAppId = "production-api";

    public async Task<decimal> GetPlannedOutputQuantity(string materialId, string warehouseId)
    {
        // Invoke Production Service to get planned/active production order quantities
        decimal response = await daprClient.InvokeMethodAsync<object, decimal>(
            ProductionAppId,
            $"api/v1/production/planned/{warehouseId}/{materialId}",
            null);

        return response;
    }
}
