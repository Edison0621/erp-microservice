using ErpSystem.Reporting.Controllers;

namespace ErpSystem.Reporting.Application;

public interface IReportService
{
    Task<FinancialSummaryReport> GetFinancialSummaryAsync(DateTime startDate, DateTime endDate);
    Task<InventoryValuationReport> GetInventoryValuationAsync();
    Task<SalesByCustomerReport> GetSalesByCustomerAsync(DateTime startDate, DateTime endDate);
    Task<PurchaseOrderReport> GetPurchaseOrderReportAsync();
    Task<ProductionEfficiencyReport> GetProductionEfficiencyAsync(DateTime startDate, DateTime endDate);
}

public class ReportService(ILogger<ReportService> logger) : IReportService
{
    public Task<FinancialSummaryReport> GetFinancialSummaryAsync(DateTime startDate, DateTime endDate)
    {
        logger.LogInformation("Generating financial summary report for {Start} to {End}", startDate, endDate);

        return Task.FromResult(new FinancialSummaryReport(
            StartDate: startDate,
            EndDate: endDate,
            TotalRevenue: 1250000.00m,
            TotalCost: 875000.00m,
            GrossProfit: 375000.00m,
            GrossMargin: 30.0m,
            RevenueBreakdown: new List<RevenueByCategory>
            {
                new("Industrial Equipment", 625000m, 50.0m),
                new("Control Systems", 312500m, 25.0m),
                new("Spare Parts", 187500m, 15.0m),
                new("Services", 125000m, 10.0m)
            }));
    }

    public Task<InventoryValuationReport> GetInventoryValuationAsync()
    {
        logger.LogInformation("Generating inventory valuation report");

        return Task.FromResult(new InventoryValuationReport(
            AsOfDate: DateTime.UtcNow,
            TotalValue: 3450000.00m,
            TotalItems: 5120,
            Items: new List<InventoryValuationItem>
            {
                new("MAT-001", "Steel Plate 10mm", "Raw Materials", 500m, "PCS", 150.00m, 75000.00m),
                new("MAT-002", "Copper Wire 2mm", "Raw Materials", 10000m, "M", 12.50m, 125000.00m),
                new("MAT-003", "Motor Assembly", "Semi-finished", 120m, "PCS", 850.00m, 102000.00m),
                new("PRD-001", "Industrial Motor A500", "Finished Goods", 85m, "PCS", 2500.00m, 212500.00m),
                new("PRD-002", "Control Panel CP-200", "Finished Goods", 62m, "PCS", 1800.00m, 111600.00m)
            }));
    }

    public Task<SalesByCustomerReport> GetSalesByCustomerAsync(DateTime startDate, DateTime endDate)
    {
        logger.LogInformation("Generating sales by customer report");

        return Task.FromResult(new SalesByCustomerReport(
            StartDate: startDate,
            EndDate: endDate,
            TotalSales: 1250000.00m,
            Customers: new List<CustomerSalesItem>
            {
                new("CUST-001", "Acme Manufacturing", 45, 312500.00m, 25.0m),
                new("CUST-002", "Global Industries", 38, 250000.00m, 20.0m),
                new("CUST-003", "Tech Solutions Ltd", 52, 187500.00m, 15.0m),
                new("CUST-004", "Premier Engineering", 29, 156250.00m, 12.5m),
                new("CUST-005", "Industrial Corp", 33, 125000.00m, 10.0m)
            }));
    }

    public Task<PurchaseOrderReport> GetPurchaseOrderReportAsync()
    {
        logger.LogInformation("Generating purchase order report");

        return Task.FromResult(new PurchaseOrderReport(
            TotalOrders: 156,
            PendingOrders: 15,
            CompletedOrders: 141,
            TotalValue: 890000.00m,
            Orders: new List<PurchaseOrderItem>
            {
                new("PO-2024-156", "SUP-001", "Steel Supply Co", DateTime.UtcNow.AddDays(-2), "Pending", 45000.00m),
                new("PO-2024-155", "SUP-002", "Electronics World", DateTime.UtcNow.AddDays(-3), "Approved", 32000.00m),
                new("PO-2024-154", "SUP-003", "Motor Components Inc", DateTime.UtcNow.AddDays(-5), "Shipped", 78000.00m),
                new("PO-2024-153", "SUP-001", "Steel Supply Co", DateTime.UtcNow.AddDays(-7), "Received", 52000.00m),
                new("PO-2024-152", "SUP-004", "Packaging Solutions", DateTime.UtcNow.AddDays(-8), "Completed", 18000.00m)
            }));
    }

    public Task<ProductionEfficiencyReport> GetProductionEfficiencyAsync(DateTime startDate, DateTime endDate)
    {
        logger.LogInformation("Generating production efficiency report");

        return Task.FromResult(new ProductionEfficiencyReport(
            StartDate: startDate,
            EndDate: endDate,
            TotalOrders: 89,
            CompletedOnTime: 76,
            Delayed: 13,
            OnTimePercentage: 85.4m,
            AverageLeadTimeDays: 4.2m));
    }
}
