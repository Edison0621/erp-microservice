using MediatR;
using ErpSystem.Sales.Domain;
using System.Text.Json;

namespace ErpSystem.Sales.Infrastructure;

public class SalesProjections(SalesReadDbContext readDb) :
    INotificationHandler<SalesOrderCreatedEvent>,
    INotificationHandler<SalesOrderConfirmedEvent>,
    INotificationHandler<SalesOrderCancelledEvent>,
    INotificationHandler<SalesOrderShipmentProcessedEvent>,
    INotificationHandler<ShipmentCreatedEvent>
{
    public async Task Handle(SalesOrderCreatedEvent n, CancellationToken ct)
    {
        SalesOrderReadModel model = new SalesOrderReadModel
        {
            Id = n.OrderId,
            SoNumber = n.SoNumber,
            CustomerId = n.CustomerId,
            CustomerName = n.CustomerName,
            Status = nameof(SalesOrderStatus.Draft),
            Currency = n.Currency,
            TotalAmount = n.Lines.Sum(l => l.LineAmount),
            Lines = JsonSerializer.Serialize(n.Lines),
            CreatedAt = n.OccurredOn
        };
        readDb.SalesOrders.Add(model);
        await readDb.SaveChangesAsync(ct);
    }

    public async Task Handle(SalesOrderConfirmedEvent n, CancellationToken ct)
    {
        SalesOrderReadModel? so = await readDb.SalesOrders.FindAsync([n.OrderId], ct);
        if (so != null) { so.Status = nameof(SalesOrderStatus.Confirmed); await readDb.SaveChangesAsync(ct); }
    }

    public async Task Handle(SalesOrderCancelledEvent n, CancellationToken ct)
    {
        SalesOrderReadModel? so = await readDb.SalesOrders.FindAsync([n.OrderId], ct);
        if (so != null) { so.Status = nameof(SalesOrderStatus.Cancelled); await readDb.SaveChangesAsync(ct); }
    }

    public async Task Handle(SalesOrderShipmentProcessedEvent n, CancellationToken ct)
    {
        SalesOrderReadModel? so = await readDb.SalesOrders.FindAsync([n.OrderId], ct);
        if (so != null)
        {
            List<SalesOrderLine> lines = JsonSerializer.Deserialize<List<SalesOrderLine>>(so.Lines) ?? [];
            foreach (ShipmentProcessedLine sl in n.Lines)
            {
                int idx = lines.FindIndex(l => l.LineNumber == sl.LineNumber);
                if (idx >= 0)
                {
                    lines[idx] = lines[idx] with { ShippedQuantity = lines[idx].ShippedQuantity + sl.ShippedQuantity };
                }
            }

            so.Lines = JsonSerializer.Serialize(lines);
            
            // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
            if (lines.All(l => l.ShippedQuantity >= l.OrderedQuantity))
                so.Status = nameof(SalesOrderStatus.FullyShipped);
            else
                so.Status = nameof(SalesOrderStatus.PartiallyShipped);
                
            await readDb.SaveChangesAsync(ct);
        }
    }

    public async Task Handle(ShipmentCreatedEvent n, CancellationToken ct)
    {
        ShipmentReadModel model = new ShipmentReadModel
        {
            Id = n.ShipmentId,
            ShipmentNumber = n.ShipmentNumber,
            SalesOrderId = n.SalesOrderId,
            SoNumber = n.SoNumber,
            ShippedDate = n.ShippedDate,
            ShippedBy = n.ShippedBy,
            WarehouseId = n.WarehouseId,
            Lines = JsonSerializer.Serialize(n.Lines)
        };
        readDb.Shipments.Add(model);
        await readDb.SaveChangesAsync(ct);
    }
}
