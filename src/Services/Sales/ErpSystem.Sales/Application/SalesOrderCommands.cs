using MediatR;
using ErpSystem.BuildingBlocks.Domain;
using ErpSystem.BuildingBlocks.EventBus;
using ErpSystem.Sales.Domain;
using ErpSystem.Sales.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ErpSystem.Sales.Application;

public record CreateSOCommand(
    string CustomerId,
    string CustomerName,
    DateTime OrderDate,
    string Currency,
    List<SalesOrderLine> Lines
) : IRequest<Guid>;

public record ConfirmSOCommand(Guid OrderId, string WarehouseId) : IRequest<bool>;
public record CancelSOCommand(Guid OrderId, string Reason) : IRequest<bool>;

public class SalesOrderCommandHandler : 
    IRequestHandler<CreateSOCommand, Guid>,
    IRequestHandler<ConfirmSOCommand, bool>,
    IRequestHandler<CancelSOCommand, bool>
{
    private readonly EventStoreRepository<SalesOrder> _repo;
    private readonly IEventBus _eventBus;

    public SalesOrderCommandHandler(EventStoreRepository<SalesOrder> repo, IEventBus eventBus)
    {
        _repo = repo;
        _eventBus = eventBus;
    }

    public async Task<Guid> Handle(CreateSOCommand request, CancellationToken ct)
    {
        var id = Guid.NewGuid();
        var soNumber = $"SO-{DateTime.UtcNow:yyyyMMdd}-{id.ToString()[..4]}";
        var so = SalesOrder.Create(id, soNumber, request.CustomerId, request.CustomerName, request.OrderDate, request.Currency, request.Lines);
        await _repo.SaveAsync(so);
        return id;
    }

    public async Task<bool> Handle(ConfirmSOCommand request, CancellationToken ct)
    {
        var so = await _repo.LoadAsync(request.OrderId);
        if (so == null) throw new KeyNotFoundException("Order not found");
        
        so.Confirm();
        await _repo.SaveAsync(so);

        // Publish Integration Event for Inventory Reservation
        var integrationEvent = new SalesIntegrationEvents.OrderConfirmedIntegrationEvent(
            so.Id,
            so.SONumber,
            so.Lines.Select(l => new SalesIntegrationEvents.OrderConfirmedItem(
                l.MaterialId,
                request.WarehouseId,
                l.OrderedQuantity
            )).ToList()
        );
        await _eventBus.PublishAsync(integrationEvent);

        return true;
    }

    public async Task<bool> Handle(CancelSOCommand request, CancellationToken ct)
    {
        var so = await _repo.LoadAsync(request.OrderId);
        if (so == null) throw new KeyNotFoundException("Order not found");
        
        so.Cancel(request.Reason);
        await _repo.SaveAsync(so);
        return true;
    }
}
