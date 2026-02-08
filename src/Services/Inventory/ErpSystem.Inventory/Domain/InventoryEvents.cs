namespace ErpSystem.Inventory.Domain;

public class InventoryIntegrationEvents
{
    public record StockLevelChangedIntegrationEvent(
        Guid InventoryItemId,
        string WarehouseId,
        string MaterialId,
        decimal OnHandQuantity,
        decimal AvailableQuantity
    );
}
