using MediatR;
using ErpSystem.Inventory.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ErpSystem.Inventory.Application;

public record GetInventoryItemQuery(string WarehouseId, string BinId, string MaterialId) : IRequest<InventoryItemReadModel?>;
public record SearchInventoryItemsQuery(string? WarehouseId, string? BinId, string? MaterialCode, int Page = 1, int PageSize = 20) : IRequest<List<InventoryItemReadModel>>;
public record GetStockTransactionsQuery(Guid InventoryItemId, int Page = 1, int PageSize = 50) : IRequest<List<StockTransactionReadModel>>;

public class InventoryQueryHandler : 
    IRequestHandler<GetInventoryItemQuery, InventoryItemReadModel?>,
    IRequestHandler<SearchInventoryItemsQuery, List<InventoryItemReadModel>>,
    IRequestHandler<GetStockTransactionsQuery, List<StockTransactionReadModel>>
{
    private readonly InventoryReadDbContext _readDb;

    public InventoryQueryHandler(InventoryReadDbContext readDb)
    {
        _readDb = readDb;
    }

    public async Task<InventoryItemReadModel?> Handle(GetInventoryItemQuery request, CancellationToken ct)
    {
        return await _readDb.InventoryItems
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.WarehouseId == request.WarehouseId && x.BinId == request.BinId && x.MaterialId == request.MaterialId, ct);
    }

    public async Task<List<InventoryItemReadModel>> Handle(SearchInventoryItemsQuery request, CancellationToken ct)
    {
        var query = _readDb.InventoryItems.AsNoTracking().AsQueryable();
        if (!string.IsNullOrEmpty(request.WarehouseId)) query = query.Where(x => x.WarehouseId == request.WarehouseId);
        if (!string.IsNullOrEmpty(request.BinId)) query = query.Where(x => x.BinId == request.BinId);
        // MaterialCode search would require joining or storing it in the read model (which we did).
        if (!string.IsNullOrEmpty(request.MaterialCode)) query = query.Where(x => x.MaterialCode.Contains(request.MaterialCode));

        return await query.OrderBy(x => x.WarehouseId).ThenBy(x => x.MaterialId)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);
    }

    public async Task<List<StockTransactionReadModel>> Handle(GetStockTransactionsQuery request, CancellationToken ct)
    {
        return await _readDb.StockTransactions
            .AsNoTracking()
            .Where(x => x.InventoryItemId == request.InventoryItemId)
            .OrderByDescending(x => x.OccurredOn)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);
    }
}
