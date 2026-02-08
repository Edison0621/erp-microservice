using MediatR;
using ErpSystem.BuildingBlocks.Domain;
using ErpSystem.BuildingBlocks.EventBus;
using ErpSystem.Sales.Domain;
using ErpSystem.Sales.Infrastructure;

namespace ErpSystem.Sales.Application;

public record CreateShipmentCommand(
    Guid SalesOrderId,
    DateTime ShippedDate,
    string ShippedBy,
    string WarehouseId,
    List<ShipmentLine> Lines
) : IRequest<Guid>;

public class ShipmentCommandHandler : IRequestHandler<CreateShipmentCommand, Guid>
{
    private readonly EventStoreRepository<Shipment> _shipmentRepo;
    private readonly EventStoreRepository<SalesOrder> _soRepo;
    private readonly IEventBus _eventBus;

    public ShipmentCommandHandler(
        EventStoreRepository<Shipment> shipmentRepo, 
        EventStoreRepository<SalesOrder> soRepo, 
        IEventBus eventBus)
    {
        _shipmentRepo = shipmentRepo;
        _soRepo = soRepo;
        _eventBus = eventBus;
    }

    public async Task<Guid> Handle(CreateShipmentCommand request, CancellationToken ct)
    {
        var so = await _soRepo.LoadAsync(request.SalesOrderId);
        if (so == null) throw new KeyNotFoundException("Order not found");

        var id = Guid.NewGuid();
        var shipmentNumber = $"SHP-{DateTime.UtcNow:yyyyMMdd}-{id.ToString()[..4]}";
        
        var shipment = Shipment.Create(
            id, 
            shipmentNumber, 
            request.SalesOrderId, 
            so.SONumber, 
            request.ShippedDate, 
            request.ShippedBy, 
            request.WarehouseId, 
            request.Lines);
            
        await _shipmentRepo.SaveAsync(shipment);

        // Update SalesOrder status/progress
        so.ProcessShipment(id, request.Lines.Select(l => new ShipmentProcessedLine(l.LineNumber, l.ShippedQuantity)).ToList());
        await _soRepo.SaveAsync(so);

        // Publish Integration Event for Inventory Issue
        var integrationEvent = new SalesIntegrationEvents.ShipmentCreatedIntegrationEvent(
            id,
            so.Id,
            request.WarehouseId,
            request.Lines.Select(l => {
                var soLine = so.Lines.FirstOrDefault(sol => sol.LineNumber == l.LineNumber);
                return new SalesIntegrationEvents.ShipmentItem(l.MaterialId, soLine?.MaterialName ?? "Unknown", l.ShippedQuantity);
            }).ToList()
        );
        await _eventBus.PublishAsync(integrationEvent);

        return id;
    }
}
