using MediatR;
using ErpSystem.BuildingBlocks.Domain;
using ErpSystem.Finance.Domain;

namespace ErpSystem.Finance.Application;

public record CreateInvoiceCommand(
    string Number,
    InvoiceType Type,
    string PartyId,
    string PartyName,
    DateTime InvoiceDate,
    DateTime DueDate,
    string Currency,
    List<InvoiceLine> Lines
) : IRequest<Guid>;

public record RegisterPaymentCommand(
    string PaymentNumber,
    PaymentDirection Direction,
    string PartyId,
    string PartyName,
    decimal Amount,
    string Currency,
    DateTime PaymentDate,
    PaymentMethod Method,
    string? ReferenceNo,
    Guid? AllocateToInvoiceId = null
) : IRequest<Guid>;

public record IssueInvoiceCommand(Guid InvoiceId) : IRequest;

public record RecordPaymentCommand(
    Guid InvoiceId, 
    decimal Amount, 
    DateTime PaymentDate, 
    PaymentMethod Method, 
    string? ReferenceNo
) : IRequest<Guid>;

public record WriteOffInvoiceCommand(Guid InvoiceId, string Reason) : IRequest;

public record CancelInvoiceCommand(Guid InvoiceId) : IRequest;

public class FinanceCommandHandler(EventStoreRepository<Invoice> invoiceRepo, EventStoreRepository<Payment> paymentRepo) :
    IRequestHandler<CreateInvoiceCommand, Guid>,
    IRequestHandler<RegisterPaymentCommand, Guid>,
    IRequestHandler<IssueInvoiceCommand>,
    IRequestHandler<RecordPaymentCommand, Guid>,
    IRequestHandler<WriteOffInvoiceCommand>,
    IRequestHandler<CancelInvoiceCommand>
{
    public async Task<Guid> Handle(CreateInvoiceCommand request, CancellationToken ct)
    {
        Guid id = Guid.NewGuid();
        Invoice invoice = Invoice.Create(id, request.Number, request.Type, request.PartyId, request.PartyName, request.InvoiceDate, request.DueDate, request.Currency);
        invoice.UpdateLines(request.Lines);
        invoice.Issue(); // Auto-issue for simplicity? Or separate step? Let's auto-issue for now.
        
        await invoiceRepo.SaveAsync(invoice);
        return id;
    }

    public async Task<Guid> Handle(RegisterPaymentCommand request, CancellationToken ct)
    {
        Guid paymentId = Guid.NewGuid();
        Payment payment = Payment.Create(
            paymentId, 
            request.PaymentNumber, 
            request.Direction, 
            request.PartyId, 
            request.PartyName, 
            request.Amount, 
            request.Currency, 
            request.PaymentDate, 
            request.Method, 
            request.ReferenceNo);

        if (request.AllocateToInvoiceId.HasValue)
        {
            Invoice? invoice = await invoiceRepo.LoadAsync(request.AllocateToInvoiceId.Value);
            if (invoice == null) throw new KeyNotFoundException($"Invoice {request.AllocateToInvoiceId} not found");

            // Allocate payment logic
            // 1. Update Payment Aggregate
            payment.AllocateToInvoice(invoice.Id, request.Amount); // Assuming full amount allocation for this simple flow
            payment.Complete(); // Auto-complete if strictly 1-to-1

            // 2. Update Invoice Aggregate
            // We need to coordination here. 
            // Ideally: Payment emits AllocatedEvent -> Process Manager -> Update Invoice
            // BUT for simplicity in this monolith-ish service: application service orchestrates OR direct call.
            // Direct call is safer for consistency within same service boundary if using same transaction (EventStore doesn't do multi-stream tx easily).
            
            // BETTER: Just update Invoice here? No, Payment Aggregate is the source of "Money Received".
            // Implementation choice: Update Invoice directly in this handler for simplicity, 
            // essentially treating this Command as "Pay Invoice" shortcut.
            
            invoice.RecordPayment(paymentId, request.Amount, request.PaymentDate, request.Method, request.ReferenceNo);
            await invoiceRepo.SaveAsync(invoice);
        }

        await paymentRepo.SaveAsync(payment);
        return paymentId;
    }

    public async Task Handle(IssueInvoiceCommand r, CancellationToken ct)
    {
        Invoice? invoice = await invoiceRepo.LoadAsync(r.InvoiceId);
        if (invoice == null) throw new KeyNotFoundException("Invoice not found");
        invoice.Issue();
        await invoiceRepo.SaveAsync(invoice);
    }

    public async Task<Guid> Handle(RecordPaymentCommand r, CancellationToken ct)
    {
        Invoice? invoice = await invoiceRepo.LoadAsync(r.InvoiceId);
        if (invoice == null) throw new KeyNotFoundException("Invoice not found");
        
        Guid paymentId = Guid.NewGuid();
        invoice.RecordPayment(paymentId, r.Amount, r.PaymentDate, r.Method, r.ReferenceNo);
        await invoiceRepo.SaveAsync(invoice);
        return paymentId;
    }

    public async Task Handle(WriteOffInvoiceCommand r, CancellationToken ct)
    {
        Invoice? invoice = await invoiceRepo.LoadAsync(r.InvoiceId);
        if (invoice == null) throw new KeyNotFoundException("Invoice not found");
        invoice.WriteOff(r.Reason);
        await invoiceRepo.SaveAsync(invoice);
    }

    public async Task Handle(CancelInvoiceCommand r, CancellationToken ct)
    {
        Invoice? invoice = await invoiceRepo.LoadAsync(r.InvoiceId);
        if (invoice == null) throw new KeyNotFoundException("Invoice not found");
        invoice.Cancel();
        await invoiceRepo.SaveAsync(invoice);
    }
}
