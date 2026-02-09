using MediatR;

namespace ErpSystem.Sales.Domain;

public class SalesIntegrationEvents
{
    public record OrderConfirmedIntegrationEvent(
        Guid OrderId,
        string SoNumber,
        decimal TotalAmount,
        List<OrderConfirmedItem> Items
    ) : INotification;

    public record OrderConfirmedItem(
        string MaterialId,
        string WarehouseId,
        decimal Quantity
    );

    public record ShipmentCreatedIntegrationEvent(
        Guid ShipmentId,
        Guid SalesOrderId,
        string CustomerId,
        string CustomerName,
        string WarehouseId,
        List<ShipmentItem> Items
    ) : INotification;

    public record ShipmentItem(
        string MaterialId,
        string MaterialName,
        decimal Quantity,
        decimal UnitPrice
    );
}
