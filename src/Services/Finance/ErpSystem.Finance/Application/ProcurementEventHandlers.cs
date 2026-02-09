using ErpSystem.BuildingBlocks.Domain;
using ErpSystem.Finance.Domain;
using MediatR;
using ErpSystem.Finance.IntegrationEvents;
using ErpSystem.BuildingBlocks.Common;

namespace ErpSystem.Finance.Application
{
    public class ProcurementEventHandlers(
        IEventStore eventStore,
        ILogger<ProcurementEventHandlers> logger) :
        INotificationHandler<IntegrationEvents.GoodsReceivedIntegrationEvent>,
        INotificationHandler<GoodsReturnedIntegrationEvent>
    {
        public async Task Handle(IntegrationEvents.GoodsReceivedIntegrationEvent @event, CancellationToken ct)
        {
            logger.LogInformation("Processing goods received for statement: PO {PoId}", @event.PurchaseOrderId);

            string statementIdStr = $"{@event.SupplierId}_{@event.ReceiptDate:yyyyMM}";
            Guid statementId = GuidHelper.CreateDeterministicGuid(statementIdStr);

            Statement? statement = await eventStore.LoadAggregateAsync<Statement>(statementId) ?? Statement.Create(statementId, @event.SupplierId, "CNY"); // Assuming CNY for now

            foreach (IntegrationEvents.GoodsReceivedItem item in @event.Items)
            {
                decimal unitPrice = item.UnitPrice;

                statement.AddLine(new StatementLine(
                    @event.ReceiptId,
                    @event.ReceiptNumber,
                    @event.ReceiptDate,
                    StatementLineType.GoodsReceived,
                    item.MaterialId,
                    "Goods Received",
                    item.Quantity,
                    unitPrice,
                    item.Quantity * unitPrice
                ));
            }

            await eventStore.SaveAggregateAsync(statement);
        }

        public async Task Handle(GoodsReturnedIntegrationEvent @event, CancellationToken ct)
        {
            logger.LogInformation("Processing goods returned for statement: PO {PoId}", @event.PurchaseOrderId);

            string statementIdStr = $"{@event.SupplierId}_{@event.ReturnDate:yyyyMM}";
            Guid statementId = GuidHelper.CreateDeterministicGuid(statementIdStr);

            Statement? statement = await eventStore.LoadAggregateAsync<Statement>(statementId) ?? Statement.Create(statementId, @event.SupplierId, "CNY");

            foreach (GoodsReturnedItem item in @event.Items)
            {
                decimal unitPrice = item.UnitPrice;

                statement.AddLine(new StatementLine(
                    @event.ReturnId,
                    @event.ReturnNumber,
                    @event.ReturnDate,
                    StatementLineType.GoodsReturned,
                    item.MaterialId,
                    @event.Reason,
                    item.Quantity,
                    unitPrice,
                    -(item.Quantity * unitPrice) // Negative amount
                ));
            }

            await eventStore.SaveAggregateAsync(statement);
        }
    }
}

namespace ErpSystem.Finance.IntegrationEvents
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
