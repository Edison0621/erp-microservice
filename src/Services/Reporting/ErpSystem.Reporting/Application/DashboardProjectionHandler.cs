using MediatR;
using Microsoft.EntityFrameworkCore;
using ErpSystem.Sales.Domain;
using ErpSystem.Procurement.Domain;

namespace ErpSystem.Reporting.Application;

public class DashboardProjectionHandler(ReportingDbContext db) :
    INotificationHandler<SalesIntegrationEvents.OrderConfirmedIntegrationEvent>,
    INotificationHandler<ProcurementIntegrationEvents.GoodsReceivedIntegrationEvent>
{
    public async Task Handle(SalesIntegrationEvents.OrderConfirmedIntegrationEvent n, CancellationToken ct)
    {
        DashboardSummaryReadModel summary = await this.GetOrCreateSummary(ct);
        summary.TotalOrders += 1;
        summary.TotalRevenue += n.TotalAmount;
        summary.LastUpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task Handle(ProcurementIntegrationEvents.GoodsReceivedIntegrationEvent n, CancellationToken ct)
    {
        DashboardSummaryReadModel summary = await this.GetOrCreateSummary(ct);
        summary.PendingPurchaseOrders = Math.Max(0, summary.PendingPurchaseOrders - 1);
        summary.InventoryValue += n.Items.Sum(x => x.Quantity * x.UnitPrice);
        summary.LastUpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    private async Task<DashboardSummaryReadModel> GetOrCreateSummary(CancellationToken ct)
    {
        DashboardSummaryReadModel? summary = await db.DashboardSummaries.FirstOrDefaultAsync(ct);
        if (summary == null)
        {
            summary = new DashboardSummaryReadModel { TenantId = "DEFAULT" };
            db.DashboardSummaries.Add(summary);
        }

        return summary;
    }
}