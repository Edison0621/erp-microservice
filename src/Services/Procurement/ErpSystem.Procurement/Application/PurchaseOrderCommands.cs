using MediatR;
using ErpSystem.BuildingBlocks.Domain;
using ErpSystem.BuildingBlocks.EventBus;
using ErpSystem.Procurement.Domain;
using ErpSystem.Procurement.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ErpSystem.Procurement.Application;

public record CreatePOCommand(
    string SupplierId, 
    string SupplierName, 
    DateTime OrderDate, 
    string Currency, 
    List<PurchaseOrderLine> Lines) : IRequest<Guid>;

public record SubmitPOCommand(Guid POId) : IRequest<bool>;
public record ApprovePOCommand(Guid POId, string ApprovedBy, string Comment) : IRequest<bool>;
public record SendPOCommand(Guid POId, string SentBy, string Method) : IRequest<bool>;
public record RecordReceiptCommand(Guid POId, DateTime ReceiptDate, string ReceivedBy, List<ReceiptLine> Lines) : IRequest<Guid>;
public record ClosePOCommand(Guid POId, string Reason) : IRequest<bool>;
public record CancelPOCommand(Guid POId, string Reason) : IRequest<bool>;

public class POCommandHandler : 
    IRequestHandler<CreatePOCommand, Guid>,
    IRequestHandler<SubmitPOCommand, bool>,
    IRequestHandler<ApprovePOCommand, bool>,
    IRequestHandler<SendPOCommand, bool>,
    IRequestHandler<RecordReceiptCommand, Guid>,
    IRequestHandler<ClosePOCommand, bool>,
    IRequestHandler<CancelPOCommand, bool>
{
    private readonly EventStoreRepository<PurchaseOrder> _repo;
    private readonly IEventBus _eventBus;

    public POCommandHandler(EventStoreRepository<PurchaseOrder> repo, IEventBus eventBus)
    {
        _repo = repo;
        _eventBus = eventBus;
    }

    public async Task<Guid> Handle(CreatePOCommand request, CancellationToken ct)
    {
        var id = Guid.NewGuid();
        var poNumber = $"PO-{DateTime.UtcNow:yyyyMMdd}-{id.ToString()[..4]}";
        var po = PurchaseOrder.Create(id, poNumber, request.SupplierId, request.SupplierName, request.OrderDate, request.Currency, request.Lines);
        await _repo.SaveAsync(po);
        return id;
    }

    public async Task<bool> Handle(SubmitPOCommand request, CancellationToken ct)
    {
        var po = await _repo.LoadAsync(request.POId);
        if (po == null) throw new KeyNotFoundException("PO not found");
        po.Submit();
        await _repo.SaveAsync(po);
        return true;
    }

    public async Task<bool> Handle(ApprovePOCommand request, CancellationToken ct)
    {
        var po = await _repo.LoadAsync(request.POId);
        if (po == null) throw new KeyNotFoundException("PO not found");
        po.Approve(request.ApprovedBy, request.Comment);
        await _repo.SaveAsync(po);
        return true;
    }

    public async Task<bool> Handle(SendPOCommand request, CancellationToken ct)
    {
        var po = await _repo.LoadAsync(request.POId);
        if (po == null) throw new KeyNotFoundException("PO not found");
        po.Send(request.SentBy, request.Method);
        await _repo.SaveAsync(po);
        return true;
    }

    public async Task<Guid> Handle(RecordReceiptCommand request, CancellationToken ct)
    {
        var po = await _repo.LoadAsync(request.POId);
        if (po == null) throw new KeyNotFoundException("PO not found");
        
        var receiptId = Guid.NewGuid();
        po.RecordReceipt(receiptId, request.ReceiptDate, request.ReceivedBy, request.Lines);
        await _repo.SaveAsync(po);

        // Publish Integration Event for Inventory
        var integrationEvent = new ProcurementIntegrationEvents.GoodsReceivedIntegrationEvent(
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
        
        await _eventBus.PublishAsync(integrationEvent);

        return receiptId;
    }

    public async Task<bool> Handle(ClosePOCommand request, CancellationToken ct)
    {
        var po = await _repo.LoadAsync(request.POId);
        if (po == null) throw new KeyNotFoundException("PO not found");
        po.Close(request.Reason);
        await _repo.SaveAsync(po);
        return true;
    }

    public async Task<bool> Handle(CancelPOCommand request, CancellationToken ct)
    {
        var po = await _repo.LoadAsync(request.POId);
        if (po == null) throw new KeyNotFoundException("PO not found");
        po.Cancel(request.Reason);
        await _repo.SaveAsync(po);
        return true;
    }
}
