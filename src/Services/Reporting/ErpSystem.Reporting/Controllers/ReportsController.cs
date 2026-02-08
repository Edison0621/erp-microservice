using ErpSystem.Reporting.Application;
using Microsoft.AspNetCore.Mvc;

namespace ErpSystem.Reporting.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    /// <summary>
    /// Get financial summary report (P&L style)
    /// </summary>
    [HttpGet("financial-summary")]
    public async Task<ActionResult<FinancialSummaryReport>> GetFinancialSummary(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        var report = await _reportService.GetFinancialSummaryAsync(
            startDate ?? DateTime.UtcNow.AddMonths(-1),
            endDate ?? DateTime.UtcNow);
        return Ok(report);
    }

    /// <summary>
    /// Get inventory valuation report
    /// </summary>
    [HttpGet("inventory-valuation")]
    public async Task<ActionResult<InventoryValuationReport>> GetInventoryValuation()
    {
        var report = await _reportService.GetInventoryValuationAsync();
        return Ok(report);
    }

    /// <summary>
    /// Get sales by customer report
    /// </summary>
    [HttpGet("sales-by-customer")]
    public async Task<ActionResult<SalesByCustomerReport>> GetSalesByCustomer(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        var report = await _reportService.GetSalesByCustomerAsync(
            startDate ?? DateTime.UtcNow.AddMonths(-1),
            endDate ?? DateTime.UtcNow);
        return Ok(report);
    }

    /// <summary>
    /// Get purchase order status report
    /// </summary>
    [HttpGet("purchase-orders")]
    public async Task<ActionResult<PurchaseOrderReport>> GetPurchaseOrderReport()
    {
        var report = await _reportService.GetPurchaseOrderReportAsync();
        return Ok(report);
    }

    /// <summary>
    /// Get production efficiency report
    /// </summary>
    [HttpGet("production-efficiency")]
    public async Task<ActionResult<ProductionEfficiencyReport>> GetProductionEfficiency(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        var report = await _reportService.GetProductionEfficiencyAsync(
            startDate ?? DateTime.UtcNow.AddMonths(-1),
            endDate ?? DateTime.UtcNow);
        return Ok(report);
    }
}

// Report DTOs
public record FinancialSummaryReport(
    DateTime StartDate,
    DateTime EndDate,
    decimal TotalRevenue,
    decimal TotalCost,
    decimal GrossProfit,
    decimal GrossMargin,
    IEnumerable<RevenueByCategory> RevenueBreakdown);

public record RevenueByCategory(string Category, decimal Amount, decimal Percentage);

public record InventoryValuationReport(
    DateTime AsOfDate,
    decimal TotalValue,
    int TotalItems,
    IEnumerable<InventoryValuationItem> Items);

public record InventoryValuationItem(
    string MaterialId,
    string MaterialName,
    string Category,
    decimal Quantity,
    string Unit,
    decimal UnitCost,
    decimal TotalValue);

public record SalesByCustomerReport(
    DateTime StartDate,
    DateTime EndDate,
    decimal TotalSales,
    IEnumerable<CustomerSalesItem> Customers);

public record CustomerSalesItem(
    string CustomerId,
    string CustomerName,
    int OrderCount,
    decimal TotalAmount,
    decimal Percentage);

public record PurchaseOrderReport(
    int TotalOrders,
    int PendingOrders,
    int CompletedOrders,
    decimal TotalValue,
    IEnumerable<PurchaseOrderItem> Orders);

public record PurchaseOrderItem(
    string OrderId,
    string SupplierId,
    string SupplierName,
    DateTime OrderDate,
    string Status,
    decimal Amount);

public record ProductionEfficiencyReport(
    DateTime StartDate,
    DateTime EndDate,
    int TotalOrders,
    int CompletedOnTime,
    int Delayed,
    decimal OnTimePercentage,
    decimal AverageLeadTimeDays);
