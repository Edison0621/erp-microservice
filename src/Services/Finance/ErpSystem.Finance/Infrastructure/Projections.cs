using MediatR;
using ErpSystem.Finance.Domain;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ErpSystem.Finance.Infrastructure;

public class FinanceProjections : 
    INotificationHandler<InvoiceCreatedEvent>,
    INotificationHandler<InvoiceLinesUpdatedEvent>,
    INotificationHandler<InvoiceIssuedEvent>,
    INotificationHandler<PaymentRecordedEvent>,
    INotificationHandler<InvoiceStatusChangedEvent>,
    INotificationHandler<PaymentCreatedEvent>,
    INotificationHandler<PaymentAllocatedEvent>,
    INotificationHandler<PaymentCompletedEvent>
{
    private readonly FinanceReadDbContext _context;

    public FinanceProjections(FinanceReadDbContext context)
    {
        _context = context;
    }

    public async Task Handle(InvoiceCreatedEvent e, CancellationToken ct)
    {
        var model = new InvoiceReadModel
        {
            InvoiceId = e.InvoiceId,
            InvoiceNumber = e.InvoiceNumber,
            Type = (int)e.Type,
            PartyId = e.PartyId,
            PartyName = e.PartyName,
            InvoiceDate = e.InvoiceDate,
            DueDate = e.DueDate,
            Currency = e.Currency,
            Status = (int)InvoiceStatus.Draft,
            LinesJson = "[]"
        };
        _context.Invoices.Add(model);
        await _context.SaveChangesAsync(ct);
    }

    public async Task Handle(InvoiceLinesUpdatedEvent e, CancellationToken ct)
    {
        var model = await _context.Invoices.FindAsync(new object[] { e.InvoiceId }, ct);
        if (model != null)
        {
            model.LinesJson = JsonSerializer.Serialize(e.Lines);
            model.TotalAmount = e.Lines.Sum(l => l.TotalAmount);
            model.OutstandingAmount = model.TotalAmount - model.PaidAmount;
            await _context.SaveChangesAsync(ct);
        }
    }

    public async Task Handle(InvoiceIssuedEvent e, CancellationToken ct)
    {
        var model = await _context.Invoices.FindAsync(new object[] { e.InvoiceId }, ct);
        if (model != null)
        {
            model.Status = (int)InvoiceStatus.Issued;
            await _context.SaveChangesAsync(ct);
        }
    }

    public async Task Handle(PaymentRecordedEvent e, CancellationToken ct)
    {
        var model = await _context.Invoices.FindAsync(new object[] { e.InvoiceId }, ct);
        if (model != null)
        {
            model.PaidAmount += e.Amount;
            model.OutstandingAmount = model.TotalAmount - model.PaidAmount;
            model.Status = model.OutstandingAmount <= 0.001m ? (int)InvoiceStatus.FullyPaid : (int)InvoiceStatus.PartiallyPaid;
            await _context.SaveChangesAsync(ct);
        }
    }

    public async Task Handle(InvoiceStatusChangedEvent e, CancellationToken ct)
    {
        var model = await _context.Invoices.FindAsync(new object[] { e.InvoiceId }, ct);
        if (model != null)
        {
            model.Status = (int)e.NewStatus;
            await _context.SaveChangesAsync(ct);
        }
    }

    public async Task Handle(PaymentCreatedEvent e, CancellationToken ct)
    {
        var model = new PaymentReadModel
        {
            PaymentId = e.PaymentId,
            PaymentNumber = e.PaymentNumber,
            Direction = (int)e.Direction,
            PartyId = e.PartyId,
            PartyName = e.PartyName,
            Amount = e.Amount,
            UnallocatedAmount = e.Amount, // Initially unallocated
            Currency = e.Currency,
            PaymentDate = e.PaymentDate,
            Method = (int)e.Method,
            ReferenceNo = e.ReferenceNo,
            Status = (int)PaymentStatus.Draft
        };
        _context.Payments.Add(model);
        await _context.SaveChangesAsync(ct);
    }

    public async Task Handle(PaymentAllocatedEvent e, CancellationToken ct)
    {
        var model = await _context.Payments.FindAsync(new object[] { e.PaymentId }, ct);
        if (model != null)
        {
            model.UnallocatedAmount -= e.AllocationAmount;
            model.InvoiceId = e.InvoiceId; // Capture the invoice ID
            await _context.SaveChangesAsync(ct);
        }
    }

    public async Task Handle(PaymentCompletedEvent e, CancellationToken ct)
    {
        var model = await _context.Payments.FindAsync(new object[] { e.PaymentId }, ct);
        if (model != null)
        {
            model.Status = (int)PaymentStatus.Completed;
            await _context.SaveChangesAsync(ct);
        }
    }
}
