using MediatR;
using ErpSystem.BuildingBlocks.Domain;
using ErpSystem.BuildingBlocks.EventBus;
using ErpSystem.Sales.Domain;

namespace ErpSystem.Sales.Application;

public record CreateShipmentCommand(
    Guid SalesOrderId,
    DateTime ShippedDate,
    string ShippedBy,
    string WarehouseId,
    List<ShipmentLine> Lines
) : IRequest<Guid>;

public class ShipmentCommandHandler(
    EventStoreRepository<Shipment> shipmentRepo,
    EventStoreRepository<SalesOrder> soRepo,
    IEventBus eventBus)
    : IRequestHandler<CreateShipmentCommand, Guid>
{
    public async Task<Guid> Handle(CreateShipmentCommand request, CancellationToken ct)
    {
        SalesOrder? so = await soRepo.LoadAsync(request.SalesOrderId);
        if (so == null) throw new KeyNotFoundException("Order not found");

        Guid id = Guid.NewGuid();
        string shipmentNumber = $"SHP-{DateTime.UtcNow:yyyyMMdd}-{id.ToString()[..4]}";

        Shipment shipment = Shipment.Create(
            id,
            shipmentNumber,
            request.SalesOrderId,
            so.SoNumber,
            request.ShippedDate,
            request.ShippedBy,
            request.WarehouseId,
            request.Lines);

        await shipmentRepo.SaveAsync(shipment);

        // Update SalesOrder status/progress
        so.ProcessShipment(id, request.Lines.Select(l => new ShipmentProcessedLine(l.LineNumber, l.ShippedQuantity)).ToList());
        await soRepo.SaveAsync(so);

        // Publish Integration Event for Inventory Issue and Finance Invoice
        SalesIntegrationEvents.ShipmentCreatedIntegrationEvent integrationEvent = new(
            id,
            so.Id,
            so.CustomerId,
            so.CustomerName,
            request.WarehouseId,
            request.Lines.Select(l =>
            {
                SalesOrderLine? soLine = so.Lines.FirstOrDefault(sol => sol.LineNumber == l.LineNumber);
                return new SalesIntegrationEvents.ShipmentItem(
                    l.MaterialId,
                    soLine?.MaterialName ?? "Unknown",
                    l.ShippedQuantity,
                    soLine?.UnitPrice ?? 0);
            }).ToList()
        );
        await eventBus.PublishAsync(integrationEvent, ct);

        return id;
    }
}
