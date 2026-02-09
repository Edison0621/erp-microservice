using MediatR;
using ErpSystem.Procurement.Domain;
using System.Text.Json;

namespace ErpSystem.Procurement.Infrastructure;

public class ProcurementProjections(ProcurementReadDbContext readDb) :
    INotificationHandler<PurchaseOrderCreatedEvent>,
    INotificationHandler<PurchaseOrderSubmittedEvent>,
    INotificationHandler<PurchaseOrderApprovedEvent>,
    INotificationHandler<PurchaseOrderSentEvent>,
    INotificationHandler<GoodsReceivedEvent>,
    INotificationHandler<PurchaseOrderClosedEvent>,
    INotificationHandler<PurchaseOrderCancelledEvent>
{
    public async Task Handle(PurchaseOrderCreatedEvent n, CancellationToken ct)
    {
        PurchaseOrderReadModel model = new PurchaseOrderReadModel
        {
            Id = n.PoId,
            PoNumber = n.PoNumber,
            SupplierId = n.SupplierId,
            SupplierName = n.SupplierName,
            Status = nameof(PurchaseOrderStatus.Draft),
            Currency = n.Currency,
            TotalAmount = n.Lines.Sum(l => l.TotalAmount),
            Lines = JsonSerializer.Serialize(n.Lines),
            CreatedAt = n.OccurredOn
        };
        readDb.PurchaseOrders.Add(model);
        await readDb.SaveChangesAsync(ct);
    }

    public async Task Handle(PurchaseOrderSubmittedEvent n, CancellationToken ct)
    {
        PurchaseOrderReadModel? po = await readDb.PurchaseOrders.FindAsync([n.PoId], ct);
        if (po != null) { po.Status = nameof(PurchaseOrderStatus.PendingApproval); await readDb.SaveChangesAsync(ct); }
    }

    public async Task Handle(PurchaseOrderApprovedEvent n, CancellationToken ct)
    {
        PurchaseOrderReadModel? po = await readDb.PurchaseOrders.FindAsync([n.PoId], ct);
        if (po != null) { po.Status = nameof(PurchaseOrderStatus.Approved); await readDb.SaveChangesAsync(ct); }

        // Also record price history for Approved POs
        List<PurchaseOrderLine>? lines = JsonSerializer.Deserialize<List<PurchaseOrderLine>>(po!.Lines);
        foreach (PurchaseOrderLine line in lines!)
        {
            readDb.PriceHistory.Add(new SupplierPriceHistory
            {
                Id = Guid.NewGuid(),
                SupplierId = po.SupplierId,
                MaterialId = line.MaterialId,
                UnitPrice = line.UnitPrice,
                Currency = po.Currency,
                EffectiveDate = DateTime.UtcNow
            });
        }

        await readDb.SaveChangesAsync(ct);
    }

    public async Task Handle(PurchaseOrderSentEvent n, CancellationToken ct)
    {
        PurchaseOrderReadModel? po = await readDb.PurchaseOrders.FindAsync([n.PoId], ct);
        if (po != null) { po.Status = nameof(PurchaseOrderStatus.SentToSupplier); await readDb.SaveChangesAsync(ct); }
    }

    public async Task Handle(GoodsReceivedEvent n, CancellationToken ct)
    {
        // 1. Create GR Read Model
        GoodsReceiptReadModel gr = new GoodsReceiptReadModel
        {
            Id = n.ReceiptId,
            GrNumber = $"GR-{DateTime.UtcNow:yyyyMMdd}-{n.ReceiptId.ToString()[..4]}",
            PurchaseOrderId = n.PoId,
            ReceiptDate = n.ReceiptDate,
            ReceivedBy = n.ReceivedBy,
            Lines = JsonSerializer.Serialize(n.Lines)
        };
        readDb.GoodsReceipts.Add(gr);

        // 2. Update PO Read Model
        PurchaseOrderReadModel? po = await readDb.PurchaseOrders.FindAsync([n.PoId], ct);
        if (po != null)
        {
            List<PurchaseOrderLine> lines = JsonSerializer.Deserialize<List<PurchaseOrderLine>>(po.Lines) ?? [];
            foreach (ReceiptLine rl in n.Lines)
            {
                int lineIdx = lines.FindIndex(l => l.LineNumber == rl.LineNumber);
                if (lineIdx >= 0)
                {
                    PurchaseOrderLine line = lines[lineIdx];
                    lines[lineIdx] = line with { ReceivedQuantity = line.ReceivedQuantity + rl.Quantity };
                }
            }

            po.Lines = JsonSerializer.Serialize(lines);
            
            po.Status = lines.All(l => l.ReceivedQuantity >= l.OrderedQuantity) ? nameof(PurchaseOrderStatus.FullyReceived) : nameof(PurchaseOrderStatus.PartiallyReceived);
        }

        await readDb.SaveChangesAsync(ct);
    }

    public async Task Handle(PurchaseOrderClosedEvent n, CancellationToken ct)
    {
        PurchaseOrderReadModel? po = await readDb.PurchaseOrders.FindAsync([n.PoId], ct);
        if (po != null) { po.Status = nameof(PurchaseOrderStatus.Closed); await readDb.SaveChangesAsync(ct); }
    }

    public async Task Handle(PurchaseOrderCancelledEvent n, CancellationToken ct)
    {
        PurchaseOrderReadModel? po = await readDb.PurchaseOrders.FindAsync([n.PoId], ct);
        if (po != null) { po.Status = nameof(PurchaseOrderStatus.Cancelled); await readDb.SaveChangesAsync(ct); }
    }
}
