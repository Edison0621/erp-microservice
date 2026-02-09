using MediatR;
using ErpSystem.BuildingBlocks.Domain;
using ErpSystem.BuildingBlocks.EventBus;
using ErpSystem.Procurement.Domain;

namespace ErpSystem.Procurement.Application;

public record CreatePoCommand(
    string SupplierId, 
    string SupplierName, 
    DateTime OrderDate, 
    string Currency, 
    List<PurchaseOrderLine> Lines) : IRequest<Guid>;

public record SubmitPoCommand(Guid PoId) : IRequest<bool>;

public record ApprovePoCommand(Guid PoId, string ApprovedBy, string Comment) : IRequest<bool>;

public record SendPoCommand(Guid PoId, string SentBy, string Method) : IRequest<bool>;

public record RecordReceiptCommand(Guid PoId, DateTime ReceiptDate, string ReceivedBy, List<ReceiptLine> Lines) : IRequest<Guid>;

public record ClosePoCommand(Guid PoId, string Reason) : IRequest<bool>;

public record CancelPoCommand(Guid PoId, string Reason) : IRequest<bool>;

public class PoCommandHandler(EventStoreRepository<PurchaseOrder> repo, IEventBus eventBus) :
    IRequestHandler<CreatePoCommand, Guid>,
    IRequestHandler<SubmitPoCommand, bool>,
    IRequestHandler<ApprovePoCommand, bool>,
    IRequestHandler<SendPoCommand, bool>,
    IRequestHandler<RecordReceiptCommand, Guid>,
    IRequestHandler<ClosePoCommand, bool>,
    IRequestHandler<CancelPoCommand, bool>
{
    public async Task<Guid> Handle(CreatePoCommand request, CancellationToken ct)
    {
        Guid id = Guid.NewGuid();
        string poNumber = $"PO-{DateTime.UtcNow:yyyyMMdd}-{id.ToString()[..4]}";
        PurchaseOrder po = PurchaseOrder.Create(id, poNumber, request.SupplierId, request.SupplierName, request.OrderDate, request.Currency, request.Lines);
        await repo.SaveAsync(po);
        return id;
    }

    public async Task<bool> Handle(SubmitPoCommand request, CancellationToken ct)
    {
        PurchaseOrder? po = await repo.LoadAsync(request.PoId);
        if (po == null) throw new KeyNotFoundException("PO not found");
        po.Submit();
        await repo.SaveAsync(po);
        return true;
    }

    public async Task<bool> Handle(ApprovePoCommand request, CancellationToken ct)
    {
        PurchaseOrder? po = await repo.LoadAsync(request.PoId);
        if (po == null) throw new KeyNotFoundException("PO not found");
        po.Approve(request.ApprovedBy, request.Comment);
        await repo.SaveAsync(po);
        return true;
    }

    public async Task<bool> Handle(SendPoCommand request, CancellationToken ct)
    {
        PurchaseOrder? po = await repo.LoadAsync(request.PoId);
        if (po == null) throw new KeyNotFoundException("PO not found");
        po.Send(request.SentBy, request.Method);
        await repo.SaveAsync(po);
        return true;
    }

    public async Task<Guid> Handle(RecordReceiptCommand request, CancellationToken ct)
    {
        PurchaseOrder? po = await repo.LoadAsync(request.PoId);
        if (po == null) throw new KeyNotFoundException("PO not found");
        
        Guid receiptId = Guid.NewGuid();
        po.RecordReceipt(receiptId, request.ReceiptDate, request.ReceivedBy, request.Lines);
        await repo.SaveAsync(po);

        // Publish Integration Event for Inventory
        ProcurementIntegrationEvents.GoodsReceivedIntegrationEvent integrationEvent = new ProcurementIntegrationEvents.GoodsReceivedIntegrationEvent(
            po.Id, 
            po.SupplierId, 
            request.ReceiptDate,
            request.Lines.Select(l => new ProcurementIntegrationEvents.GoodsReceivedItem(
                po.Lines.First(pl => pl.LineNumber == l.LineNumber).MaterialId,
                l.WarehouseId,
                l.LocationId,
                l.Quantity
            )).ToList()
        );
        
        await eventBus.PublishAsync(integrationEvent, ct);

        return receiptId;
    }

    public async Task<bool> Handle(ClosePoCommand request, CancellationToken ct)
    {
        PurchaseOrder? po = await repo.LoadAsync(request.PoId);
        if (po == null) throw new KeyNotFoundException("PO not found");
        po.Close(request.Reason);
        await repo.SaveAsync(po);
        return true;
    }

    public async Task<bool> Handle(CancelPoCommand request, CancellationToken ct)
    {
        PurchaseOrder? po = await repo.LoadAsync(request.PoId);
        if (po == null) throw new KeyNotFoundException("PO not found");
        po.Cancel(request.Reason);
        await repo.SaveAsync(po);
        return true;
    }
}
