using MediatR;
using ErpSystem.Finance.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ErpSystem.Finance.Application;

public record GetInvoicesQuery(int Page = 1, int PageSize = 20) : IRequest<List<InvoiceReadModel>>;

public record GetInvoiceQuery(Guid Id) : IRequest<InvoiceReadModel?>;

public record GetPaymentsQuery(int Page = 1, int PageSize = 20) : IRequest<List<PaymentReadModel>>;

public record AgingBucket(string Bucket, decimal Amount, int Count);

public record AgingReport(List<AgingBucket> Buckets);

public record GetAgingReportQuery : IRequest<AgingReport>;

public record GetAgingAnalysisQuery(int Type, DateTime AsOf, string? PartyId) : IRequest<List<AgingBucket>>;

public record GetOverdueInvoicesQuery(int Type, DateTime AsOf, string? PartyId) : IRequest<List<InvoiceReadModel>>;

public class FinanceQueryHandler(FinanceReadDbContext context) :
    IRequestHandler<GetInvoicesQuery, List<InvoiceReadModel>>,
    IRequestHandler<GetInvoiceQuery, InvoiceReadModel?>,
    IRequestHandler<GetPaymentsQuery, List<PaymentReadModel>>,
    IRequestHandler<GetAgingReportQuery, AgingReport>,
    IRequestHandler<GetAgingAnalysisQuery, List<AgingBucket>>,
    IRequestHandler<GetOverdueInvoicesQuery, List<InvoiceReadModel>>,
    IRequestHandler<GetFinancialDashboardStatsQuery, FinancialDashboardStats>
{
    public async Task<List<InvoiceReadModel>> Handle(GetInvoicesQuery request, CancellationToken ct)
    {
        return await context.Invoices.AsNoTracking()
            .OrderByDescending(x => x.InvoiceDate)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);
    }

    public async Task<InvoiceReadModel?> Handle(GetInvoiceQuery request, CancellationToken ct)
    {
        return await context.Invoices.AsNoTracking().FirstOrDefaultAsync(x => x.InvoiceId == request.Id, ct);
    }

    public async Task<List<PaymentReadModel>> Handle(GetPaymentsQuery request, CancellationToken ct)
    {
        return await context.Payments.AsNoTracking()
            .OrderByDescending(x => x.PaymentDate)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);
    }

    public async Task<AgingReport> Handle(GetAgingReportQuery request, CancellationToken ct)
    {
        DateTime today = DateTime.UtcNow.Date;
        List<InvoiceReadModel> unpaidInvoices = await context.Invoices.AsNoTracking()
            .Where(x => x.Status == (int)Domain.InvoiceStatus.Issued || x.Status == (int)Domain.InvoiceStatus.PartiallyPaid)
            .ToListAsync(ct);

        List<AgingBucket> buckets =
        [
            new("Current", unpaidInvoices.Where(x => x.DueDate >= today).Sum(x => x.OutstandingAmount), unpaidInvoices.Count(x => x.DueDate >= today)),
            new("1-30 Days", unpaidInvoices.Where(x => x.DueDate < today && x.DueDate >= today.AddDays(-30)).Sum(x => x.OutstandingAmount), unpaidInvoices.Count(x => x.DueDate < today && x.DueDate >= today.AddDays(-30))),
            new("31-60 Days", unpaidInvoices.Where(x => x.DueDate < today.AddDays(-30) && x.DueDate >= today.AddDays(-60)).Sum(x => x.OutstandingAmount), unpaidInvoices.Count(x => x.DueDate < today.AddDays(-30) && x.DueDate >= today.AddDays(-60))),
            new("61-90 Days", unpaidInvoices.Where(x => x.DueDate < today.AddDays(-60) && x.DueDate >= today.AddDays(-90)).Sum(x => x.OutstandingAmount), unpaidInvoices.Count(x => x.DueDate < today.AddDays(-60) && x.DueDate >= today.AddDays(-90))),
            new("90+ Days", unpaidInvoices.Where(x => x.DueDate < today.AddDays(-90)).Sum(x => x.OutstandingAmount), unpaidInvoices.Count(x => x.DueDate < today.AddDays(-90)))
        ];

        return new AgingReport(buckets);
    }

    public async Task<List<AgingBucket>> Handle(GetAgingAnalysisQuery request, CancellationToken ct)
    {
        // Reuse the logic or verify if "Type" or "PartyId" filters are needed.
        // For now, implementing a basic version similar to AgingReport but filtering if needed.
        DateTime today = request.AsOf.Date;
        IQueryable<InvoiceReadModel> query = context.Invoices.AsNoTracking()
           .Where(x => x.Status == (int)Domain.InvoiceStatus.Issued || x.Status == (int)Domain.InvoiceStatus.PartiallyPaid);

        if (!string.IsNullOrEmpty(request.PartyId))
            query = query.Where(x => x.PartyId == request.PartyId);

        List<InvoiceReadModel> unpaidInvoices = await query.ToListAsync(ct);

        // Helper to create buckets based on implementation requirement. 
        // Assuming same 30-day buckets.
        return
        [
            new("Current", unpaidInvoices.Where(x => x.DueDate >= today).Sum(x => x.OutstandingAmount), unpaidInvoices.Count(x => x.DueDate >= today)),
             new("1-30 Days", unpaidInvoices.Where(x => x.DueDate < today && x.DueDate >= today.AddDays(-30)).Sum(x => x.OutstandingAmount), unpaidInvoices.Count(x => x.DueDate < today && x.DueDate >= today.AddDays(-30))),
             new("31-60 Days", unpaidInvoices.Where(x => x.DueDate < today.AddDays(-30) && x.DueDate >= today.AddDays(-60)).Sum(x => x.OutstandingAmount), unpaidInvoices.Count(x => x.DueDate < today.AddDays(-30) && x.DueDate >= today.AddDays(-60))),
             new("61-90 Days", unpaidInvoices.Where(x => x.DueDate < today.AddDays(-60) && x.DueDate >= today.AddDays(-90)).Sum(x => x.OutstandingAmount), unpaidInvoices.Count(x => x.DueDate < today.AddDays(-60) && x.DueDate >= today.AddDays(-90))),
             new("90+ Days", unpaidInvoices.Where(x => x.DueDate < today.AddDays(-90)).Sum(x => x.OutstandingAmount), unpaidInvoices.Count(x => x.DueDate < today.AddDays(-90)))
        ];
    }

    public async Task<List<InvoiceReadModel>> Handle(GetOverdueInvoicesQuery request, CancellationToken ct)
    {
        DateTime today = request.AsOf.Date;
        IQueryable<InvoiceReadModel> query = context.Invoices.AsNoTracking()
           .Where(x => (x.Status == (int)Domain.InvoiceStatus.Issued || x.Status == (int)Domain.InvoiceStatus.PartiallyPaid) && x.DueDate < today);

        if (!string.IsNullOrEmpty(request.PartyId))
            query = query.Where(x => x.PartyId == request.PartyId);

        return await query.OrderBy(x => x.DueDate).ToListAsync(ct);
    }

    public async Task<FinancialDashboardStats> Handle(GetFinancialDashboardStatsQuery request, CancellationToken ct)
    {
        // 1. Total Metrics
        var metrics = await context.Invoices.AsNoTracking()
            .GroupBy(x => 1)
            .Select(g => new
            {
                TotalAR = g.Where(x => x.Type == (int)Domain.InvoiceType.AccountsReceivable).Sum(x => x.OutstandingAmount),
                TotalAP = g.Where(x => x.Type == (int)Domain.InvoiceType.AccountsPayable).Sum(x => x.OutstandingAmount),
                OrderCount = g.Count(x => x.Type == (int)Domain.InvoiceType.AccountsReceivable && x.Status != (int)Domain.InvoiceStatus.Draft), // AR Orders
                ReconciledCount = g.Count(x => x.Status == (int)Domain.InvoiceStatus.FullyPaid)
            })
            .FirstOrDefaultAsync(ct);

        // 2. Trend Data (Last 6 Months)
        DateTime sixMonthsAgo = DateTime.UtcNow.AddMonths(-6);
        var trends = await context.Payments.AsNoTracking()
            .Where(x => x.PaymentDate >= sixMonthsAgo)
            .GroupBy(x => new { x.PaymentDate.Year, x.PaymentDate.Month, x.Direction })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                g.Key.Direction,
                Amount = g.Sum(x => x.Amount)
            })
            .ToListAsync(ct);

        List<MonthlyTrend> monthlyTrends = new();
        for (int i = 0; i < 6; i++)
        {
            DateTime month = DateTime.UtcNow.AddMonths(-i);
            var mTrends = trends.Where(t => t.Year == month.Year && t.Month == month.Month).ToList();
            monthlyTrends.Add(new MonthlyTrend(
                $"{month.Year}-{month.Month:00}",
                mTrends.Where(t => t.Direction == (int)Domain.PaymentDirection.Incoming).Sum(t => t.Amount),
                mTrends.Where(t => t.Direction == (int)Domain.PaymentDirection.Outgoing).Sum(t => t.Amount)
            ));
        }

        return new FinancialDashboardStats(
            metrics?.TotalAR ?? 0,
            metrics?.TotalAP ?? 0,
            metrics?.OrderCount ?? 0,
            metrics?.ReconciledCount ?? 0,
            monthlyTrends.OrderBy(x => x.Month).ToList()
        );
    }
}

public record GetFinancialDashboardStatsQuery : IRequest<FinancialDashboardStats>;

public record FinancialDashboardStats(
    decimal TotalReceivable,
    decimal TotalPayable,
    int OrderCount,
    int ReconciledCount,
    List<MonthlyTrend> Trends
);

public record MonthlyTrend(string Month, decimal Incoming, decimal Outgoing);

