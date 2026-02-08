using MediatR;
using ErpSystem.Finance.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ErpSystem.Finance.Application;

public record GetInvoicesQuery(int Page = 1, int PageSize = 20) : IRequest<List<InvoiceReadModel>>;
public record GetInvoiceQuery(Guid Id) : IRequest<InvoiceReadModel?>;
public record GetPaymentsQuery(int Page = 1, int PageSize = 20) : IRequest<List<PaymentReadModel>>;

public record AgingBucket(string Bucket, decimal Amount, int Count);
public record AgingReport(List<AgingBucket> Buckets);
public record GetAgingReportQuery() : IRequest<AgingReport>;
public record GetAgingAnalysisQuery(int Type, DateTime AsOf, string? PartyId) : IRequest<List<AgingBucket>>;
public record GetOverdueInvoicesQuery(int Type, DateTime AsOf, string? PartyId) : IRequest<List<InvoiceReadModel>>;

public class FinanceQueryHandler : 
    IRequestHandler<GetInvoicesQuery, List<InvoiceReadModel>>,
    IRequestHandler<GetInvoiceQuery, InvoiceReadModel?>,
    IRequestHandler<GetPaymentsQuery, List<PaymentReadModel>>,
    IRequestHandler<GetAgingReportQuery, AgingReport>,
    IRequestHandler<GetAgingAnalysisQuery, List<AgingBucket>>,
    IRequestHandler<GetOverdueInvoicesQuery, List<InvoiceReadModel>>
{
    private readonly FinanceReadDbContext _context;

    public FinanceQueryHandler(FinanceReadDbContext context)
    {
        _context = context;
    }

    public async Task<List<InvoiceReadModel>> Handle(GetInvoicesQuery request, CancellationToken ct)
    {
        return await _context.Invoices.AsNoTracking()
            .OrderByDescending(x => x.InvoiceDate)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);
    }

    public async Task<InvoiceReadModel?> Handle(GetInvoiceQuery request, CancellationToken ct)
    {
        return await _context.Invoices.AsNoTracking().FirstOrDefaultAsync(x => x.InvoiceId == request.Id, ct);
    }

    public async Task<List<PaymentReadModel>> Handle(GetPaymentsQuery request, CancellationToken ct)
    {
        return await _context.Payments.AsNoTracking()
            .OrderByDescending(x => x.PaymentDate)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);
    }

    public async Task<AgingReport> Handle(GetAgingReportQuery request, CancellationToken ct)
    {
        var today = DateTime.UtcNow.Date;
        var unpaidInvoices = await _context.Invoices.AsNoTracking()
            .Where(x => x.Status == (int)Domain.InvoiceStatus.Issued || x.Status == (int)Domain.InvoiceStatus.PartiallyPaid)
            .ToListAsync(ct);

        var buckets = new List<AgingBucket>
        {
            new("Current", unpaidInvoices.Where(x => x.DueDate >= today).Sum(x => x.OutstandingAmount), unpaidInvoices.Count(x => x.DueDate >= today)),
            new("1-30 Days", unpaidInvoices.Where(x => x.DueDate < today && x.DueDate >= today.AddDays(-30)).Sum(x => x.OutstandingAmount), unpaidInvoices.Count(x => x.DueDate < today && x.DueDate >= today.AddDays(-30))),
            new("31-60 Days", unpaidInvoices.Where(x => x.DueDate < today.AddDays(-30) && x.DueDate >= today.AddDays(-60)).Sum(x => x.OutstandingAmount), unpaidInvoices.Count(x => x.DueDate < today.AddDays(-30) && x.DueDate >= today.AddDays(-60))),
            new("61-90 Days", unpaidInvoices.Where(x => x.DueDate < today.AddDays(-60) && x.DueDate >= today.AddDays(-90)).Sum(x => x.OutstandingAmount), unpaidInvoices.Count(x => x.DueDate < today.AddDays(-60) && x.DueDate >= today.AddDays(-90))),
            new("90+ Days", unpaidInvoices.Where(x => x.DueDate < today.AddDays(-90)).Sum(x => x.OutstandingAmount), unpaidInvoices.Count(x => x.DueDate < today.AddDays(-90)))
        };

        return new AgingReport(buckets);
    }

    public async Task<List<AgingBucket>> Handle(GetAgingAnalysisQuery request, CancellationToken ct)
    {
         // Reuse the logic or verify if "Type" or "PartyId" filters are needed.
         // For now, implementing a basic version similar to AgingReport but filtering if needed.
         var today = request.AsOf.Date;
         var query = _context.Invoices.AsNoTracking()
            .Where(x => x.Status == (int)Domain.InvoiceStatus.Issued || x.Status == (int)Domain.InvoiceStatus.PartiallyPaid);
         
         if (!string.IsNullOrEmpty(request.PartyId))
             query = query.Where(x => x.PartyId == request.PartyId);

         var unpaidInvoices = await query.ToListAsync(ct);

         // Helper to create buckets based on implementation requirement. 
         // Assuming same 30-day buckets.
         return new List<AgingBucket>
        {
            new("Current", unpaidInvoices.Where(x => x.DueDate >= today).Sum(x => x.OutstandingAmount), unpaidInvoices.Count(x => x.DueDate >= today)),
            new("1-30 Days", unpaidInvoices.Where(x => x.DueDate < today && x.DueDate >= today.AddDays(-30)).Sum(x => x.OutstandingAmount), unpaidInvoices.Count(x => x.DueDate < today && x.DueDate >= today.AddDays(-30))),
            new("31-60 Days", unpaidInvoices.Where(x => x.DueDate < today.AddDays(-30) && x.DueDate >= today.AddDays(-60)).Sum(x => x.OutstandingAmount), unpaidInvoices.Count(x => x.DueDate < today.AddDays(-30) && x.DueDate >= today.AddDays(-60))),
            new("61-90 Days", unpaidInvoices.Where(x => x.DueDate < today.AddDays(-60) && x.DueDate >= today.AddDays(-90)).Sum(x => x.OutstandingAmount), unpaidInvoices.Count(x => x.DueDate < today.AddDays(-60) && x.DueDate >= today.AddDays(-90))),
            new("90+ Days", unpaidInvoices.Where(x => x.DueDate < today.AddDays(-90)).Sum(x => x.OutstandingAmount), unpaidInvoices.Count(x => x.DueDate < today.AddDays(-90)))
        };
    }

    public async Task<List<InvoiceReadModel>> Handle(GetOverdueInvoicesQuery request, CancellationToken ct)
    {
        var today = request.AsOf.Date;
        var query = _context.Invoices.AsNoTracking()
           .Where(x => (x.Status == (int)Domain.InvoiceStatus.Issued || x.Status == (int)Domain.InvoiceStatus.PartiallyPaid) && x.DueDate < today);

        if (!string.IsNullOrEmpty(request.PartyId))
             query = query.Where(x => x.PartyId == request.PartyId);

        return await query.OrderBy(x => x.DueDate).ToListAsync(ct);
    }
}
