using MediatR;
using ErpSystem.Procurement.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ErpSystem.Procurement.Application;

public record GetPOByIdQuery(Guid Id) : IRequest<PurchaseOrderReadModel?>;
public record SearchPOsQuery(string? SupplierId, string? Status, int Page = 1, int PageSize = 20) : IRequest<List<PurchaseOrderReadModel>>;
public record GetSupplierPriceHistoryQuery(string MaterialId, string? SupplierId) : IRequest<List<SupplierPriceHistory>>;

public class ProcurementQueryHandler : 
    IRequestHandler<GetPOByIdQuery, PurchaseOrderReadModel?>,
    IRequestHandler<SearchPOsQuery, List<PurchaseOrderReadModel>>,
    IRequestHandler<GetSupplierPriceHistoryQuery, List<SupplierPriceHistory>>
{
    private readonly ProcurementReadDbContext _readDb;

    public ProcurementQueryHandler(ProcurementReadDbContext readDb)
    {
        _readDb = readDb;
    }

    public async Task<PurchaseOrderReadModel?> Handle(GetPOByIdQuery request, CancellationToken ct)
    {
        return await _readDb.PurchaseOrders.FindAsync(new object[] { request.Id }, ct);
    }

    public async Task<List<PurchaseOrderReadModel>> Handle(SearchPOsQuery request, CancellationToken ct)
    {
        var query = _readDb.PurchaseOrders.AsNoTracking().AsQueryable();
        if (!string.IsNullOrEmpty(request.SupplierId)) query = query.Where(x => x.SupplierId == request.SupplierId);
        if (!string.IsNullOrEmpty(request.Status)) query = query.Where(x => x.Status == request.Status);
        
        return await query.OrderByDescending(x => x.CreatedAt)
                          .Skip((request.Page - 1) * request.PageSize)
                          .Take(request.PageSize)
                          .ToListAsync(ct);
    }

    public async Task<List<SupplierPriceHistory>> Handle(GetSupplierPriceHistoryQuery request, CancellationToken ct)
    {
        var query = _readDb.PriceHistory.AsNoTracking().AsQueryable();
        query = query.Where(x => x.MaterialId == request.MaterialId);
        if (!string.IsNullOrEmpty(request.SupplierId)) query = query.Where(x => x.SupplierId == request.SupplierId);
        
        return await query.OrderByDescending(x => x.EffectiveDate).ToListAsync(ct);
    }
}
