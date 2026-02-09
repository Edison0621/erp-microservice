using ErpSystem.Reporting.Application;
using Microsoft.AspNetCore.Mvc;

namespace ErpSystem.Reporting.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportsController(IReportService reportService) : ControllerBase
{
    /// <summary>
    /// Get financial summary report
    /// </summary>
    [HttpGet("financial-summary")]
    public async Task<ActionResult<FinancialSummaryReport>> GetFinancialSummary(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        FinancialSummaryReport report = await reportService.GetFinancialSummaryAsync(
            startDate ?? DateTime.UtcNow.AddMonths(-1),
            endDate ?? DateTime.UtcNow);
        return this.Ok(report);
    }

    /// <summary>
    /// Get inventory valuation report
    /// </summary>
    [HttpGet("inventory-valuation")]
    public async Task<ActionResult<InventoryValuationReport>> GetInventoryValuation()
    {
        InventoryValuationReport report = await reportService.GetInventoryValuationAsync();
        return this.Ok(report);
    }

    /// <summary>
    /// Get sales by customer report
    /// </summary>
    [HttpGet("sales-by-customer")]
    public async Task<ActionResult<SalesByCustomerReport>> GetSalesByCustomer(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        SalesByCustomerReport report = await reportService.GetSalesByCustomerAsync(
            startDate ?? DateTime.UtcNow.AddMonths(-1),
            endDate ?? DateTime.UtcNow);
        return this.Ok(report);
    }

    /// <summary>
    /// Get purchase order status report
    /// </summary>
    [HttpGet("purchase-orders")]
    public async Task<ActionResult<PurchaseOrderReport>> GetPurchaseOrderReport()
    {
        PurchaseOrderReport report = await reportService.GetPurchaseOrderReportAsync();
        return this.Ok(report);
    }

    /// <summary>
    /// Get production efficiency report
    /// </summary>
    [HttpGet("production-efficiency")]
    public async Task<ActionResult<ProductionEfficiencyReport>> GetProductionEfficiency(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        ProductionEfficiencyReport report = await reportService.GetProductionEfficiencyAsync(
            startDate ?? DateTime.UtcNow.AddMonths(-1),
            endDate ?? DateTime.UtcNow);
        return this.Ok(report);
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
