using ErpSystem.Sales.Domain;
using MediatR;
using ErpSystem.Sales.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ErpSystem.Sales.Application;

public record GetSoByIdQuery(Guid Id) : IRequest<SalesOrderReadModel?>;

public record SearchSOsQuery(string? CustomerId, string? Status, int Page = 1, int PageSize = 20) : IRequest<List<SalesOrderReadModel>>;

public record GetBillableLinesQuery(Guid OrderId) : IRequest<BillableLinesResult?>;

public record BillableLinesResult(
    Guid SalesOrderId,
    string CustomerId,
    string CustomerName,
    string Currency,
    List<BillableLine> Lines
);

public record BillableLine(
    string LineNumber,
    string MaterialId,
    string MaterialName,
    decimal ShippedQuantity,
    decimal BillableQuantity,
    decimal UnitPrice,
    decimal DiscountRate
);

public class SalesQueryHandler(SalesReadDbContext readDb) :
    IRequestHandler<GetSoByIdQuery, SalesOrderReadModel?>,
    IRequestHandler<SearchSOsQuery, List<SalesOrderReadModel>>,
    IRequestHandler<GetBillableLinesQuery, BillableLinesResult?>
{
    public async Task<SalesOrderReadModel?> Handle(GetSoByIdQuery request, CancellationToken ct)
    {
        return await readDb.SalesOrders.FindAsync([request.Id], ct);
    }

    public async Task<List<SalesOrderReadModel>> Handle(SearchSOsQuery request, CancellationToken ct)
    {
        IQueryable<SalesOrderReadModel> query = readDb.SalesOrders.AsNoTracking().AsQueryable();
        if (!string.IsNullOrEmpty(request.CustomerId)) query = query.Where(x => x.CustomerId == request.CustomerId);
        if (!string.IsNullOrEmpty(request.Status)) query = query.Where(x => x.Status == request.Status);
        
        return await query.OrderByDescending(x => x.CreatedAt)
                          .Skip((request.Page - 1) * request.PageSize)
                          .Take(request.PageSize)
                          .ToListAsync(ct);
    }

    public async Task<BillableLinesResult?> Handle(GetBillableLinesQuery request, CancellationToken ct)
    {
        SalesOrderReadModel? so = await readDb.SalesOrders.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.OrderId, ct);
        if (so == null) return null;

        List<SalesOrderLine> lines = System.Text.Json.JsonSerializer.Deserialize<List<SalesOrderLine>>(so.Lines) ?? [];
        
        return new BillableLinesResult(
            so.Id,
            so.CustomerId,
            so.CustomerName,
            so.Currency,
            lines.Select(l => new BillableLine(
                l.LineNumber,
                l.MaterialId,
                l.MaterialName,
                l.ShippedQuantity,
                l.ShippedQuantity, // For now, assume entire shipped amount is billable
                l.UnitPrice,
                l.DiscountRate
            )).ToList()
        );
    }
}
