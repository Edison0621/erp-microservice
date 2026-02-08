using MediatR;
using ErpSystem.BuildingBlocks.Domain;
using ErpSystem.BuildingBlocks.EventBus;
using ErpSystem.Procurement.Domain;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace ErpSystem.Procurement.Infrastructure;

public class ProcurementProjections : 
    INotificationHandler<PurchaseOrderCreatedEvent>,
    INotificationHandler<PurchaseOrderSubmittedEvent>,
    INotificationHandler<PurchaseOrderApprovedEvent>,
    INotificationHandler<PurchaseOrderSentEvent>,
    INotificationHandler<GoodsReceivedEvent>,
    INotificationHandler<PurchaseOrderClosedEvent>,
    INotificationHandler<PurchaseOrderCancelledEvent>
{
    private readonly ProcurementReadDbContext _readDb;

    public ProcurementProjections(ProcurementReadDbContext readDb)
    {
        _readDb = readDb;
    }

    public async Task Handle(PurchaseOrderCreatedEvent n, CancellationToken ct)
    {
        var model = new PurchaseOrderReadModel
        {
            Id = n.POId,
            PONumber = n.PONumber,
            SupplierId = n.SupplierId,
            SupplierName = n.SupplierName,
            Status = PurchaseOrderStatus.Draft.ToString(),
            Currency = n.Currency,
            TotalAmount = n.Lines.Sum(l => l.TotalAmount),
            Lines = JsonSerializer.Serialize(n.Lines),
            CreatedAt = n.OccurredOn
        };
        _readDb.PurchaseOrders.Add(model);
        await _readDb.SaveChangesAsync(ct);
    }

    public async Task Handle(PurchaseOrderSubmittedEvent n, CancellationToken ct)
    {
        var po = await _readDb.PurchaseOrders.FindAsync(new object[] { n.POId }, ct);
        if (po != null) { po.Status = PurchaseOrderStatus.PendingApproval.ToString(); await _readDb.SaveChangesAsync(ct); }
    }

    public async Task Handle(PurchaseOrderApprovedEvent n, CancellationToken ct)
    {
        var po = await _readDb.PurchaseOrders.FindAsync(new object[] { n.POId }, ct);
        if (po != null) { po.Status = PurchaseOrderStatus.Approved.ToString(); await _readDb.SaveChangesAsync(ct); }

        // Also record price history for Approved POs
        var lines = JsonSerializer.Deserialize<List<PurchaseOrderLine>>(po!.Lines);
        foreach (var line in lines!)
        {
            _readDb.PriceHistory.Add(new SupplierPriceHistory
            {
                Id = Guid.NewGuid(),
                SupplierId = po.SupplierId,
                MaterialId = line.MaterialId,
                UnitPrice = line.UnitPrice,
                Currency = po.Currency,
                EffectiveDate = DateTime.UtcNow
            });
        }
        await _readDb.SaveChangesAsync(ct);
    }

    public async Task Handle(PurchaseOrderSentEvent n, CancellationToken ct)
    {
        var po = await _readDb.PurchaseOrders.FindAsync(new object[] { n.POId }, ct);
        if (po != null) { po.Status = PurchaseOrderStatus.SentToSupplier.ToString(); await _readDb.SaveChangesAsync(ct); }
    }

    public async Task Handle(GoodsReceivedEvent n, CancellationToken ct)
    {
        // 1. Create GR Read Model
        var gr = new GoodsReceiptReadModel
        {
            Id = n.ReceiptId,
            GRNumber = $"GR-{DateTime.UtcNow:yyyyMMdd}-{n.ReceiptId.ToString()[..4]}",
            PurchaseOrderId = n.POId,
            ReceiptDate = n.ReceiptDate,
            ReceivedBy = n.ReceivedBy,
            Lines = JsonSerializer.Serialize(n.Lines)
        };
        _readDb.GoodsReceipts.Add(gr);

        // 2. Update PO Read Model
        var po = await _readDb.PurchaseOrders.FindAsync(new object[] { n.POId }, ct);
        if (po != null)
        {
            var lines = JsonSerializer.Deserialize<List<PurchaseOrderLine>>(po.Lines) ?? new();
            foreach (var rl in n.Lines)
            {
                var lineIdx = lines.FindIndex(l => l.LineNumber == rl.LineNumber);
                if (lineIdx >= 0)
                {
                    var line = lines[lineIdx];
                    lines[lineIdx] = line with { ReceivedQuantity = line.ReceivedQuantity + rl.Quantity };
                }
            }
            po.Lines = JsonSerializer.Serialize(lines);
            
            if (lines.All(l => l.ReceivedQuantity >= l.OrderedQuantity))
                po.Status = PurchaseOrderStatus.FullyReceived.ToString();
            else
                po.Status = PurchaseOrderStatus.PartiallyReceived.ToString();
        }

        await _readDb.SaveChangesAsync(ct);
    }

    public async Task Handle(PurchaseOrderClosedEvent n, CancellationToken ct)
    {
        var po = await _readDb.PurchaseOrders.FindAsync(new object[] { n.POId }, ct);
        if (po != null) { po.Status = PurchaseOrderStatus.Closed.ToString(); await _readDb.SaveChangesAsync(ct); }
    }

    public async Task Handle(PurchaseOrderCancelledEvent n, CancellationToken ct)
    {
        var po = await _readDb.PurchaseOrders.FindAsync(new object[] { n.POId }, ct);
        if (po != null) { po.Status = PurchaseOrderStatus.Cancelled.ToString(); await _readDb.SaveChangesAsync(ct); }
    }
}
