using ErpSystem.Reporting.Controllers;

namespace ErpSystem.Reporting.Application;

public interface IDashboardService
{
    Task<DashboardSummary> GetSummaryAsync();
    Task<IEnumerable<TrendDataPoint>> GetSalesTrendAsync(int days);
    Task<IEnumerable<InventoryStatusItem>> GetInventoryStatusAsync();
    Task<IEnumerable<TopProductItem>> GetTopProductsAsync(int count);
    Task<IEnumerable<ActivityItem>> GetRecentActivitiesAsync(int count);
}

public class DashboardService : IDashboardService
{
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(ILogger<DashboardService> logger)
    {
        _logger = logger;
    }

    public async Task<DashboardSummary> GetSummaryAsync()
    {
        // In production, aggregate from multiple services via Dapr
        // For demo, return sample data
        _logger.LogInformation("Fetching dashboard summary");
        
        return new DashboardSummary(
            TotalRevenue: 1250000.00m,
            RevenueChange: 12.5m,
            TotalOrders: 1847,
            OrdersChange: 8,
            InventoryValue: 3450000.00m,
            LowStockItems: 23,
            PendingPurchaseOrders: 15,
            ActiveProductionOrders: 7);
    }

    public async Task<IEnumerable<TrendDataPoint>> GetSalesTrendAsync(int days)
    {
        var trend = new List<TrendDataPoint>();
        var baseValue = 40000m;
        var random = new Random(42); // Deterministic for demo

        for (int i = days; i >= 0; i--)
        {
            var date = DateTime.UtcNow.Date.AddDays(-i);
            var variance = (decimal)(random.NextDouble() * 20000 - 10000);
            trend.Add(new TrendDataPoint(date, baseValue + variance, date.ToString("MM/dd")));
        }

        return trend;
    }

    public async Task<IEnumerable<InventoryStatusItem>> GetInventoryStatusAsync()
    {
        return new List<InventoryStatusItem>
        {
            new("Raw Materials", 1250, 45, 3, 890000m),
            new("Work in Progress", 320, 12, 0, 450000m),
            new("Finished Goods", 890, 28, 5, 1200000m),
            new("Spare Parts", 2100, 89, 12, 340000m),
            new("Packaging", 560, 23, 2, 120000m)
        };
    }

    public async Task<IEnumerable<TopProductItem>> GetTopProductsAsync(int count)
    {
        return new List<TopProductItem>
        {
            new("PRD-001", "Industrial Motor A500", 234, 156000m),
            new("PRD-002", "Control Panel CP-200", 189, 142000m),
            new("PRD-003", "Hydraulic Pump HP-50", 156, 98000m),
            new("PRD-004", "Sensor Array SA-100", 312, 87000m),
            new("PRD-005", "Power Supply PS-750", 278, 72000m)
        }.Take(count);
    }

    public async Task<IEnumerable<ActivityItem>> GetRecentActivitiesAsync(int count)
    {
        var activities = new List<ActivityItem>
        {
            new(DateTime.UtcNow.AddMinutes(-5), "Sales", "OrderCreated", "New order SO-2024-1847 created", "user-001"),
            new(DateTime.UtcNow.AddMinutes(-12), "Inventory", "StockReceived", "Received 500 units of MAT-001", "user-002"),
            new(DateTime.UtcNow.AddMinutes(-25), "Production", "OrderCompleted", "Production order PO-2024-089 completed", "user-003"),
            new(DateTime.UtcNow.AddMinutes(-45), "Procurement", "POApproved", "Purchase order approved for Supplier-005", "user-004"),
            new(DateTime.UtcNow.AddHours(-1), "Finance", "InvoiceGenerated", "Invoice INV-2024-3421 generated", "user-001")
        };

        return activities.Take(count);
    }
}
