using MediatR;

namespace ErpSystem.Production.Domain;

public class ProductionIntegrationEvents
{
    public record ProductionMaterialIssuedIntegrationEvent(
        Guid OrderId,
        string OrderNumber,
        string WarehouseId,
        List<MaterialIssueItem> Items
    ) : INotification;

    public record MaterialIssueItem(string MaterialId, decimal Quantity);

    public record ProductionCompletedIntegrationEvent(
        Guid OrderId,
        string OrderNumber,
        string MaterialId,
        string WarehouseId,
        decimal Quantity
    ) : INotification;
}
