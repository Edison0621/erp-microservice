using MediatR;
using ErpSystem.BuildingBlocks.Domain;
using ErpSystem.BuildingBlocks.EventBus;
using ErpSystem.Sales.Domain;

namespace ErpSystem.Sales.Application;

public record CreateSoCommand(
    string CustomerId,
    string CustomerName,
    DateTime OrderDate,
    string Currency,
    List<SalesOrderLine> Lines
) : IRequest<Guid>;

public record ConfirmSoCommand(Guid OrderId, string WarehouseId) : IRequest<bool>;

public record CancelSoCommand(Guid OrderId, string Reason) : IRequest<bool>;

public class SalesOrderCommandHandler(EventStoreRepository<SalesOrder> repo, IEventBus eventBus) :
    IRequestHandler<CreateSoCommand, Guid>,
    IRequestHandler<ConfirmSoCommand, bool>,
    IRequestHandler<CancelSoCommand, bool>
{
    public async Task<Guid> Handle(CreateSoCommand request, CancellationToken ct)
    {
        Guid id = Guid.NewGuid();
        string soNumber = $"SO-{DateTime.UtcNow:yyyyMMdd}-{id.ToString()[..4]}";
        SalesOrder so = SalesOrder.Create(id, soNumber, request.CustomerId, request.CustomerName, request.OrderDate, request.Currency, request.Lines);
        await repo.SaveAsync(so);
        return id;
    }

    public async Task<bool> Handle(ConfirmSoCommand request, CancellationToken ct)
    {
        SalesOrder? so = await repo.LoadAsync(request.OrderId);
        if (so == null) throw new KeyNotFoundException("Order not found");

        so.Confirm();
        await repo.SaveAsync(so);

        // Publish Integration Event for Inventory Reservation
        SalesIntegrationEvents.OrderConfirmedIntegrationEvent integrationEvent = new(
            so.Id,
            so.SoNumber,
            so.TotalAmount,
            so.Lines.Select(l => new SalesIntegrationEvents.OrderConfirmedItem(
                l.MaterialId,
                request.WarehouseId,
                l.OrderedQuantity
            )).ToList()
        );
        await eventBus.PublishAsync(integrationEvent, ct);

        return true;
    }

    public async Task<bool> Handle(CancelSoCommand request, CancellationToken ct)
    {
        SalesOrder? so = await repo.LoadAsync(request.OrderId);
        if (so == null) throw new KeyNotFoundException("Order not found");

        so.Cancel(request.Reason);
        await repo.SaveAsync(so);
        return true;
    }
}
