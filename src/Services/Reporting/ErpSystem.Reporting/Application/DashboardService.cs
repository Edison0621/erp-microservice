using ErpSystem.Reporting.Controllers;
using Microsoft.EntityFrameworkCore;

namespace ErpSystem.Reporting.Application;

public interface IDashboardService
{
    Task<DashboardSummary> GetSummaryAsync();
    Task<IEnumerable<TrendDataPoint>> GetSalesTrendAsync(int days);
    Task<IEnumerable<InventoryStatusItem>> GetInventoryStatusAsync();
    Task<IEnumerable<TopProductItem>> GetTopProductsAsync(int count);
    Task<IEnumerable<ActivityItem>> GetRecentActivitiesAsync(int count);
}

/// <summary>
/// TODO DashboardService.
/// Implements the <see cref="ErpSystem.Reporting.Application.IDashboardService" />
/// </summary>
/// <param name="logger">The logger.</param>
/// <seealso cref="ErpSystem.Reporting.Application.IDashboardService" />
public class DashboardService(ReportingDbContext db, ILogger<DashboardService> logger) : IDashboardService
{
    public async Task<DashboardSummary> GetSummaryAsync()
    {
        logger.LogInformation("Fetching real dashboard summary from database");

        DashboardSummaryReadModel? summary = await db.DashboardSummaries.FirstOrDefaultAsync();
        if (summary == null)
        {
            // Initial/Empty state
            return new DashboardSummary(0, 0, 0, 0, 0, 0, 0, 0);
        }

        return new DashboardSummary(
            TotalRevenue: summary.TotalRevenue,
            RevenueChange: summary.RevenueChange,
            TotalOrders: summary.TotalOrders,
            OrdersChange: summary.OrdersChange,
            InventoryValue: summary.InventoryValue,
            LowStockItems: summary.LowStockItems,
            PendingPurchaseOrders: summary.PendingPurchaseOrders,
            ActiveProductionOrders: summary.ActiveProductionOrders);
    }

    public Task<IEnumerable<TrendDataPoint>> GetSalesTrendAsync(int days)
    {
        List<TrendDataPoint> trend = [];
        decimal baseValue = 40000m;
        Random random = new(42); // Deterministic for demo

        for (int i = days; i >= 0; i--)
        {
            DateTime date = DateTime.UtcNow.Date.AddDays(-i);
            decimal variance = (decimal)(random.NextDouble() * 20000 - 10000);
            trend.Add(new TrendDataPoint(date, baseValue + variance, date.ToString("MM/dd")));
        }

        return Task.FromResult<IEnumerable<TrendDataPoint>>(trend);
    }

    public Task<IEnumerable<InventoryStatusItem>> GetInventoryStatusAsync()
    {
        return Task.FromResult<IEnumerable<InventoryStatusItem>>(new List<InventoryStatusItem>
        {
            new("Raw Materials", 1250, 45, 3, 890000m),
            new("Work in Progress", 320, 12, 0, 450000m),
            new("Finished Goods", 890, 28, 5, 1200000m),
            new("Spare Parts", 2100, 89, 12, 340000m),
            new("Packaging", 560, 23, 2, 120000m)
        });
    }

    public Task<IEnumerable<TopProductItem>> GetTopProductsAsync(int count)
    {
        return Task.FromResult(new List<TopProductItem>
        {
            new("PRD-001", "Industrial Motor A500", 234, 156000m),
            new("PRD-002", "Control Panel CP-200", 189, 142000m),
            new("PRD-003", "Hydraulic Pump HP-50", 156, 98000m),
            new("PRD-004", "Sensor Array SA-100", 312, 87000m),
            new("PRD-005", "Power Supply PS-750", 278, 72000m)
        }.Take(count));
    }

    public Task<IEnumerable<ActivityItem>> GetRecentActivitiesAsync(int count)
    {
        List<ActivityItem> activities =
        [
            new(DateTime.UtcNow.AddMinutes(-5), "Sales", "OrderCreated", "New order SO-2024-1847 created", "user-001"),
            new(DateTime.UtcNow.AddMinutes(-12), "Inventory", "StockReceived", "Received 500 units of MAT-001", "user-002"),
            new(DateTime.UtcNow.AddMinutes(-25), "Production", "OrderCompleted", "Production order PO-2024-089 completed", "user-003"),
            new(DateTime.UtcNow.AddMinutes(-45), "Procurement", "POApproved", "Purchase order approved for Supplier-005", "user-004"),
            new(DateTime.UtcNow.AddHours(-1), "Finance", "InvoiceGenerated", "Invoice INV-2024-3421 generated", "user-001")
        ];

        return Task.FromResult(activities.Take(count));
    }
}
