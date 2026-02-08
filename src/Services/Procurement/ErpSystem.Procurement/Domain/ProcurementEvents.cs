using MediatR;

namespace ErpSystem.Procurement.Domain;

public class ProcurementIntegrationEvents
{
    public record GoodsReceivedIntegrationEvent(
        Guid PurchaseOrderId, 
        string SupplierId, 
        DateTime ReceiptDate, 
        List<GoodsReceivedItem> Items
    ) : INotification;

    public record GoodsReceivedItem(
        string MaterialId, 
        string WarehouseId, 
        string LocationId, 
        decimal Quantity
    );
}
