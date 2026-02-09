using MediatR;

namespace ErpSystem.Procurement.Domain;

public class ProcurementIntegrationEvents
{
    public record GoodsReceivedIntegrationEvent(
        Guid PurchaseOrderId,
        Guid ReceiptId,
        string ReceiptNumber,
        string SupplierId,
        DateTime ReceiptDate,
        List<GoodsReceivedItem> Items
    ) : INotification;

    public record GoodsReceivedItem(
        string MaterialId,
        string WarehouseId,
        string LocationId,
        decimal Quantity,
        decimal UnitPrice
    );

    public record GoodsReturnedIntegrationEvent(
        Guid PurchaseOrderId,
        Guid ReturnId,
        string ReturnNumber,
        string SupplierId,
        DateTime ReturnDate,
        List<GoodsReturnedItem> Items,
        string Reason
    ) : INotification;

    public record GoodsReturnedItem(
        string MaterialId,
        decimal Quantity,
        decimal UnitPrice
    );
}
