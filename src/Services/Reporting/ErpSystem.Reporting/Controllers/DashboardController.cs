using ErpSystem.Reporting.Application;
using Microsoft.AspNetCore.Mvc;

namespace ErpSystem.Reporting.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController(IDashboardService dashboardService) : ControllerBase
{
    /// <summary>
    /// Get KPI summary for executive dashboard
    /// </summary>
    [HttpGet("summary")]
    public async Task<ActionResult<DashboardSummary>> GetSummary()
    {
        DashboardSummary summary = await dashboardService.GetSummaryAsync();
        return this.Ok(summary);
    }

    /// <summary>
    /// Get sales trend data for charts
    /// </summary>
    [HttpGet("sales-trend")]
    public async Task<ActionResult<IEnumerable<TrendDataPoint>>> GetSalesTrend([FromQuery] int days = 30)
    {
        IEnumerable<TrendDataPoint> trend = await dashboardService.GetSalesTrendAsync(days);
        return this.Ok(trend);
    }

    /// <summary>
    /// Get inventory status by category
    /// </summary>
    [HttpGet("inventory-status")]
    public async Task<ActionResult<IEnumerable<InventoryStatusItem>>> GetInventoryStatus()
    {
        IEnumerable<InventoryStatusItem> status = await dashboardService.GetInventoryStatusAsync();
        return this.Ok(status);
    }

    /// <summary>
    /// Get top selling products
    /// </summary>
    [HttpGet("top-products")]
    public async Task<ActionResult<IEnumerable<TopProductItem>>> GetTopProducts([FromQuery] int count = 10)
    {
        IEnumerable<TopProductItem> products = await dashboardService.GetTopProductsAsync(count);
        return this.Ok(products);
    }

    /// <summary>
    /// Get recent activities across all modules
    /// </summary>
    [HttpGet("recent-activities")]
    public async Task<ActionResult<IEnumerable<ActivityItem>>> GetRecentActivities([FromQuery] int count = 20)
    {
        IEnumerable<ActivityItem> activities = await dashboardService.GetRecentActivitiesAsync(count);
        return this.Ok(activities);
    }
}

// DTOs
public record DashboardSummary(
    decimal TotalRevenue,
    decimal RevenueChange,
    int TotalOrders,
    int OrdersChange,
    decimal InventoryValue,
    int LowStockItems,
    int PendingPurchaseOrders,
    int ActiveProductionOrders);

public record TrendDataPoint(DateTime Date, decimal Value, string Label);

public record InventoryStatusItem(string Category, int TotalItems, int LowStock, int OutOfStock, decimal Value);

public record TopProductItem(string ProductId, string ProductName, int QuantitySold, decimal Revenue);

public record ActivityItem(DateTime Timestamp, string Module, string Action, string Description, string? UserId);
