using MediatR;
using ErpSystem.BuildingBlocks.Domain;
using ErpSystem.Sales.Domain;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace ErpSystem.Sales.Infrastructure;

public class SalesProjections : 
    INotificationHandler<SalesOrderCreatedEvent>,
    INotificationHandler<SalesOrderConfirmedEvent>,
    INotificationHandler<SalesOrderCancelledEvent>,
    INotificationHandler<SalesOrderShipmentProcessedEvent>,
    INotificationHandler<ShipmentCreatedEvent>
{
    private readonly SalesReadDbContext _readDb;

    public SalesProjections(SalesReadDbContext readDb)
    {
        _readDb = readDb;
    }

    public async Task Handle(SalesOrderCreatedEvent n, CancellationToken ct)
    {
        var model = new SalesOrderReadModel
        {
            Id = n.OrderId,
            SONumber = n.SONumber,
            CustomerId = n.CustomerId,
            CustomerName = n.CustomerName,
            Status = SalesOrderStatus.Draft.ToString(),
            Currency = n.Currency,
            TotalAmount = n.Lines.Sum(l => l.LineAmount),
            Lines = JsonSerializer.Serialize(n.Lines),
            CreatedAt = n.OccurredOn
        };
        _readDb.SalesOrders.Add(model);
        await _readDb.SaveChangesAsync(ct);
    }

    public async Task Handle(SalesOrderConfirmedEvent n, CancellationToken ct)
    {
        var so = await _readDb.SalesOrders.FindAsync(new object[] { n.OrderId }, ct);
        if (so != null) { so.Status = SalesOrderStatus.Confirmed.ToString(); await _readDb.SaveChangesAsync(ct); }
    }

    public async Task Handle(SalesOrderCancelledEvent n, CancellationToken ct)
    {
        var so = await _readDb.SalesOrders.FindAsync(new object[] { n.OrderId }, ct);
        if (so != null) { so.Status = SalesOrderStatus.Cancelled.ToString(); await _readDb.SaveChangesAsync(ct); }
    }

    public async Task Handle(SalesOrderShipmentProcessedEvent n, CancellationToken ct)
    {
        var so = await _readDb.SalesOrders.FindAsync(new object[] { n.OrderId }, ct);
        if (so != null)
        {
            var lines = JsonSerializer.Deserialize<List<SalesOrderLine>>(so.Lines) ?? new();
            foreach (var sl in n.Lines)
            {
                var idx = lines.FindIndex(l => l.LineNumber == sl.LineNumber);
                if (idx >= 0)
                {
                    lines[idx] = lines[idx] with { ShippedQuantity = lines[idx].ShippedQuantity + sl.ShippedQuantity };
                }
            }
            so.Lines = JsonSerializer.Serialize(lines);
            
            if (lines.All(l => l.ShippedQuantity >= l.OrderedQuantity))
                so.Status = SalesOrderStatus.FullyShipped.ToString();
            else
                so.Status = SalesOrderStatus.PartiallyShipped.ToString();
                
            await _readDb.SaveChangesAsync(ct);
        }
    }

    public async Task Handle(ShipmentCreatedEvent n, CancellationToken ct)
    {
        var model = new ShipmentReadModel
        {
            Id = n.ShipmentId,
            ShipmentNumber = n.ShipmentNumber,
            SalesOrderId = n.SalesOrderId,
            SONumber = n.SONumber,
            ShippedDate = n.ShippedDate,
            ShippedBy = n.ShippedBy,
            WarehouseId = n.WarehouseId,
            Lines = JsonSerializer.Serialize(n.Lines)
        };
        _readDb.Shipments.Add(model);
        await _readDb.SaveChangesAsync(ct);
    }
}
