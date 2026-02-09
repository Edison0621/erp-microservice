using MediatR;
using ErpSystem.Finance.Domain;
using System.Text.Json;

namespace ErpSystem.Finance.Infrastructure;

public class FinanceProjections(FinanceReadDbContext context) :
    INotificationHandler<InvoiceCreatedEvent>,
    INotificationHandler<InvoiceLinesUpdatedEvent>,
    INotificationHandler<InvoiceIssuedEvent>,
    INotificationHandler<PaymentRecordedEvent>,
    INotificationHandler<InvoiceStatusChangedEvent>,
    INotificationHandler<PaymentCreatedEvent>,
    INotificationHandler<PaymentAllocatedEvent>,
    INotificationHandler<PaymentCompletedEvent>
{
    public async Task Handle(InvoiceCreatedEvent e, CancellationToken ct)
    {
        InvoiceReadModel model = new InvoiceReadModel
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
        context.Invoices.Add(model);
        await context.SaveChangesAsync(ct);
    }

    public async Task Handle(InvoiceLinesUpdatedEvent e, CancellationToken ct)
    {
        InvoiceReadModel? model = await context.Invoices.FindAsync([e.InvoiceId], ct);
        if (model != null)
        {
            model.LinesJson = JsonSerializer.Serialize(e.Lines);
            model.TotalAmount = e.Lines.Sum(l => l.TotalAmount);
            model.OutstandingAmount = model.TotalAmount - model.PaidAmount;
            await context.SaveChangesAsync(ct);
        }
    }

    public async Task Handle(InvoiceIssuedEvent e, CancellationToken ct)
    {
        InvoiceReadModel? model = await context.Invoices.FindAsync([e.InvoiceId], ct);
        if (model != null)
        {
            model.Status = (int)InvoiceStatus.Issued;
            await context.SaveChangesAsync(ct);
        }
    }

    public async Task Handle(PaymentRecordedEvent e, CancellationToken ct)
    {
        InvoiceReadModel? model = await context.Invoices.FindAsync([e.InvoiceId], ct);
        if (model != null)
        {
            model.PaidAmount += e.Amount;
            model.OutstandingAmount = model.TotalAmount - model.PaidAmount;
            model.Status = model.OutstandingAmount <= 0.001m ? (int)InvoiceStatus.FullyPaid : (int)InvoiceStatus.PartiallyPaid;
            await context.SaveChangesAsync(ct);
        }
    }

    public async Task Handle(InvoiceStatusChangedEvent e, CancellationToken ct)
    {
        InvoiceReadModel? model = await context.Invoices.FindAsync([e.InvoiceId], ct);
        if (model != null)
        {
            model.Status = (int)e.NewStatus;
            await context.SaveChangesAsync(ct);
        }
    }

    public async Task Handle(PaymentCreatedEvent e, CancellationToken ct)
    {
        PaymentReadModel model = new PaymentReadModel
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
        context.Payments.Add(model);
        await context.SaveChangesAsync(ct);
    }

    public async Task Handle(PaymentAllocatedEvent e, CancellationToken ct)
    {
        PaymentReadModel? model = await context.Payments.FindAsync([e.PaymentId], ct);
        if (model != null)
        {
            model.UnallocatedAmount -= e.AllocationAmount;
            model.InvoiceId = e.InvoiceId; // Capture the invoice ID
            await context.SaveChangesAsync(ct);
        }
    }

    public async Task Handle(PaymentCompletedEvent e, CancellationToken ct)
    {
        PaymentReadModel? model = await context.Payments.FindAsync([e.PaymentId], ct);
        if (model != null)
        {
            model.Status = (int)PaymentStatus.Completed;
            await context.SaveChangesAsync(ct);
        }
    }
}
