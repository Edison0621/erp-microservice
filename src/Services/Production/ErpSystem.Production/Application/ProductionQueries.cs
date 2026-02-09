using MediatR;
using ErpSystem.Production.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ErpSystem.Production.Application;

public record GetProductionOrderByIdQuery(Guid Id) : IRequest<ProductionOrderReadModel?>;

public record SearchProductionOrdersQuery(string? MaterialId, string? Status, int Page = 1, int PageSize = 20) : IRequest<List<ProductionOrderReadModel>>;

public record GetProductionWipQuery(string? MaterialId) : IRequest<List<ProductionOrderReadModel>>;

public class ProductionQueryHandler(ProductionReadDbContext readDb) :
    IRequestHandler<GetProductionOrderByIdQuery, ProductionOrderReadModel?>,
    IRequestHandler<SearchProductionOrdersQuery, List<ProductionOrderReadModel>>,
    IRequestHandler<GetProductionWipQuery, List<ProductionOrderReadModel>>
{
    public async Task<ProductionOrderReadModel?> Handle(GetProductionOrderByIdQuery request, CancellationToken ct)
    {
        return await readDb.ProductionOrders.FindAsync([request.Id], ct);
    }

    public async Task<List<ProductionOrderReadModel>> Handle(SearchProductionOrdersQuery request, CancellationToken ct)
    {
        IQueryable<ProductionOrderReadModel> query = readDb.ProductionOrders.AsNoTracking().AsQueryable();
        if (!string.IsNullOrEmpty(request.MaterialId)) query = query.Where(x => x.MaterialId == request.MaterialId);
        if (!string.IsNullOrEmpty(request.Status)) query = query.Where(x => x.Status == request.Status);
        
        return await query.OrderByDescending(x => x.CreatedDate)
                          .Skip((request.Page - 1) * request.PageSize)
                          .Take(request.PageSize)
                          .ToListAsync(ct);
    }

    public async Task<List<ProductionOrderReadModel>> Handle(GetProductionWipQuery request, CancellationToken ct)
    {
        string[] wipStatuses = ["Released", "InProgress", "PartiallyCompleted"];
        IQueryable<ProductionOrderReadModel> query = readDb.ProductionOrders.AsNoTracking().Where(x => wipStatuses.Contains(x.Status));
        if (!string.IsNullOrEmpty(request.MaterialId)) query = query.Where(x => x.MaterialId == request.MaterialId);
        
        return await query.OrderBy(x => x.CreatedDate).ToListAsync(ct);
    }
}
