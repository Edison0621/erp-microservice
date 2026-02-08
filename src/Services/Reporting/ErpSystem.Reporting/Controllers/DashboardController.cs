using ErpSystem.Reporting.Application;
using Microsoft.AspNetCore.Mvc;

namespace ErpSystem.Reporting.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    /// <summary>
    /// Get KPI summary for executive dashboard
    /// </summary>
    [HttpGet("summary")]
    public async Task<ActionResult<DashboardSummary>> GetSummary()
    {
        var summary = await _dashboardService.GetSummaryAsync();
        return Ok(summary);
    }

    /// <summary>
    /// Get sales trend data for charts
    /// </summary>
    [HttpGet("sales-trend")]
    public async Task<ActionResult<IEnumerable<TrendDataPoint>>> GetSalesTrend([FromQuery] int days = 30)
    {
        var trend = await _dashboardService.GetSalesTrendAsync(days);
        return Ok(trend);
    }

    /// <summary>
    /// Get inventory status by category
    /// </summary>
    [HttpGet("inventory-status")]
    public async Task<ActionResult<IEnumerable<InventoryStatusItem>>> GetInventoryStatus()
    {
        var status = await _dashboardService.GetInventoryStatusAsync();
        return Ok(status);
    }

    /// <summary>
    /// Get top selling products
    /// </summary>
    [HttpGet("top-products")]
    public async Task<ActionResult<IEnumerable<TopProductItem>>> GetTopProducts([FromQuery] int count = 10)
    {
        var products = await _dashboardService.GetTopProductsAsync(count);
        return Ok(products);
    }

    /// <summary>
    /// Get recent activities across all modules
    /// </summary>
    [HttpGet("recent-activities")]
    public async Task<ActionResult<IEnumerable<ActivityItem>>> GetRecentActivities([FromQuery] int count = 20)
    {
        var activities = await _dashboardService.GetRecentActivitiesAsync(count);
        return Ok(activities);
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
